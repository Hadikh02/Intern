using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class MeetingAttendee
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public int UserId { get; set; }

    public string? AttendanceStatus { get; set; }

    public string Role { get; set; } = null!;

    public virtual Meeting Meeting { get; set; } = null!;

    public virtual ICollection<Minute> Minutes { get; set; } = new List<Minute>();

    public virtual User User { get; set; } = null!;
}
