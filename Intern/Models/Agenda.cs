﻿using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Agenda
{
    public int Id { get; set; }

    public string Topic { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string ItemNumber { get; set; } = null!;

    public int TimeAllocation { get; set; }

    public int MeetingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Meeting Meeting { get; set; } = null!;
}
