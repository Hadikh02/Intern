namespace Intern.DTOs
{
    public class UpdateAttendeeStatusDto
    {
        public int MeetingId { get; set; }
        public int UserId { get; set; }
        public bool HasAudio { get; set; }
        public bool HasVideo { get; set; }
        public bool IsHandRaised { get; set; }
    }
}
