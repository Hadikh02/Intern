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
    public class MinutesController : ControllerBase
    {
        private readonly InternContext _context;

        public MinutesController(InternContext context)
        {
            _context = context;
        }

        // GET: api/Minutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Minute>>> GetMinutes()
        {
            return await _context.Minutes.ToListAsync();
        }

        // GET: api/Minutes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Minute>> GetMinute(int id)
        {
            var minute = await _context.Minutes.FindAsync(id);

            if (minute == null)
            {
                return NotFound();
            }

            return minute;
        }

        // PUT: api/Minutes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMinute(int id, Minute minute)
        {
            if (id != minute.Id)
            {
                return BadRequest();
            }

            _context.Entry(minute).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MinuteExists(id))
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

        // POST: api/Minutes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Minute>> PostMinute(Minute minute)
        {
            if (minute.MeetingId <= 0)
                return BadRequest("Meeting Id is required and must be greater than 0");

            if (minute.MeetingAttendeeId <= 0)
                return BadRequest("Attendee Id is required and must be greater than 0");

            var meetingExists = await _context.Meetings.FindAsync(minute.MeetingId);
            if (meetingExists == null)
                return BadRequest($"Meeting with Id {minute.MeetingId} not found");

            var attendee = await _context.MeetingAttendees
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == minute.MeetingAttendeeId);

            if (attendee == null)
                return BadRequest($"Attendee with Id {minute.MeetingAttendeeId} not found");

            if (attendee.User.UserType != "Employee")
                return BadRequest("Only employees can assign actions to attendees");

            _context.Minutes.Add(minute);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMinute", new { id = minute.Id }, minute);
        }

        // DELETE: api/Minutes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMinute(int id)
        {
            var minute = await _context.Minutes.FindAsync(id);
            if (minute == null)
            {
                return NotFound();
            }

            _context.Minutes.Remove(minute);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MinuteExists(int id)
        {
            return _context.Minutes.Any(e => e.Id == id);
        }
    }
}
