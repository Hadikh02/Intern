using AutoMapper;
using Intern.DTOs;
using Intern.Models;
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
    public class MeetingsController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;
        public MeetingsController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

            if (room.Status != "Available")
                return Conflict($"Room {room.RoomNumber} is currently {room.Status}");

            var timeConflict = room.Meetings.Any(m =>
                m.MeetingDate == meetingDto.MeetingDate &&
                m.StartTime < meetingDto.EndTime &&
                m.EndTime > meetingDto.StartTime);

            if (timeConflict)
                return Conflict("Room is already booked during the requested time");

            room.Status = "Not Available";
            _context.Rooms.Update(room);

            var meeting = _mapper.Map<Meeting>(meetingDto);

            meeting.RecordingPath ??= string.Empty;
            if (meeting.IsRecorded && meeting.RecordingUploadedAt == null)
                meeting.RecordingUploadedAt = DateOnly.FromDateTime(DateTime.UtcNow);

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, meeting);
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
