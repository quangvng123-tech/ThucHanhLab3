using System.Globalization;
using System.Security.Claims;
using System.Text;
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
        private readonly IWebHostEnvironment _env;

        public DashboardController(TourDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // 1. Thống kê tổng quan (UC28)
        public async Task<IActionResult> Index()
        {
            var stats = await _db.VTourStats.OrderByDescending(s => s.Revenue).ToListAsync();
            ViewBag.TotalRevenue = stats.Sum(s => s.Revenue ?? 0);
            ViewBag.TotalBookings = stats.Sum(s => s.TotalBookings ?? 0);
            ViewBag.PendingCount = await _db.Bookings.CountAsync(b => b.Status == "PENDING");
            ViewBag.PendingReviews = await _db.Reviews.CountAsync(r => r.Status == "PENDING");
            return View(stats);
        }

        // 2. Quản lý Đơn đặt chỗ (UC16, UC17, UC37)
        public async Task<IActionResult> Bookings(string? status)
        {
            var query = _db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Departure).ThenInclude(d => d.Tour)
                .Include(b => b.Promotion)
                .Include(b => b.BookingPassengers)
                .Include(b => b.Payments)
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
        // 3. QUẢN LÝ TOUR + UPLOAD ẢNH TỪ MÁY TÍNH + LỊCH KHỞI HÀNH + LỊCH TRÌNH CHI TIẾT
        // =====================================================================
        public async Task<IActionResult> Tours()
        {
            var tours = await _db.Tours
                .Include(t => t.Category)
                .Include(t => t.DepartureProvince)
                .Include(t => t.TourImages)
                .Include(t => t.TourDepartures)
                .Include(t => t.TourItineraries)
                .Include(t => t.Reviews)
                .OrderByDescending(t => t.TourId)
                .ToListAsync();
            return View(tours);
        }

        // Hàm hỗ trợ 1: Lưu file ảnh từ máy tính vào thư mục wwwroot/images/tours
        private async Task<string> SaveUploadedFileAsync(IFormFile file)
        {
            string uploadFolder = Path.Combine(_env.WebRootPath, "images", "tours");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string ext = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/tours/{uniqueFileName}";
        }

        // Hàm hỗ trợ 2: Tìm Tỉnh/Thành theo tên (hoặc ID); nếu Admin gõ tỉnh mới chưa có trong bảng PROVINCES thì tự động thêm mới
        private async Task<int> ResolveOrCreateProvinceIdAsync(string? provinceNameInput, int? fallbackProvinceId = null, string? regionName = "CENTRAL")
        {
            string validRegion = (regionName == "NORTH" || regionName == "CENTRAL" || regionName == "SOUTH") ? regionName : "CENTRAL";
            string cleanName = (provinceNameInput ?? "").Trim();

            if (!string.IsNullOrEmpty(cleanName))
            {
                var existingProv = await _db.Provinces
                    .FirstOrDefaultAsync(p => p.ProvinceName.ToLower() == cleanName.ToLower());

                if (existingProv != null)
                {
                    if (!string.IsNullOrEmpty(regionName))
                    {
                        existingProv.Region = validRegion;
                        await _db.SaveChangesAsync();
                    }
                    return existingProv.ProvinceId;
                }

                var newProv = new Province
                {
                    ProvinceName = cleanName,
                    Region = validRegion,
                    Description = $"Khám phá hành trình du lịch đặc sắc tại {cleanName}."
                };
                _db.Provinces.Add(newProv);
                await _db.SaveChangesAsync();
                return newProv.ProvinceId;
            }

            if (fallbackProvinceId.HasValue && fallbackProvinceId.Value > 0)
                return fallbackProvinceId.Value;

            var firstProv = await _db.Provinces.FirstOrDefaultAsync();
            return firstProv?.ProvinceId ?? 1;
        }

        // Hàm hỗ trợ 3: Tự động sinh Mã Tour ngẫu nhiên dạng NDN-616, NTY-123 và đảm bảo không bao giờ trùng trong DB
        private async Task<string> GenerateUniqueTourCodeAsync(string? currentCode, string tourName)
        {
            string code = (currentCode ?? "").Trim().ToUpper();

            if (string.IsNullOrEmpty(code) || await _db.Tours.AnyAsync(t => t.TourCode.ToUpper() == code))
            {
                string normalized = (tourName ?? "TOUR").Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();
                foreach (char c in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        sb.Append(c);
                }
                string cleanName = sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToUpper();
                var words = cleanName.Split(new[] { ' ', '-', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

                string prefix = "";
                foreach (var w in words)
                {
                    if (char.IsLetter(w[0]) && prefix.Length < 3)
                        prefix += w[0];
                }

                const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ";
                while (prefix.Length < 3)
                {
                    prefix += alphabet[Random.Shared.Next(alphabet.Length)];
                }

                do
                {
                    int num = Random.Shared.Next(100, 1000);
                    code = $"{prefix}-{num}";
                }
                while (await _db.Tours.AnyAsync(t => t.TourCode.ToUpper() == code));
            }

            return code;
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
            string? tourCode, string tourName, int categoryId,
            string? departureProvinceName, int? departureProvinceId, string? regionName,
            byte durationDays, byte durationNights, string? transport, decimal defaultRating,
            string? summary, string? description, string status,
            // Upload nhiều file ảnh từ máy tính
            List<IFormFile>? imageFiles, List<string>? newImageCaptions,
            // Danh sách các đợt khởi hành & giá
            List<DateTime>? depDates, List<DateTime>? retDates, List<string>? meetingPoints,
            List<decimal>? priceAdults, List<decimal>? priceChilds, List<int>? maxSeatsList, List<string>? depStatuses,
            // Danh sách lịch trình từng ngày
            List<int>? dayNos, List<string>? itiTitles, List<string>? itiContents, List<string>? itiMeals, List<string>? itiAccommodations)
        {
            // 1. Chuẩn hóa Số ngày & Số đêm (Số đêm chỉ được phép = Số ngày - 1 hoặc = Số ngày)
            if (durationDays < 1) durationDays = 1;
            byte minNights = (byte)Math.Max(0, durationDays - 1);
            if (durationNights != minNights && durationNights != durationDays)
            {
                durationNights = minNights;
            }

            // 2. Tự động nhận diện hoặc tạo mới Tỉnh/Thành khởi hành
            int finalProvinceId = await ResolveOrCreateProvinceIdAsync(departureProvinceName, departureProvinceId, regionName);

            // 3. Tự động tạo Mã Tour ngẫu nhiên kiểu NDN-616 nếu để trống hoặc bị trùng
            string finalTourCode = await GenerateUniqueTourCodeAsync(tourCode, tourName);

            int staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tour = new Tour
            {
                TourCode = finalTourCode,
                TourName = tourName.Trim(),
                CategoryId = categoryId,
                DepartureProvinceId = finalProvinceId,
                DurationDays = durationDays,
                DurationNights = durationNights,
                Transport = string.IsNullOrWhiteSpace(transport) ? "BUS" : transport.Trim(),
                DefaultRating = defaultRating,
                Summary = summary,
                Description = description,
                Status = string.IsNullOrWhiteSpace(status) ? "PUBLISHED" : status,
                CreatedBy = staffId,
                CreatedAt = DateTime.Now
            };

            _db.Tours.Add(tour);
            await _db.SaveChangesAsync();

            // 4. Lưu các file ảnh upload từ máy tính
            if (imageFiles != null && imageFiles.Any())
            {
                bool first = true;
                for (int i = 0; i < imageFiles.Count; i++)
                {
                    if (imageFiles[i] != null && imageFiles[i].Length > 0)
                    {
                        string savedUrl = await SaveUploadedFileAsync(imageFiles[i]);
                        string caption = (newImageCaptions != null && i < newImageCaptions.Count && !string.IsNullOrWhiteSpace(newImageCaptions[i]))
                            ? newImageCaptions[i].Trim()
                            : tour.TourName;

                        _db.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = savedUrl,
                            Caption = caption,
                            IsPrimary = first
                        });
                        first = false;
                    }
                }
            }

            // 5. Lưu danh sách Đợt khởi hành & Giá Người lớn / Trẻ em dưới 13 tuổi (Ngày đi >= Hôm nay, Ngày về > Ngày đi)
            if (depDates != null)
            {
                for (int i = 0; i < depDates.Count; i++)
                {
                    decimal pAdult = (priceAdults != null && i < priceAdults.Count) ? priceAdults[i] : 0;
                    decimal pChild = (priceChilds != null && i < priceChilds.Count) ? priceChilds[i] : 0;
                    int seats = (maxSeatsList != null && i < maxSeatsList.Count && maxSeatsList[i] > 0) ? maxSeatsList[i] : 30;
                    DateTime dDate = depDates[i].Date < DateTime.Today ? DateTime.Today : depDates[i].Date;
                    DateTime rDate = (retDates != null && i < retDates.Count && retDates[i].Date > dDate)
                        ? retDates[i].Date
                        : dDate.AddDays(Math.Max(1, (int)durationDays));

                    _db.TourDepartures.Add(new TourDeparture
                    {
                        TourId = tour.TourId,
                        DepartureDate = dDate,
                        ReturnDate = rDate,
                        MeetingPoint = (meetingPoints != null && i < meetingPoints.Count && !string.IsNullOrWhiteSpace(meetingPoints[i])) ? meetingPoints[i].Trim() : "Trung tâm thành phố",
                        PriceAdult = pAdult,
                        PriceChild = pChild,
                        MaxSeats = (byte)Math.Min(seats, 250),
                        BookedSeats = 0,
                        Status = (depStatuses != null && i < depStatuses.Count) ? depStatuses[i] : "OPEN"
                    });
                }
            }

            // 6. Lưu Lịch trình chi tiết từng ngày: Đảm bảo đủ chính xác từ Ngày 1 đến Ngày durationDays
            for (byte day = 1; day <= durationDays; day++)
            {
                int idx = day - 1;
                string title = (itiTitles != null && idx < itiTitles.Count && !string.IsNullOrWhiteSpace(itiTitles[idx]))
                    ? itiTitles[idx].Trim()
                    : $"Hành trình Ngày {day}";
                string content = (itiContents != null && idx < itiContents.Count && !string.IsNullOrWhiteSpace(itiContents[idx]))
                    ? itiContents[idx].Trim()
                    : $"Tham quan và trải nghiệm lịch trình ngày {day}.";
                string meals = (itiMeals != null && idx < itiMeals.Count && !string.IsNullOrWhiteSpace(itiMeals[idx]))
                    ? itiMeals[idx].Trim()
                    : "Sáng, Trưa, Tối";
                string accom = (itiAccommodations != null && idx < itiAccommodations.Count && !string.IsNullOrWhiteSpace(itiAccommodations[idx]))
                    ? itiAccommodations[idx].Trim()
                    : (day == durationDays ? "Không" : "Khách sạn");

                _db.TourItineraries.Add(new TourItinerary
                {
                    TourId = tour.TourId,
                    DayNo = day,
                    Title = title,
                    Content = content,
                    Meals = meals,
                    Accommodation = accom
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo mới tour '{tour.TourName}' (Mã: {tour.TourCode}) thành công!";
            return RedirectToAction(nameof(Tours));
        }

        [HttpGet]
        public async Task<IActionResult> EditTour(int id)
        {
            var tour = await _db.Tours
                .Include(t => t.DepartureProvince)
                .Include(t => t.TourImages)
                .Include(t => t.TourDepartures)
                .Include(t => t.TourItineraries)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            ViewBag.Categories = await _db.TourCategories.ToListAsync();
            ViewBag.Provinces = await _db.Provinces.OrderBy(p => p.ProvinceName).ToListAsync();
            return View(tour);
        }

        [HttpPost]
        public async Task<IActionResult> EditTour(
            int tourId, string? tourCode, string tourName, int categoryId,
            string? departureProvinceName, int? departureProvinceId, string? regionName,
            byte durationDays, byte durationNights, string? transport, decimal defaultRating,
            string? summary, string? description, string status,
            // Ảnh hiện có được giữ lại + Ảnh mới upload từ máy tính
            List<string>? existingImageUrls, List<string>? existingImageCaptions,
            List<IFormFile>? newImageFiles, List<string>? newImageCaptions,
            // Danh sách Lịch khởi hành & Giá Tour
            List<int>? depIds, List<DateTime>? depDates, List<DateTime>? retDates, List<string>? meetingPoints,
            List<decimal>? priceAdults, List<decimal>? priceChilds, List<int>? maxSeatsList, List<string>? depStatuses,
            // Danh sách Lịch trình chi tiết từng ngày
            List<int>? dayNos, List<string>? itiTitles, List<string>? itiContents, List<string>? itiMeals, List<string>? itiAccommodations)
        {
            var tour = await _db.Tours
                .Include(t => t.TourImages)
                .Include(t => t.TourDepartures).ThenInclude(d => d.Bookings)
                .Include(t => t.TourItineraries)
                .FirstOrDefaultAsync(t => t.TourId == tourId);

            if (tour == null) return NotFound();

            // 1. Chuẩn hóa Số ngày & Số đêm
            if (durationDays < 1) durationDays = 1;
            byte minNights = (byte)Math.Max(0, durationDays - 1);
            if (durationNights != minNights && durationNights != durationDays)
            {
                durationNights = minNights;
            }

            // 2. Tự động nhận diện hoặc tạo mới Tỉnh/Thành khởi hành
            int finalProvinceId = await ResolveOrCreateProvinceIdAsync(departureProvinceName, departureProvinceId, regionName);

            if (!string.IsNullOrWhiteSpace(tourCode))
            {
                tour.TourCode = tourCode.Trim().ToUpper();
            }
            tour.TourName = tourName.Trim();
            tour.CategoryId = categoryId;
            tour.DepartureProvinceId = finalProvinceId;
            tour.DurationDays = durationDays;
            tour.DurationNights = durationNights;
            tour.Transport = string.IsNullOrWhiteSpace(transport) ? "BUS" : transport.Trim();
            tour.DefaultRating = defaultRating;
            tour.Summary = summary;
            tour.Description = description;
            tour.Status = status;

            // 3. Cập nhật Hình ảnh (Giữ ảnh cũ chưa bị xóa + Thêm file ảnh mới từ máy tính)
            _db.TourImages.RemoveRange(tour.TourImages);
            bool firstImg = true;

            if (existingImageUrls != null)
            {
                for (int i = 0; i < existingImageUrls.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(existingImageUrls[i]))
                    {
                        _db.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = existingImageUrls[i].Trim(),
                            Caption = (existingImageCaptions != null && i < existingImageCaptions.Count) ? existingImageCaptions[i] : tour.TourName,
                            IsPrimary = firstImg
                        });
                        firstImg = false;
                    }
                }
            }

            if (newImageFiles != null)
            {
                for (int i = 0; i < newImageFiles.Count; i++)
                {
                    if (newImageFiles[i] != null && newImageFiles[i].Length > 0)
                    {
                        string savedUrl = await SaveUploadedFileAsync(newImageFiles[i]);
                        string caption = (newImageCaptions != null && i < newImageCaptions.Count && !string.IsNullOrWhiteSpace(newImageCaptions[i]))
                            ? newImageCaptions[i].Trim()
                            : tour.TourName;

                        _db.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = savedUrl,
                            Caption = caption,
                            IsPrimary = firstImg
                        });
                        firstImg = false;
                    }
                }
            }

            // 4. Cập nhật Lịch khởi hành & Giá Tour (TOUR_DEPARTURES)
            var keptDepIds = depIds != null ? depIds.Where(id => id > 0).ToHashSet() : new HashSet<int>();
            foreach (var oldDep in tour.TourDepartures.ToList())
            {
                if (!keptDepIds.Contains(oldDep.DepartureId))
                {
                    if (!oldDep.Bookings.Any())
                        _db.TourDepartures.Remove(oldDep);
                    else
                        oldDep.Status = "CLOSED";
                }
            }

            if (depDates != null)
            {
                for (int i = 0; i < depDates.Count; i++)
                {
                    int currentDepId = (depIds != null && i < depIds.Count) ? depIds[i] : 0;
                    DateTime dDate = depDates[i].Date;
                    DateTime rDate = (retDates != null && i < retDates.Count && retDates[i].Date > dDate)
                        ? retDates[i].Date
                        : dDate.AddDays(Math.Max(1, (int)durationDays));
                    string mPoint = (meetingPoints != null && i < meetingPoints.Count && !string.IsNullOrWhiteSpace(meetingPoints[i]))
                        ? meetingPoints[i].Trim()
                        : "Trung tâm thành phố";
                    decimal pAdult = (priceAdults != null && i < priceAdults.Count) ? priceAdults[i] : 0;
                    decimal pChild = (priceChilds != null && i < priceChilds.Count) ? priceChilds[i] : 0;
                    int seats = (maxSeatsList != null && i < maxSeatsList.Count && maxSeatsList[i] > 0) ? maxSeatsList[i] : 30;
                    string dStatus = (depStatuses != null && i < depStatuses.Count) ? depStatuses[i] : "OPEN";

                    if (currentDepId > 0)
                    {
                        var existingDep = tour.TourDepartures.FirstOrDefault(d => d.DepartureId == currentDepId);
                        if (existingDep != null)
                        {
                            existingDep.DepartureDate = dDate;
                            existingDep.ReturnDate = rDate;
                            existingDep.MeetingPoint = mPoint;
                            existingDep.PriceAdult = pAdult;
                            existingDep.PriceChild = pChild;
                            existingDep.MaxSeats = (byte)Math.Max((int)existingDep.BookedSeats, Math.Min(seats, 250));
                            existingDep.Status = dStatus;
                        }
                    }
                    else
                    {
                        _db.TourDepartures.Add(new TourDeparture
                        {
                            TourId = tour.TourId,
                            DepartureDate = dDate < DateTime.Today ? DateTime.Today : dDate,
                            ReturnDate = rDate,
                            MeetingPoint = mPoint,
                            PriceAdult = pAdult,
                            PriceChild = pChild,
                            MaxSeats = (byte)Math.Min(seats, 250),
                            BookedSeats = 0,
                            Status = dStatus
                        });
                    }
                }
            }

            // 5. Cập nhật Lịch trình chi tiết từng ngày (Đúng bằng số ngày durationDays)
            _db.TourItineraries.RemoveRange(tour.TourItineraries);
            await _db.SaveChangesAsync();

            for (byte day = 1; day <= durationDays; day++)
            {
                int idx = day - 1;
                string title = (itiTitles != null && idx < itiTitles.Count && !string.IsNullOrWhiteSpace(itiTitles[idx]))
                    ? itiTitles[idx].Trim()
                    : $"Hành trình Ngày {day}";
                string content = (itiContents != null && idx < itiContents.Count && !string.IsNullOrWhiteSpace(itiContents[idx]))
                    ? itiContents[idx].Trim()
                    : $"Tham quan và trải nghiệm lịch trình ngày {day}.";
                string meals = (itiMeals != null && idx < itiMeals.Count && !string.IsNullOrWhiteSpace(itiMeals[idx]))
                    ? itiMeals[idx].Trim()
                    : "Sáng, Trưa, Tối";
                string accom = (itiAccommodations != null && idx < itiAccommodations.Count && !string.IsNullOrWhiteSpace(itiAccommodations[idx]))
                    ? itiAccommodations[idx].Trim()
                    : (day == durationDays ? "Không" : "Khách sạn");

                _db.TourItineraries.Add(new TourItinerary
                {
                    TourId = tour.TourId,
                    DayNo = day,
                    Title = title,
                    Content = content,
                    Meals = meals,
                    Accommodation = accom
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã lưu toàn bộ thông tin, hình ảnh, lịch khởi hành và lịch trình {durationDays} ngày của '{tour.TourName}'!";
            return RedirectToAction(nameof(Tours));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _db.Tours
                .Include(t => t.TourDepartures).ThenInclude(d => d.Bookings)
                .Include(t => t.TourImages)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourDestinations)
                .Include(t => t.Reviews)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            // Ràng buộc: Nếu đã có khách đặt chỗ thì tuyệt đối không cho xóa
            bool hasCustomerBookings = tour.TourDepartures.Any(d => d.BookedSeats > 0 || d.Bookings.Any());
            if (hasCustomerBookings)
            {
                TempData["Error"] = $"Không thể xóa tour '{tour.TourName}' vì đã có khách hàng đặt chỗ!";
                return RedirectToAction(nameof(Tours));
            }

            _db.Reviews.RemoveRange(tour.Reviews);
            _db.TourDestinations.RemoveRange(tour.TourDestinations);
            _db.TourItineraries.RemoveRange(tour.TourItineraries);
            _db.TourImages.RemoveRange(tour.TourImages);
            _db.TourDepartures.RemoveRange(tour.TourDepartures);
            _db.Tours.Remove(tour);

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa hoàn toàn tour '{tour.TourName}' khỏi hệ thống!";
            return RedirectToAction(nameof(Tours));
        }

        // 4. Quản lý Đánh giá
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
                    ? "Đã DUYỆT đánh giá! Điểm sao trung bình của Tour đã được cập nhật."
                    : "Đã từ chối / ẨN đánh giá này.";
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
                TempData["Success"] = "Đã xóa đánh giá của khách hàng.";
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