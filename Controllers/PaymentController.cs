using System.Data;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    // ---------- Helper VNPay (sandbox / production dùng chung) ----------
    public static class VnPayHelper
    {
        private static string Encode(string s) => WebUtility.UrlEncode(s);

        private static string Hmac512(string key, string data)
        {
            using var h = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
        }

        private static string BuildQuery(SortedDictionary<string, string> p) =>
            string.Join("&", p.Where(kv => !string.IsNullOrEmpty(kv.Value))
                              .Select(kv => $"{Encode(kv.Key)}={Encode(kv.Value)}"));

        public static string BuildPaymentUrl(IConfiguration cfg, string txnRef, decimal amount,
                                             string orderInfo, string ip, string returnUrl)
        {
            var p = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = cfg["VnPay:TmnCode"]!,
                ["vnp_Amount"] = ((long)(amount * 100)).ToString(),   // VNPay yêu cầu nhân 100
                ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ip,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = orderInfo,                          // không dấu, không ký tự đặc biệt
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = txnRef
            };
            string query = BuildQuery(p);
            return $"{cfg["VnPay:PayUrl"]}?{query}&vnp_SecureHash={Hmac512(cfg["VnPay:HashSecret"]!, query)}";
        }

        // Dùng cho cả Return URL và IPN
        public static bool ValidateSignature(IQueryCollection q, string secret)
        {
            var p = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in q)
                if (kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                    p[kv.Key] = kv.Value.ToString();
            string expected = Hmac512(secret, BuildQuery(p));
            return string.Equals(expected, q["vnp_SecureHash"].ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Authorize]
    public class PaymentController : Controller
    {
        private readonly TourDbContext _db;
        private readonly IConfiguration _cfg;
        public PaymentController(TourDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

        // Khách bấm "Thanh toán" -> tạo giao dịch PENDING -> chuyển sang cổng
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int bookingId, string method = "VNPAY")
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var booking = await _db.Bookings.Include(b => b.Customer)
                                  .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            // Chỉ chủ đơn mới được thanh toán đơn của mình
            if (booking == null || booking.Customer.UserId != userId) return Forbid();

            if (method != "VNPAY")
            {
                TempData["Error"] = "Phương thức này chưa được tích hợp. Vui lòng chọn VNPay hoặc thanh toán tại quầy.";
                return RedirectToAction("MyBookings", "Booking");
            }

            try
            {
                var pId = new OracleParameter("p_payment_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
                var pRef = new OracleParameter("p_txn_ref", OracleDbType.Varchar2, 100) { Direction = ParameterDirection.Output };
                var pAmt = new OracleParameter("p_amount", OracleDbType.Decimal) { Direction = ParameterDirection.Output };

                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_INIT_PAYMENT(:p_booking_id, :p_method, :p_payment_id, :p_txn_ref, :p_amount); END;",
                    new OracleParameter("p_booking_id", OracleDbType.Int32) { Value = bookingId },
                    new OracleParameter("p_method", OracleDbType.Varchar2) { Value = method },
                    pId, pRef, pAmt);

                string txnRef = pRef.Value.ToString()!;
                decimal amount = Convert.ToDecimal(pAmt.Value.ToString());

                string returnUrl = Url.Action(nameof(VnPayReturn), "Payment", null, Request.Scheme)!;
                string url = VnPayHelper.BuildPaymentUrl(_cfg, txnRef, amount,
                    $"Thanh toan don {booking.BookingCode}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1", returnUrl);
                return Redirect(url);
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "Không thể thanh toán: " + ex.Message;
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        // Cổng đưa khách quay lại (khách có thể đóng trình duyệt trước bước này -> cần thêm IPN bên dưới)
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            if (!VnPayHelper.ValidateSignature(Request.Query, _cfg["VnPay:HashSecret"]!))
            {
                TempData["Error"] = "Chữ ký thanh toán không hợp lệ.";
                return RedirectToAction("MyBookings", "Booking");
            }

            bool ok = Request.Query["vnp_ResponseCode"] == "00" && Request.Query["vnp_TransactionStatus"] == "00";
            decimal paid = decimal.Parse(Request.Query["vnp_Amount"]!) / 100m;

            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_COMPLETE_PAYMENT(:p_txn_ref, :p_success, :p_paid_amount, :p_booking_id); END;",
                    new OracleParameter("p_txn_ref", OracleDbType.Varchar2) { Value = Request.Query["vnp_TxnRef"].ToString() },
                    new OracleParameter("p_success", OracleDbType.Int32) { Value = ok ? 1 : 0 },
                    new OracleParameter("p_paid_amount", OracleDbType.Decimal) { Value = paid },
                    new OracleParameter("p_booking_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output });

                if (ok) TempData["Success"] = "Thanh toán thành công!";
                else TempData["Error"] = "Thanh toán không thành công hoặc đã bị hủy.";
            }
            catch (OracleException ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyBookings", "Booking");
        }

        // IPN: VNPay gọi server-to-server. Cần URL public (ngrok khi chạy local) và đăng ký trong cổng merchant.
        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> VnPayIpn()
        {
            if (!VnPayHelper.ValidateSignature(Request.Query, _cfg["VnPay:HashSecret"]!))
                return Json(new { RspCode = "97", Message = "Invalid signature" });
            try
            {
                bool ok = Request.Query["vnp_ResponseCode"] == "00" && Request.Query["vnp_TransactionStatus"] == "00";
                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_COMPLETE_PAYMENT(:p_txn_ref, :p_success, :p_paid_amount, :p_booking_id); END;",
                    new OracleParameter("p_txn_ref", OracleDbType.Varchar2) { Value = Request.Query["vnp_TxnRef"].ToString() },
                    new OracleParameter("p_success", OracleDbType.Int32) { Value = ok ? 1 : 0 },
                    new OracleParameter("p_paid_amount", OracleDbType.Decimal) { Value = decimal.Parse(Request.Query["vnp_Amount"]!) / 100m },
                    new OracleParameter("p_booking_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output });
                return Json(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (OracleException ex) when (ex.Number == 20080) { return Json(new { RspCode = "01", Message = "Order not found" }); }
            catch (OracleException ex) when (ex.Number == 20081) { return Json(new { RspCode = "04", Message = "Invalid amount" }); }
            catch { return Json(new { RspCode = "99", Message = "Unknown error" }); }
        }
    }

    // ---------- Nhân viên: thu tiền mặt + duyệt hoàn tiền ----------
    [Authorize(Roles = "ADMIN,STAFF")]
    public class PaymentAdminController : Controller
    {
        private readonly TourDbContext _db;
        public PaymentAdminController(TourDbContext db) => _db = db;

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CashPayment(int bookingId)
        {
            try
            {
                int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _db.Database.ExecuteSqlRawAsync("BEGIN SP_RECORD_CASH_PAYMENT(:b, :s); END;",
                    new OracleParameter("b", OracleDbType.Int32) { Value = bookingId },
                    new OracleParameter("s", OracleDbType.Int32) { Value = staffId });
                TempData["Success"] = "Đã ghi nhận thu tiền mặt.";
            }
            catch (OracleException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("Bookings", "Dashboard");
        }

        public async Task<IActionResult> Refunds()
        {
            var list = await _db.Payments.Include(p => p.Booking).ThenInclude(b => b.Customer)
                .Where(p => p.PaymentType == "REFUND")
                .OrderBy(p => p.Status == "PENDING" ? 0 : 1).ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int paymentId, bool approve)
        {
            try
            {
                int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _db.Database.ExecuteSqlRawAsync("BEGIN SP_PROCESS_REFUND(:p, :s, :a); END;",
                    new OracleParameter("p", OracleDbType.Int32) { Value = paymentId },
                    new OracleParameter("s", OracleDbType.Int32) { Value = staffId },
                    new OracleParameter("a", OracleDbType.Int32) { Value = approve ? 1 : 0 });
                TempData["Success"] = approve ? "Đã xác nhận hoàn tiền." : "Đã từ chối yêu cầu hoàn tiền.";
            }
            catch (OracleException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Refunds));
        }
    }
}
