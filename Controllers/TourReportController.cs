using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementWeb.Models;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    [Authorize(Roles = "ADMIN,STAFF")]
    public class TourReportController : Controller
    {
        private readonly TourDbContext _db;
        public TourReportController(TourDbContext db) => _db = db;

        // Danh sách hành khách của một tour (chỉ họ tên + giới tính), nhóm theo từng ngày khởi hành.
        // Cùng quy tắc với V_TOUR_STATS: chỉ tính các đơn chưa hủy.
        [HttpGet]
        public async Task<IActionResult> Passengers(int tourId)
        {
            var tour = await _db.Tours.FirstOrDefaultAsync(t => t.TourId == tourId);
            if (tour == null) return NotFound();

            var bookings = await _db.Bookings
                .Include(b => b.Departure)
                .Include(b => b.BookingPassengers)
                .Where(b => b.Departure.TourId == tourId && b.Status != "CANCELLED")
                .OrderBy(b => b.Departure.DepartureDate).ThenBy(b => b.BookingId)
                .ToListAsync();

            var model = new TourPassengerDetail
            {
                TourName = tour.TourName,
                Adults = bookings.Sum(b => (int)b.NumAdults),
                Children = bookings.Sum(b => (int)b.NumChildren)
            };

            foreach (var g in bookings.GroupBy(b => b.Departure.DepartureId))
            {
                var group = new DepartureGroup { DepartureDate = g.First().Departure.DepartureDate };
                foreach (var p in g.SelectMany(b => b.BookingPassengers).OrderBy(p => p.PassengerId))
                {
                    group.Passengers.Add(new PassengerRow
                    {
                        FullName = p.FullName,
                        Gender = p.Gender,
                        IsChild = p.PassengerType == "CHILD"
                    });
                }
                model.Departures.Add(group);
            }

            return PartialView("_TourPassengers", model);
        }
    }
}
