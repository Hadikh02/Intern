namespace Intern.DTOs
{
    public class MeetingAttendeeDto
    {
        public int Id { get; set; }

        public int MeetingId { get; set; }

        public int UserId { get; set; }

        public string? AttendanceStatus { get; set; }

        public string Role { get; set; } = null!;
    }
}
