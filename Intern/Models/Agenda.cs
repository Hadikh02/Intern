using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;
namespace Intern.Models;

public partial class Agenda
{
    public int Id { get; set; }

    public string Topic { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string ItemNumber { get; set; } = null!;

    public TimeOnly TimeAllocation { get; set; }

    public int MeetingId { get; set; }
    [JsonIgnore]
    [ValidateNever]
    public virtual Meeting Meeting { get; set; } = null!;
}
