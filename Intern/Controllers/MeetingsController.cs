using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Intern.Models;

namespace Intern.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeetingsController : ControllerBase
    {
        private readonly InternContext _context;

        public MeetingsController(InternContext context)
        {
            _context = context;
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
        public async Task<ActionResult<Meeting>> PostMeeting([FromBody] Meeting meeting)
        {
            // 1. Basic validations
            if (meeting.UserId <= 0)
                return BadRequest("User Id is required and must be greater than 0");

            if (meeting.RoomId <= 0)
                return BadRequest("Room Id is required and must be greater than 0");

            if (string.IsNullOrWhiteSpace(meeting.Title))
                return BadRequest("Meeting title is required");

            if (meeting.EndTime <= meeting.StartTime)
                return BadRequest("End time must be after start time");

            // 2. Verify user exists and is employee
            var user = await _context.Users.FindAsync(meeting.UserId);
            if (user == null)
                return BadRequest($"User with ID {meeting.UserId} not found");

            // 3. Get and validate room
            var room = await _context.Rooms
                .Include(r => r.Meetings)
                .FirstOrDefaultAsync(r => r.Id == meeting.RoomId);

            if (room == null)
                return BadRequest($"Room with ID {meeting.RoomId} not found");

            if (room.Status != "Available")
                return Conflict($"Room {room.RoomNumber} is currently {room.Status}");

            // 4. Check for time conflicts
            var timeConflict = room.Meetings.Any(m =>
                m.MeetingDate == meeting.MeetingDate &&
                m.StartTime < meeting.EndTime &&
                m.EndTime > meeting.StartTime);

            if (timeConflict)
                return Conflict("Room is already booked during the requested time");

            // 5. Update room status
            room.Status = "Not Available";
            _context.Rooms.Update(room);

            // 6. Set meeting defaults
            meeting.RecordingPath ??= string.Empty;
            if (meeting.IsRecorded && meeting.RecordingUploadedAt == default)
                meeting.RecordingUploadedAt = DateOnly.FromDateTime(DateTime.UtcNow);

            // 7. Save changes
            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            // 8. Return response
            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, new
            {
                meeting.Id,
                meeting.Title,
                Organizer = new { user.Id, user.FirstName, user.LastName },
                Room = new { room.Id, room.RoomNumber, room.Location },
                meeting.MeetingDate,
                meeting.StartTime,
                meeting.EndTime,
                room.Status
            });
        }

        // DELETE: api/Meetings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }
            var checkMeetAttendeee = await _context.MeetingAttendees.AnyAsync(m => m.MeetingId == id);
            if (checkMeetAttendeee)
            {
                return BadRequest("Meeting cannot be deleted since there are attandance");
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MeetingExists(int id)
        {
            return _context.Meetings.Any(e => e.Id == id);
        }
    }
}
