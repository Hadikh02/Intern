using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Intern.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeetingAttendeesController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;

        public MeetingAttendeesController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/MeetingAttendees
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeetingAttendee>>> GetMeetingAttendees()
        {
            return await _context.MeetingAttendees.ToListAsync();
        }

        // GET: api/MeetingAttendees/5
        [Authorize(Roles = "Admin,Employee")]
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
        [Authorize(Roles = "Admin,Employee")]
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
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<MeetingAttendeeDto>> PostMeetingAttendee([FromBody] MeetingAttendeeDto createDto)
        {
            // 1. Basic validation
            if (createDto.UserId <= 0)
                return BadRequest("User Id is required and must be greater than 0");

            if (createDto.MeetingId <= 0)
                return BadRequest("Meeting Id is required and must be greater than 0");

            // 2. Check current time against meeting time
            var currentTime = DateTime.UtcNow;

            // 3. Get meeting with time range
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == createDto.MeetingId);

            if (meeting == null)
                return BadRequest($"Meeting {createDto.MeetingId} not found");

            // 4. Time validation
            if (currentTime < meeting.StartTime)
                return BadRequest($"Meeting hasn't started yet (starts at {meeting.StartTime})");

            if (currentTime > meeting.EndTime)
                return BadRequest($"Meeting already ended (ended at {meeting.EndTime})");

            // 5. Check user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == createDto.UserId);
            if (!userExists)
                return BadRequest($"User Id {createDto.UserId} not found");

            // 6. Check existing attendance
            if (await _context.MeetingAttendees
                .AnyAsync(a => a.MeetingId == createDto.MeetingId
                            && a.UserId == createDto.UserId))
            {
                return Conflict("User already attending");
            }

            // 7. Map DTO to entity
            var meetingAttendee = _mapper.Map<MeetingAttendee>(createDto);

            // 8. Add and save
            _context.MeetingAttendees.Add(meetingAttendee);
            await _context.SaveChangesAsync();

            // 9. Map entity to DTO for response
            var resultDto = _mapper.Map<MeetingAttendeeDto>(meetingAttendee);

            return CreatedAtAction(nameof(GetMeetingAttendee), new { id = meetingAttendee.Id }, resultDto);
        }
        // DELETE: api/MeetingAttendees/5
        [Authorize(Roles = "Admin,Employee")]
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
