using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Notification
{
    public int Id { get; set; }

    public string EventType { get; set; } = null!;

    public string EventDescription { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public int MeetingId { get; set; }
    public int UserId { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
