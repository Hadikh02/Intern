﻿namespace Intern.DTOs
{
    public class AgendaDto
    {
        public int Id { get; set; }

        public string Topic { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string ItemNumber { get; set; } = null!;

        public int TimeAllocation { get; set; }

        public int MeetingId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
