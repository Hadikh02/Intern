using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace Intern.Models;

public partial class MeetingAttendee
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public int UserId { get; set; }

    public string? AttendanceStatus { get; set; }

    public string Role { get; set; } = null!;
    [JsonIgnore]
    [ValidateNever]
    public virtual Meeting Meeting { get; set; } = null!;

    public virtual ICollection<Minute> Minutes { get; set; } = new List<Minute>();
    [JsonIgnore]
    [ValidateNever]
    public virtual User User { get; set; } = null!;
}
