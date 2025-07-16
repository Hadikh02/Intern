namespace Intern.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = null!;
        public string Location { get; set; } = null!;
        public bool HasVideo { get; set; }
        public bool HasProjector { get; set; }
        public int Capacity { get; set; }
        public int UserId { get; set; }
    }
}
