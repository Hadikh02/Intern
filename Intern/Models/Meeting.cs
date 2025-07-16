using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Meeting
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public string Title { get; set; } = null!;

    public DateOnly MeetingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ICollection<Agenda> Agenda { get; set; } = new List<Agenda>();

    public virtual ICollection<MeetingAttendee> MeetingAttendees { get; set; } = new List<MeetingAttendee>();

    public virtual ICollection<Minute> Minutes { get; set; } = new List<Minute>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
