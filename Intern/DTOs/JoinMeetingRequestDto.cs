namespace Intern.DTOs
{
    public class JoinMeetingRequestDto
    {
        public int MeetingId { get; set; }
        public int UserId { get; set; }
        public bool HasAudio { get; set; } = false;
        public bool HasVideo { get; set; } = false;
        public bool IsHandRaised { get; set; } = false;
        public string Role { get; set; } = "Participant"; // Default role
    }
}