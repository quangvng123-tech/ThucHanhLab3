using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly TourDbContext _db;

        public HomeController(TourDbContext db)
        {
            _db = db;
        }

        // UC05 + UC07 + UC32: Danh sách tour từ V_TOUR_LIST
        public async Task<IActionResult> Index(int? provinceId, decimal? maxPrice, DateTime? departureDate, string? keyword)
        {
            DateTime today = DateTime.Today;
            if (departureDate.HasValue && departureDate.Value.Date < today)
            {
                departureDate = today;
            }

            var tourList = await _db.VTourLists.ToListAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                tourList = tourList.Where(t =>
                    (t.TourName != null && t.TourName.ToLower().Contains(kw)) ||
                    (t.TourCode != null && t.TourCode.ToLower().Contains(kw)) ||
                    (t.DepartureFrom != null && t.DepartureFrom.ToLower().Contains(kw)) ||
                    (t.CategoryName != null && t.CategoryName.ToLower().Contains(kw))
                ).ToList();
            }

            if (provinceId.HasValue || maxPrice.HasValue || departureDate.HasValue)
            {
                var toursWithDetails = await _db.Tours
                    .Include(t => t.TourDestinations).ThenInclude(td => td.Destination)
                    .Include(t => t.TourDepartures)
                    .Where(t => t.Status == "PUBLISHED")
                    .ToListAsync();

                var validTourIds = toursWithDetails
                    .Where(t => !provinceId.HasValue
                        || t.DepartureProvinceId == provinceId.Value
                        || t.TourDestinations.Any(td => td.Destination != null && td.Destination.ProvinceId == provinceId.Value))
                    .Where(t => (!maxPrice.HasValue && !departureDate.HasValue) ||
                        t.TourDepartures.Any(dep =>
                            dep.Status == "OPEN" &&
                            dep.DepartureDate.Date >= today &&
                            (!maxPrice.HasValue || dep.PriceAdult <= maxPrice.Value) &&
                            (!departureDate.HasValue || dep.DepartureDate.Date >= departureDate.Value.Date)))
                    .Select(t => (decimal)t.TourId)
                    .ToHashSet();

                tourList = tourList.Where(v => validTourIds.Contains(v.TourId)).ToList();
            }

            // Map ảnh chính của từng Tour để hiển thị ngoài trang chủ
            var primaryImages = await _db.TourImages
                .GroupBy(i => i.TourId)
                .Select(g => new { TourId = g.Key, Url = g.OrderByDescending(x => x.IsPrimary).First().ImageUrl })
                .ToDictionaryAsync(x => (decimal)x.TourId, x => x.Url);

            ViewBag.PrimaryImages = primaryImages;
            ViewBag.Provinces = await _db.Provinces.OrderBy(p => p.ProvinceName).ToListAsync();
            ViewBag.AllTourNames = await _db.Tours.Where(t => t.Status == "PUBLISHED").Select(t => t.TourName).ToListAsync();
            ViewBag.SelectedProvince = provinceId;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.DepartureDate = departureDate?.ToString("yyyy-MM-dd");
            ViewBag.Keyword = keyword;

            return View(tourList.OrderBy(t => t.NextDeparture).ToList());
        }

        // UC06 + UC32: Chi tiết Tour (Gallery nhiều ảnh + Sao trung bình + Đánh giá của khách)
        public async Task<IActionResult> Details(int id)
        {
            var tour = await _db.Tours
                .Include(t => t.Category)
                .Include(t => t.DepartureProvince)
                .Include(t => t.TourImages)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourDepartures)
                .Include(t => t.TourDestinations).ThenInclude(td => td.Destination)
                .Include(t => t.Reviews).ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            // Kiểm tra xem người dùng đang đăng nhập đã có đánh giá nào cho Tour này chưa
            Review? myReview = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer != null)
                {
                    myReview = tour.Reviews.FirstOrDefault(r => r.CustomerId == customer.CustomerId);
                }
            }

            ViewBag.MyReview = myReview;
            return View(tour);
        }

        // UC33: Khách hàng Gửi mới hoặc Cập nhật đánh giá (Tự động chuyển sang PENDING đợi Admin duyệt)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveReview(int tourId, byte rating, string commentText)
        {
            if (rating < 1 || rating > 5) rating = 5;

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

            var existingReview = await _db.Reviews
                .FirstOrDefaultAsync(r => r.TourId == tourId && r.CustomerId == customer.CustomerId);

            if (existingReview == null)
            {
                // Tìm đơn đặt chỗ gần nhất của khách cho tour này (nếu có)
                var booking = await _db.Bookings
                    .Include(b => b.Departure)
                    .FirstOrDefaultAsync(b => b.CustomerId == customer.CustomerId && b.Departure.TourId == tourId);

                _db.Reviews.Add(new Review
                {
                    TourId = tourId,
                    CustomerId = customer.CustomerId,
                    BookingId = booking?.BookingId,
                    Rating = rating,
                    CommentText = commentText.Trim(),
                    Status = "PENDING", // Đợi Admin duyệt mới hiển thị công khai
                    CreatedAt = DateTime.Now
                });
                TempData["Success"] = "Đã gửi đánh giá! Đánh giá của bạn đang chờ Quản trị viên duyệt để hiển thị công khai.";
            }
            else
            {
                // Sửa đánh giá -> Đưa trạng thái về lại PENDING để Quản trị viên duyệt lại
                existingReview.Rating = rating;
                existingReview.CommentText = commentText.Trim();
                existingReview.Status = "PENDING";
                existingReview.ModeratedBy = null;
                existingReview.CreatedAt = DateTime.Now;
                TempData["Success"] = "Đã cập nhật đánh giá! Đánh giá đã chuyển sang trạng thái chờ Quản trị viên duyệt lại.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = tourId });
        }

        // Khách hàng tự xóa đánh giá của chính mình
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteMyReview(int reviewId, int tourId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer != null)
            {
                var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.CustomerId == customer.CustomerId);
                if (review != null)
                {
                    _db.Reviews.Remove(review);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa đánh giá của bạn thành công.";
                }
            }
            return RedirectToAction(nameof(Details), new { id = tourId });
        }

        // UC08: Xem Địa phương & Điểm du lịch 3 miền (Tự động liên kết với cả các Tour mới tạo)
        public async Task<IActionResult> Destinations(string? region)
        {
            var query = _db.Provinces
                .Include(p => p.Destinations.Where(d => d.Status == "ACTIVE"))
                .Include(p => p.Tours.Where(t => t.Status == "PUBLISHED"))
                    .ThenInclude(t => t.TourDepartures)
                .Include(p => p.Destinations)
                    .ThenInclude(d => d.TourDestinations)
                        .ThenInclude(td => td.Tour)
                .AsQueryable();

            if (!string.IsNullOrEmpty(region))
            {
                query = query.Where(p => p.Region == region);
            }

            ViewBag.SelectedRegion = region;
            return View(await query.OrderBy(p => p.ProvinceId).ToListAsync());
        }
    }
}