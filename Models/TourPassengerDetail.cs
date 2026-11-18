namespace TourManagementWeb.Models
{
    // Dữ liệu hiển thị cửa sổ "Chi tiết hành khách" của từng tour ở trang tổng quan
    public class TourPassengerDetail
    {
        public string TourName { get; set; } = "";
        public int Adults { get; set; }     // tổng người lớn (các đơn chưa hủy)
        public int Children { get; set; }   // tổng trẻ em (các đơn chưa hủy)
        public int Total => Adults + Children;

        public List<DepartureGroup> Departures { get; set; } = new();

        public IEnumerable<PassengerRow> AllPassengers => Departures.SelectMany(d => d.Passengers);
        public int Listed => AllPassengers.Count();
        public int Male => AllPassengers.Count(p => p.Gender == "MALE");
        public int Female => AllPassengers.Count(p => p.Gender == "FEMALE");
    }

    public class DepartureGroup
    {
        public DateTime DepartureDate { get; set; }
        public List<PassengerRow> Passengers { get; set; } = new();
    }

    public class PassengerRow
    {
        public string FullName { get; set; } = "";
        public string? Gender { get; set; }   // MALE / FEMALE / OTHER / null
        public bool IsChild { get; set; }
    }
}
