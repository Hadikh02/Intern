using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace Intern.Models;

public partial class Meeting
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public string Title { get; set; } = null!;

    public DateOnly MeetingDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string RecordingPath { get; set; } = null!;

    public bool IsRecorded { get; set; }

    public DateOnly RecordingUploadedAt { get; set; }

    public virtual ICollection<Agenda> Agenda { get; set; } = new List<Agenda>();

    public virtual ICollection<MeetingAttendee> MeetingAttendees { get; set; } = new List<MeetingAttendee>();

    public virtual ICollection<Minute> Minutes { get; set; } = new List<Minute>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    [JsonIgnore]
    [ValidateNever]
    public virtual Room Room { get; set; } = null!;
    [JsonIgnore]
    [ValidateNever]
    public virtual User User { get; set; } = null!;
}
