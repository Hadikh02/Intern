namespace Intern.DTOs
{
    public class MeetingDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }     // Organizer or owner

        public int RoomId { get; set; }

        public string Title { get; set; } = null!;

        public DateOnly MeetingDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public string RecordingPath { get; set; } = null!;

        public bool IsRecorded { get; set; }

        public DateOnly RecordingUploadedAt { get; set; }
    }
}
