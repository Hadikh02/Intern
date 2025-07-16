namespace Intern.DTOs
{
    public class MeetingSummaryDto
    {
        public int MeetingsLastWeek { get; set; }

        // From GetMostUsedRoom
        public string MostUsedRoomNumber { get; set; }
        public int MostUsedRoomMeetingCount { get; set; }

        // You might also want to add:
        public DateTime SummaryDate { get; set; } = DateTime.UtcNow;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
    }
}
