using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly TourDbContext _db;
        public BookingController(TourDbContext db) => _db = db;

        // UC09: Hiển thị form đặt tour theo đợt khởi hành
        [HttpGet]
        public async Task<IActionResult> Create(int departureId)
        {
            var dep = await _db.TourDepartures
                .Include(d => d.Tour)
                .FirstOrDefaultAsync(d => d.DepartureId == departureId);

            if (dep == null || dep.Status != "OPEN")
            {
                TempData["Error"] = "Đợt khởi hành không tồn tại hoặc không còn nhận đặt chỗ.";
                return RedirectToAction("Index", "Home");
            }

            DateTime today = DateTime.Today;
            ViewBag.Promotions = await _db.Promotions
                .Where(p => p.Status == "ACTIVE" && p.StartDate <= today && p.EndDate >= today && (p.UsageLimit == null || p.UsedCount < p.UsageLimit))
                .OrderBy(p => p.MinOrderAmount)
                .ToListAsync();

            return View(dep);
        }

        // UC09 + UC10 + UC11: Tự động đếm Người lớn & Trẻ em từ danh sách hành khách và gọi SP_CREATE_BOOKING
        [HttpPost]
        public async Task<IActionResult> Create(int departureId, int totalPassengers, string? promoCode, string? note, List<string> paxNames, List<string> paxTypes, List<string> paxGenders)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                var user = await _db.Users.FindAsync(userId);
                customer = new Customer
                {
                    UserId = userId,
                    FullName = user?.FullName ?? User.Identity?.Name ?? "Khách hàng",
                    Email = user?.Email ?? "customer@tourdemo.vn",
                    Phone = user?.Phone ?? "0900000000",
                    CreatedAt = DateTime.Now
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            // Tự động đếm số Người lớn (ADULT) và Trẻ em (CHILD) từ danh sách dòng hành khách
            int numAdults = 0;
            int numChildren = 0;
            if (paxTypes != null && paxTypes.Any())
            {
                numAdults = paxTypes.Count(t => t == "ADULT");
                numChildren = paxTypes.Count(t => t == "CHILD");
            }
            else
            {
                numAdults = Math.Max(1, totalPassengers);
            }

            if (numAdults < 1)
            {
                TempData["Error"] = "Đơn đặt chỗ phải có ít nhất 1 Người lớn trong danh sách hành khách.";
                return RedirectToAction(nameof(Create), new { departureId });
            }

            try
            {
                var pCust = new OracleParameter("p_customer_id", OracleDbType.Int32) { Value = customer.CustomerId };
                var pDep = new OracleParameter("p_departure_id", OracleDbType.Int32) { Value = departureId };
                var pAdults = new OracleParameter("p_adults", OracleDbType.Int32) { Value = numAdults };
                var pChildren = new OracleParameter("p_children", OracleDbType.Int32) { Value = numChildren };
                var pPromo = new OracleParameter("p_promo_code", OracleDbType.Varchar2)
                {
                    Value = string.IsNullOrWhiteSpace(promoCode) ? DBNull.Value : promoCode.Trim().ToUpper()
                };
                var pNote = new OracleParameter("p_note", OracleDbType.Varchar2)
                {
                    Value = string.IsNullOrWhiteSpace(note) ? DBNull.Value : note.Trim()
                };
                var pBookingId = new OracleParameter("p_booking_id", OracleDbType.Decimal)
                {
                    Direction = ParameterDirection.Output
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_CREATE_BOOKING(:p_customer_id, :p_departure_id, :p_adults, :p_children, :p_promo_code, :p_note, :p_booking_id); END;",
                    pCust, pDep, pAdults, pChildren, pPromo, pNote, pBookingId);

                int newBookingId = Convert.ToInt32(pBookingId.Value.ToString());

                // Lưu chi tiết từng hành khách vào bảng BOOKING_PASSENGERS
                if (paxNames != null)
                {
                    for (int i = 0; i < paxNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(paxNames[i]))
                        {
                            _db.BookingPassengers.Add(new BookingPassenger
                            {
                                BookingId = newBookingId,
                                FullName = paxNames[i].Trim(),
                                PassengerType = (paxTypes != null && i < paxTypes.Count) ? paxTypes[i] : "ADULT",
                                Gender = (paxGenders != null && i < paxGenders.Count) ? paxGenders[i] : "MALE"
                            });
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = $"Đặt tour thành công ({numAdults} người lớn, {numChildren} trẻ em)! Đơn #{newBookingId} đã được ghi nhận.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "Không thể đặt tour: " + ex.Message;
                return RedirectToAction(nameof(Create), new { departureId });
            }
        }

        // UC13: Lịch sử đặt tour của khách
        public async Task<IActionResult> MyBookings()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return View(new List<Booking>());

            var bookings = await _db.Bookings
                .Include(b => b.Departure).ThenInclude(d => d.Tour)
                .Include(b => b.Promotion)
                .Include(b => b.BookingPassengers)
                .Where(b => b.CustomerId == customer.CustomerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> PayOnline(int bookingId, string method = "VNPAY")
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null || booking.PaymentStatus == "PAID") return RedirectToAction(nameof(MyBookings));

            _db.Payments.Add(new Payment
            {
                BookingId = booking.BookingId,
                PaymentType = "PAYMENT",
                Method = method,
                Amount = booking.FinalAmount,
                TransactionRef = $"{method}{DateTime.Now:yyyyMMddHHmmss}",
                Status = "SUCCESS",
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Thanh toán đơn {booking.BookingCode} thành công!";
            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_CANCEL_BOOKING(:p_booking_id); END;",
                    new OracleParameter("p_booking_id", OracleDbType.Int32) { Value = bookingId });

                TempData["Success"] = "Đã hủy đơn đặt tour và hoàn trả chỗ trống thành công!";
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "Không thể hủy đơn: " + ex.Message;
            }
            return RedirectToAction(nameof(MyBookings));
        }
    }
}