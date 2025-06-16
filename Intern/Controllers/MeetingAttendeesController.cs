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
    public class MeetingAttendeesController : ControllerBase
    {
        private readonly InternContext _context;

        public MeetingAttendeesController(InternContext context)
        {
            _context = context;
        }

        // GET: api/MeetingAttendees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeetingAttendee>>> GetMeetingAttendees()
        {
            return await _context.MeetingAttendees.ToListAsync();
        }

        // GET: api/MeetingAttendees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MeetingAttendee>> GetMeetingAttendee(int id)
        {
            var meetingAttendee = await _context.MeetingAttendees.FindAsync(id);

            if (meetingAttendee == null)
            {
                return NotFound();
            }

            return meetingAttendee;
        }

        // PUT: api/MeetingAttendees/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeetingAttendee(int id, MeetingAttendee meetingAttendee)
        {
            if (id != meetingAttendee.Id)
            {
                return BadRequest();
            }

            _context.Entry(meetingAttendee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeetingAttendeeExists(id))
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

        [HttpPost]
        public async Task<ActionResult<MeetingAttendee>> PostMeetingAttendee([FromBody] MeetingAttendee meetingAttendee)
        {
            // 1. Basic validation
            if (meetingAttendee.UserId <= 0)
                return BadRequest("User Id is required and must be greater than 0");

            if (meetingAttendee.MeetingId <= 0)
                return BadRequest("Meeting Id is required and must be greater than 0");

            // 2. Check current time against meeting time
            var currentTime = DateTime.UtcNow;

            // 3. Get meeting with time range
            var meeting = await _context.Meetings
                .Include(m => m.User) // Organizer info
                .FirstOrDefaultAsync(m => m.Id == meetingAttendee.MeetingId);

            if (meeting == null)
                return BadRequest($"Meeting {meetingAttendee.MeetingId} not found");

            // 4. Time validation
            if (currentTime < meeting.StartTime)
                return BadRequest($"Meeting hasn't started yet (starts at {meeting.StartTime})");

            if (currentTime > meeting.EndTime)
                return BadRequest($"Meeting already ended (ended at {meeting.EndTime})");

            // 5. Check user exists
            var user = await _context.Users.FindAsync(meetingAttendee.UserId);
            if (user == null)
                return BadRequest($"User Id {meetingAttendee.UserId} not found");

            // 6. Check if organizer is adding attendees
            var isOrganizerAdding = meeting.UserId == meetingAttendee.UserId;

            // 7. Check existing attendance
            if (await _context.MeetingAttendees
                .AnyAsync(a => a.MeetingId == meetingAttendee.MeetingId
                            && a.UserId == meetingAttendee.UserId))
            {
                return Conflict("User already attending");
            }


            // 9. Add and save
            _context.MeetingAttendees.Add(meetingAttendee);
            await _context.SaveChangesAsync();

            // 10. Return response
            return CreatedAtAction(nameof(GetMeetingAttendee), new { id = meetingAttendee.Id }, new
            {
                meetingAttendee.Id,
                User = new { user.Id, user.FirstName, user.LastName },
                Meeting = new { meeting.Id, meeting.Title, meeting.StartTime, meeting.EndTime },
                meetingAttendee.Role,
                meetingAttendee.AttendanceStatus,
                IsOrganizer = isOrganizerAdding
            });
        }
        // DELETE: api/MeetingAttendees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeetingAttendee(int id)
        {
            var meetingAttendee = await _context.MeetingAttendees.FindAsync(id);
            if (meetingAttendee == null)
            {
                return NotFound();
            }
            var checkMinute = await _context.Minutes.AnyAsync(minute => minute.MeetingAttendeeId == id);
            if (checkMinute)
            {
                return BadRequest($"The user with id {id} cannot be removed since you assign task for this user");
            }

            _context.MeetingAttendees.Remove(meetingAttendee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MeetingAttendeeExists(int id)
        {
            return _context.MeetingAttendees.Any(e => e.Id == id);
        }
    }
}
