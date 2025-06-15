using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Minute
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public string AssignAction { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime DueDate { get; set; }

    public int? MeetingAttendeeId { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;

    public virtual MeetingAttendee? MeetingAttendee { get; set; }
}
