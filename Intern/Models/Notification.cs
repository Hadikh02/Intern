using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string EventType { get; set; } = null!;

    public string EventDescription { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public int MeetingId { get; set; }

    public virtual Meeting Meeting { get; set; } = null!;
}
