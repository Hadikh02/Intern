using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Intern.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Intern.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeetingsController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        public MeetingsController(InternContext context, IMapper mapper, IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
        }

        // GET: api/Meetings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Meeting>>> GetMeetings()
        {
            return await _context.Meetings.ToListAsync();
        }

        // GET: api/Meetings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Meeting>> GetMeeting(int id)
        {
            var meeting = await _context.Meetings.FindAsync(id);

            if (meeting == null)
            {
                return NotFound();
            }

            return meeting;
        }
        [HttpGet("Room/{roomId}")]
        public async Task<IActionResult> GetMeetingsByRoomAndDate(int roomId, [FromQuery] DateOnly date)
        {
            var meetings = await _context.Meetings
                .Where(m => m.RoomId == roomId && m.MeetingDate == date)
                .Select(m => new {
                    m.StartTime,
                    m.EndTime
                })
                .ToListAsync();

            return Ok(meetings);
        }

        // PUT: api/Meetings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeeting(int id, Meeting meeting)
        {
            if (id != meeting.Id)
            {
                return BadRequest();
            }
            if (string.IsNullOrWhiteSpace(meeting.Title))
            {
                return BadRequest("Meeting title is required");
            }


            _context.Entry(meeting).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeetingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Meetings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Meeting>> PostMeeting([FromBody] MeetingDto meetingDto)
        {
            if (meetingDto.UserId <= 0)
                return BadRequest("User Id is required");

            if (meetingDto.RoomId <= 0)
                return BadRequest("Room Id is required");

            if (string.IsNullOrWhiteSpace(meetingDto.Title))
                return BadRequest("Meeting title is required");

            if (meetingDto.EndTime <= meetingDto.StartTime)
                return BadRequest("End time must be after start time");

            var user = await _context.Users.FindAsync(meetingDto.UserId);
            if (user == null)
                return BadRequest($"User with ID {meetingDto.UserId} not found");

            var room = await _context.Rooms.Include(r => r.Meetings)
                                           .FirstOrDefaultAsync(r => r.Id == meetingDto.RoomId);

            if (room == null)
                return BadRequest($"Room with ID {meetingDto.RoomId} not found");

            var timeConflict = room.Meetings.Any(m =>
                m.MeetingDate == meetingDto.MeetingDate &&
                m.StartTime < meetingDto.EndTime &&
                m.EndTime > meetingDto.StartTime);

            if (timeConflict)
                return Conflict("Room is already booked during the requested time");

            _context.Rooms.Update(room);

            var meeting = _mapper.Map<Meeting>(meetingDto);

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, meeting);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var meeting = await _context.Meetings
                .Include(m => m.MeetingAttendees).ThenInclude(a => a.User)
                .Include(m => m.Agenda)
                .Include(m => m.Minutes)
                .Include(m => m.Notifications)
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null) return NotFound();

            if (meeting.UserId != int.Parse(userId))
                return Forbid("Only meeting organizer can delete");

            var meetingDateTime = meeting.MeetingDate.ToDateTime(meeting.StartTime);
            if (DateTime.Now >= meetingDateTime)
                return BadRequest("Cannot delete meeting that has already started");

            // ✅ Send emails to all attendees
            foreach (var attendee in meeting.MeetingAttendees)
            {
                var user = attendee.User;
                if (user == null || string.IsNullOrWhiteSpace(user.Email)) continue;

                var emailRequest = new EmailNotificationRequest
                {
                    To = user.Email,
                    Subject = $"[Cancelled] Meeting: {meeting.Title}",
                    RecipientName = $"{user.FirstName} {user.LastName}",
                    EventType = "Meeting Cancellation",
                    EventDescription = $"The meeting titled \"{meeting.Title}\" has been cancelled by the organizer.",
                    MeetingTitle = meeting.Title,
                    MeetingDate = meeting.MeetingDate.ToString("yyyy-MM-dd"),
                    MeetingTime = meeting.StartTime.ToString("hh\\:mm"),
                    MeetingDuration = $"{(meeting.EndTime - meeting.StartTime).TotalMinutes} minutes",
                    RoomDetails = $"Room ID: {meeting.RoomId}",
                    SentAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _ = _emailService.SendMeetingNotificationAsync(emailRequest); // fire-and-forget
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Add to your MeetingsController
        [HttpGet("summary/monthly")]
        public async Task<ActionResult<MeetingSummaryDto>> GetMonthlySummary()
        {
            var date = DateTime.UtcNow.AddDays(-30);
            var count = await _context.Meetings
                .Where(m => m.MeetingDate >= DateOnly.FromDateTime(date))
                .CountAsync();

            return Ok(new { count });
        }

        [HttpGet("summary/weekly")]
        public async Task<ActionResult<MeetingSummaryDto>> GetWeeklySummary()
        {
            var date = DateTime.UtcNow.AddDays(-7);
            var count = await _context.Meetings
                .Where(m => m.MeetingDate >= DateOnly.FromDateTime(date))
                .CountAsync();

            return Ok(new { count });
        }

        [HttpGet("summary/most-used-room")]
        public async Task<ActionResult> GetMostUsedRoom()
        {
            var result = await _context.Meetings
                .GroupBy(m => m.RoomId)
                .Select(g => new {
                    RoomId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            var room = await _context.Rooms.FindAsync(result.RoomId);

            return Ok(new
            {
                roomNumber = room?.RoomNumber ?? "Unknown",
                count = result.Count
            });
        }

        private bool MeetingExists(int id)
        {
            return _context.Meetings.Any(e => e.Id == id);
        }
    }
}
