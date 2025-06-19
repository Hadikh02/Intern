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
    public class MinutesController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;

        public MinutesController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Minutes
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Minute>>> GetMinutes()
        {
            return await _context.Minutes.ToListAsync();
        }

        // GET: api/Minutes/5
        [Authorize(Roles = "Admin,Employee")]
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
        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMinute(int id, [FromBody] MinuteDto minuteDto)
        {
            if (id != minuteDto.Id)
                return BadRequest("Minute ID mismatch.");

            var existingMinute = await _context.Minutes.FindAsync(id);
            if (existingMinute == null)
                return NotFound();

            // Use AutoMapper to map updated fields from DTO to entity
            _mapper.Map(minuteDto, existingMinute);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MinuteExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }


        // POST: api/Minutes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<MinuteDto>> PostMinute([FromBody] MinuteDto minuteDto)
        {
            if (minuteDto.MeetingId <= 0)
                return BadRequest("Meeting Id is required and must be greater than 0");

            if (minuteDto.MeetingAttendeeId == null || minuteDto.MeetingAttendeeId <= 0)
                return BadRequest("Attendee Id is required and must be greater than 0");

            var meetingExists = await _context.Meetings.FindAsync(minuteDto.MeetingId);
            if (meetingExists == null)
                return BadRequest($"Meeting with Id {minuteDto.MeetingId} not found");

            var attendee = await _context.MeetingAttendees
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == minuteDto.MeetingAttendeeId);
            if (attendee == null)
                return BadRequest($"Attendee with Id {minuteDto.MeetingAttendeeId} not found");

            if (string.IsNullOrWhiteSpace(minuteDto.AssignAction))
                return BadRequest("AssignAction is required");

            if (minuteDto.CreatedAt == default)
                minuteDto.CreatedAt = DateTime.UtcNow;

            var minute = _mapper.Map<Minute>(minuteDto);

            _context.Minutes.Add(minute);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<MinuteDto>(minute);
            return CreatedAtAction("GetMinute", new { id = minute.Id }, resultDto);
        }


        // DELETE: api/Minutes/5
        [Authorize(Roles = "Admin,Employee")]
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
