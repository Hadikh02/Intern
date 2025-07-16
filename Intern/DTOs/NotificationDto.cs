namespace Intern.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }

        public string EventType { get; set; } = null!;

        public string EventDescription { get; set; } = null!;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public int MeetingId { get; set; }
        public int UserID { get; set; }
    }
}
