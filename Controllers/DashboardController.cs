using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    [Authorize(Roles = "ADMIN,STAFF")]
    public class DashboardController : Controller
    {
        private readonly TourDbContext _db;
        public DashboardController(TourDbContext db) => _db = db;

        // 1. Thống kê tổng quan
        public async Task<IActionResult> Index()
        {
            var stats = await _db.VTourStats.OrderByDescending(s => s.Revenue).ToListAsync();
            ViewBag.TotalRevenue = stats.Sum(s => s.Revenue ?? 0);
            ViewBag.TotalBookings = stats.Sum(s => s.TotalBookings ?? 0);
            ViewBag.PendingCount = await _db.Bookings.CountAsync(b => b.Status == "PENDING");
            ViewBag.PendingReviews = await _db.Reviews.CountAsync(r => r.Status == "PENDING");
            return View(stats);
        }

        // 2. Quản lý Đơn đặt chỗ
        public async Task<IActionResult> Bookings(string? status)
        {
            var query = _db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Departure).ThenInclude(d => d.Tour)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            ViewBag.CurrentStatus = status;
            return View(await query.OrderByDescending(b => b.CreatedAt).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            try
            {
                int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _db.Database.ExecuteSqlRawAsync(
                    "BEGIN SP_CONFIRM_BOOKING(:p_booking_id, :p_staff_id); END;",
                    new OracleParameter("p_booking_id", OracleDbType.Int32) { Value = bookingId },
                    new OracleParameter("p_staff_id", OracleDbType.Int32) { Value = staffId });

                TempData["Success"] = $"Đã xác nhận đơn #{bookingId} thành công!";
            }
            catch (OracleException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Bookings));
        }

        [HttpPost]
        public async Task<IActionResult> RecordCashPayment(int bookingId)
        {
            var booking = await _db.Bookings.FindAsync(bookingId);
            if (booking != null && booking.PaymentStatus == "UNPAID")
            {
                _db.Payments.Add(new Payment
                {
                    BookingId = bookingId,
                    PaymentType = "PAYMENT",
                    Method = "CASH",
                    Amount = booking.FinalAmount,
                    TransactionRef = $"CASH-{DateTime.Now:yyyyMMddHHmmss}",
                    Status = "SUCCESS",
                    PaidAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thu tiền mặt tại quầy cho đơn {booking.BookingCode}.";
            }
            return RedirectToAction(nameof(Bookings));
        }

        // =====================================================================
        // 3. QUẢN LÝ TOUR: DANH SÁCH + THÊM + SỬA + XÓA + NHIỀU HÌNH ẢNH + SỐ SAO MẶC ĐỊNH
        // =====================================================================
        public async Task<IActionResult> Tours()
        {
            var tours = await _db.Tours
                .Include(t => t.Category)
                .Include(t => t.DepartureProvince)
                .Include(t => t.TourImages)
                .Include(t => t.TourDepartures)
                .Include(t => t.Reviews)
                .OrderByDescending(t => t.TourId)
                .ToListAsync();
            return View(tours);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTour()
        {
            ViewBag.Categories = await _db.TourCategories.ToListAsync();
            ViewBag.Provinces = await _db.Provinces.OrderBy(p => p.ProvinceName).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTour(
            string tourCode, string tourName, int categoryId, int departureProvinceId,
            byte durationDays, byte durationNights, string transport, decimal defaultRating,
            string? summary, string? description, string status,
            decimal priceAdult, decimal priceChild, DateTime departureDate, short maxSeats, string? meetingPoint,
            List<string>? imageUrls, List<string>? imageCaptions)
        {
            if (await _db.Tours.AnyAsync(t => t.TourCode.ToUpper() == tourCode.Trim().ToUpper()))
            {
                TempData["Error"] = "Mã Tour này đã tồn tại! Vui lòng nhập mã khác.";
                return RedirectToAction(nameof(CreateTour));
            }

            int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tour = new Tour
            {
                TourCode = tourCode.Trim().ToUpper(),
                TourName = tourName.Trim(),
                CategoryId = categoryId,
                DepartureProvinceId = departureProvinceId,
                DurationDays = durationDays,
                DurationNights = durationNights,
                Transport = transport,
                DefaultRating = defaultRating,
                Summary = summary,
                Description = description,
                Status = status,
                CreatedBy = staffId,
                CreatedAt = DateTime.Now
            };

            _db.Tours.Add(tour);
            await _db.SaveChangesAsync();

            // Lưu nhiều hình ảnh cho Tour
            if (imageUrls != null)
            {
                bool first = true;
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(imageUrls[i]))
                    {
                        _db.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = imageUrls[i].Trim(),
                            Caption = (imageCaptions != null && i < imageCaptions.Count) ? imageCaptions[i] : tour.TourName,
                            IsPrimary = first
                        });
                        first = false;
                    }
                }
            }

            // Tạo luôn 1 đợt khởi hành mở bán để hiển thị giá & ngày đi ngay trên trang chủ
            if (priceAdult > 0)
            {
                _db.TourDepartures.Add(new TourDeparture
                {
                    TourId = tour.TourId,
                    DepartureDate = departureDate.Date,
                    ReturnDate = departureDate.Date.AddDays(Math.Max(1, durationDays - 1)),
                    PriceAdult = priceAdult,
                    PriceChild = priceChild,
                    MaxSeats = maxSeats > 0 ? (byte)maxSeats : (byte)30,
                    BookedSeats = 0,
                    MeetingPoint = string.IsNullOrWhiteSpace(meetingPoint) ? "Trung tâm thành phố" : meetingPoint,
                    Status = "OPEN"
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm mới tour {tour.TourName} kèm danh sách hình ảnh thành công!";
            return RedirectToAction(nameof(Tours));
        }

        [HttpGet]
        public async Task<IActionResult> EditTour(int id)
        {
            var tour = await _db.Tours
                .Include(t => t.TourImages)
                .Include(t => t.TourDepartures)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            ViewBag.Categories = await _db.TourCategories.ToListAsync();
            ViewBag.Provinces = await _db.Provinces.OrderBy(p => p.ProvinceName).ToListAsync();
            return View(tour);
        }

        [HttpPost]
        public async Task<IActionResult> EditTour(
            int tourId, string tourCode, string tourName, int categoryId, int departureProvinceId,
            byte durationDays, byte durationNights, string transport, decimal defaultRating,
            string? summary, string? description, string status,
            List<string>? imageUrls, List<string>? imageCaptions)
        {
            var tour = await _db.Tours.Include(t => t.TourImages).FirstOrDefaultAsync(t => t.TourId == tourId);
            if (tour == null) return NotFound();

            tour.TourCode = tourCode.Trim().ToUpper();
            tour.TourName = tourName.Trim();
            tour.CategoryId = categoryId;
            tour.DepartureProvinceId = departureProvinceId;
            tour.DurationDays = durationDays;
            tour.DurationNights = durationNights;
            tour.Transport = transport;
            tour.DefaultRating = defaultRating;
            tour.Summary = summary;
            tour.Description = description;
            tour.Status = status;

            // Cập nhật lại toàn bộ danh sách ảnh của Tour
            _db.TourImages.RemoveRange(tour.TourImages);
            if (imageUrls != null)
            {
                bool first = true;
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(imageUrls[i]))
                    {
                        _db.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = imageUrls[i].Trim(),
                            Caption = (imageCaptions != null && i < imageCaptions.Count) ? imageCaptions[i] : tour.TourName,
                            IsPrimary = first
                        });
                        first = false;
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật tour {tour.TourName} thành công!";
            return RedirectToAction(nameof(Tours));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _db.Tours
                .Include(t => t.TourDepartures).ThenInclude(d => d.Bookings)
                .Include(t => t.Reviews)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            // Nếu tour đã có đơn đặt chỗ thì ẩn đi để bảo toàn lịch sử tài chính, nếu chưa có đơn thì xóa hẳn
            if (tour.TourDepartures.Any(d => d.Bookings.Any()))
            {
                tour.Status = "HIDDEN";
                await _db.SaveChangesAsync();
                TempData["Error"] = $"Tour '{tour.TourName}' đã có khách đặt đơn nên được chuyển sang trạng thái ẨN (HIDDEN) để bảo toàn dữ liệu kế toán.";
            }
            else
            {
                _db.Reviews.RemoveRange(tour.Reviews);
                _db.TourDepartures.RemoveRange(tour.TourDepartures);
                _db.Tours.Remove(tour);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa hoàn toàn tour '{tour.TourName}' khỏi hệ thống!";
            }
            return RedirectToAction(nameof(Tours));
        }

        // =====================================================================
        // 4. QUẢN LÝ ĐÁNH GIÁ CỦA KHÁCH HÀNG (DUYỆT / KHÔNG DUYỆT / XÓA)
        // =====================================================================
        public async Task<IActionResult> Reviews(string? status)
        {
            var query = _db.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Tour)
                .Include(r => r.ModeratedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            ViewBag.CurrentStatus = status;
            return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ModerateReview(int reviewId, string status)
        {
            var review = await _db.Reviews.FindAsync(reviewId);
            if (review != null && (status == "APPROVED" || status == "HIDDEN"))
            {
                int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                review.Status = status;
                review.ModeratedBy = staffId;
                await _db.SaveChangesAsync();

                TempData["Success"] = status == "APPROVED"
                    ? "Đã DUYỆT đánh giá! Điểm sao trung bình của Tour đã được cập nhật tự động."
                    : "Đã từ chối / ẨN đánh giá này khỏi trang Tour.";
            }
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReviewAdmin(int reviewId)
        {
            var review = await _db.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa vĩnh viễn đánh giá của khách hàng.";
            }
            return RedirectToAction(nameof(Reviews));
        }

        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _db.AuditLogs.Include(a => a.User).OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync();
            return View(logs);
        }
    }
}