﻿using System;
using System.Collections.Generic;

namespace Intern.Models;

public partial class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? UserType { get; set; }
    public string? RefreshToken {  get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? VerificationCode { get; set; } 

    public DateTime? VerificationCodeExpiry { get; set; }

    public virtual ICollection<MeetingAttendee> MeetingAttendees { get; set; } = new List<MeetingAttendee>();

    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
