using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string? Status { get; set; }

    public bool HasVideo { get; set; }

    public bool HasProjector { get; set; }

    public int Capacity { get; set; }

    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
