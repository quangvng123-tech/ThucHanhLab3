using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly TourDbContext _db;
        public FavoriteController(TourDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Trang "Tour yêu thích" của khách đang đăng nhập (tour thêm gần nhất hiện đầu tiên)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = CurrentUserId;

            var favs = await _db.FavoriteTours
                .Where(f => f.Customer.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.TourId)
                .ToListAsync();

            var order = new Dictionary<decimal, int>();
            for (int i = 0; i < favs.Count; i++) order[favs[i]] = i;

            var tours = (await _db.VTourLists.ToListAsync())
                .Where(v => order.ContainsKey(v.TourId))
                .OrderBy(v => order[v.TourId])
                .ToList();

            // Ảnh chính của từng tour (giống trang chủ)
            ViewBag.PrimaryImages = await _db.TourImages
                .GroupBy(i => i.TourId)
                .Select(g => new { TourId = g.Key, Url = g.OrderByDescending(x => x.IsPrimary).First().ImageUrl })
                .ToDictionaryAsync(x => (decimal)x.TourId, x => x.Url);

            return View(tours);
        }

        // Bấm tim: chưa yêu thích -> thêm; đã yêu thích -> bỏ. Trả về trạng thái mới để giao diện đồng bộ.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int tourId)
        {
            if (!await _db.Tours.AnyAsync(t => t.TourId == tourId)) return NotFound();

            var customer = await GetOrCreateCustomerAsync();
            var fav = await _db.FavoriteTours
                .FirstOrDefaultAsync(f => f.CustomerId == customer.CustomerId && f.TourId == tourId);

            try
            {
                if (fav == null)
                    _db.FavoriteTours.Add(new FavoriteTour
                    {
                        CustomerId = customer.CustomerId,
                        TourId = tourId,
                        CreatedAt = DateTime.Now
                    });
                else
                    _db.FavoriteTours.Remove(fav);

                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Bấm quá nhanh làm trùng khóa chính (CUSTOMER_ID, TOUR_ID): bỏ qua, lấy lại trạng thái thực tế
                _db.ChangeTracker.Clear();
            }

            bool favorited = await _db.FavoriteTours
                .AnyAsync(f => f.CustomerId == customer.CustomerId && f.TourId == tourId);
            int count = await _db.FavoriteTours.CountAsync(f => f.CustomerId == customer.CustomerId);

            return Json(new { favorited, count });
        }

        private async Task<Customer> GetOrCreateCustomerAsync()
        {
            int userId = CurrentUserId;
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null) return customer;

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
            return customer;
        }
    }
}
