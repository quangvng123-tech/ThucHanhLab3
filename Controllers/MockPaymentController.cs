using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    // Cổng thanh toán GIẢ LẬP dùng cho đồ án / demo.
    // Luồng giống cổng thật: tạo giao dịch PENDING -> sang trang cổng -> chốt SUCCESS/FAILED.
    [Authorize]
    public class MockPaymentController : Controller
    {
        private readonly TourDbContext _db;
        public MockPaymentController(TourDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Bước 1: khách bấm "Thanh toán" -> tạo giao dịch PENDING -> chuyển sang trang cổng
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int bookingId, string method = "BANK")
        {
            var booking = await _db.Bookings.Include(b => b.Customer)
                                  .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null || booking.Customer.UserId != CurrentUserId) return Forbid();

            if (method != "VNPAY" && method != "MOMO" && method != "BANK") method = "BANK";

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

                return RedirectToAction(nameof(Gateway), new { txnRef = pRef.Value.ToString() });
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "Không thể thanh toán: " + ex.Message;
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        // Bước 2: trang "cổng thanh toán"
        [HttpGet]
        public async Task<IActionResult> Gateway(string txnRef)
        {
            var p = await FindOwnPending(txnRef);
            if (p == null)
            {
                TempData["Error"] = "Giao dịch không tồn tại hoặc đã được xử lý.";
                return RedirectToAction("MyBookings", "Booking");
            }
            return View(p);
        }

        // Bước 3: khách chọn kết quả -> chốt giao dịch
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string txnRef, string outcome)
        {
            var p = await FindOwnPending(txnRef);
            if (p == null)
            {
                TempData["Error"] = "Giao dịch không tồn tại hoặc đã được xử lý.";
                return RedirectToAction("MyBookings", "Booking");
            }

            bool ok = outcome == "success";
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_COMPLETE_PAYMENT(:p_txn_ref, :p_success, :p_paid_amount, :p_booking_id); END;",
                    new OracleParameter("p_txn_ref", OracleDbType.Varchar2) { Value = txnRef },
                    new OracleParameter("p_success", OracleDbType.Int32) { Value = ok ? 1 : 0 },
                    new OracleParameter("p_paid_amount", OracleDbType.Decimal) { Value = p.Amount },
                    new OracleParameter("p_booking_id", OracleDbType.Decimal) { Direction = ParameterDirection.Output });

                if (ok) TempData["Success"] = $"Thanh toán đơn {p.Booking.BookingCode} thành công!";
                else TempData["Error"] = "Thanh toán không thành công hoặc đã bị hủy.";
            }
            catch (OracleException ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyBookings", "Booking");
        }

        // Chỉ cho phép thao tác giao dịch PENDING thuộc về chính người đang đăng nhập
        private Task<Payment?> FindOwnPending(string txnRef)
        {
            int uid = CurrentUserId;
            return _db.Payments.Include(x => x.Booking).ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(x => x.TransactionRef == txnRef
                                       && x.PaymentType == "PAYMENT"
                                       && x.Status == "PENDING"
                                       && x.Booking.Customer.UserId == uid);
        }
    }
}
