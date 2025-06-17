namespace Intern.DTOs
{
    public class MinuteDto
    {
        public int Id { get; set; }

        public int MeetingId { get; set; }

        public string AssignAction { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime DueDate { get; set; }

        public int? MeetingAttendeeId { get; set; }
    }
}
