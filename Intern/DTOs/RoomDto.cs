namespace Intern.DTOs
{
    public class RoomDto
    {
        public string RoomNumber { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string? Status { get; set; }
        public bool HasVideo { get; set; }
        public bool HasProjector { get; set; }
        public int Capacity { get; set; }
        public int UserId { get; set; }
    }
}
