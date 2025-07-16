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
using System.Security.Claims;
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

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MeetingAttendee>>> GetMeetingAttendees([FromQuery] int? meetingId)
        {
            var query = _context.MeetingAttendees
                .Include(a => a.User)
                .AsQueryable();

            if (meetingId.HasValue)
                query = query.Where(a => a.MeetingId == meetingId);

            return await query.ToListAsync();
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
        [HttpGet("joined")]
        public async Task<ActionResult<IEnumerable<MeetingAttendee>>> GetJoinedAttendees(int meetingId)
        {
            // Get the latest status for each user
            var latestAttendees = _context.MeetingAttendees
                .Include(a => a.User)
                .Where(a => a.MeetingId == meetingId)
                .GroupBy(a => a.UserId)
                .Select(g => g.OrderByDescending(a => a.Id).First()) // Get the most recent record per user
                .Where(a => a.AttendanceStatus == "Present" || a.AttendanceStatus == "Joined") // Only active attendees
                .Select(a => new {
                    a.Id,
                    a.MeetingId,
                    a.UserId,
                    a.User.FirstName,
                    a.User.LastName,
                    a.AttendanceStatus
                })
                .ToList();

            return Ok(latestAttendees);
        }
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateAttendeeStatus([FromBody] UpdateAttendeeStatusDto dto)
        {
            var attendee = await _context.MeetingAttendees
                .Where(a => a.MeetingId == dto.MeetingId && a.UserId == dto.UserId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            if (attendee == null)
            {
                return NotFound("Attendee not found");
            }

            _context.MeetingAttendees.Update(attendee);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("leave")]
        public IActionResult LeaveMeeting([FromBody] LeaveMeetingRequestDto request)
        {
            // Get the most recent attendee record for this user and meeting
            var attendee = _context.MeetingAttendees
                .Where(a => a.MeetingId == request.MeetingId && a.UserId == request.UserId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (attendee == null)
                return NotFound("Attendee not found.");

            // Check if already left
            if (attendee.AttendanceStatus == "Left")
                return Ok("Already left the meeting.");

            // Update status to Left
            attendee.AttendanceStatus = "Left";

            try
            {
                _context.SaveChanges();
                return Ok("Left meeting successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error leaving meeting: {ex.Message}");
            }
        }

        // Optional: Add a method to clean up duplicate records
        [HttpPost("cleanup-duplicates")]
        public IActionResult CleanupDuplicateAttendees(int meetingId)
        {
            var duplicates = _context.MeetingAttendees
                .Where(a => a.MeetingId == meetingId)
                .GroupBy(a => a.UserId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderByDescending(a => a.Id).Skip(1)) // Keep the latest, remove older ones
                .ToList();

            if (duplicates.Any())
            {
                _context.MeetingAttendees.RemoveRange(duplicates);
                _context.SaveChanges();
                return Ok($"Removed {duplicates.Count} duplicate records.");
            }

            return Ok("No duplicates found.");
        }
        [HttpPut("join")]
        public async Task<IActionResult> JoinMeeting([FromBody] JoinMeetingRequestDto request)
        {
            if (request == null || request.MeetingId <= 0 || request.UserId <= 0)
                return BadRequest("Invalid join request.");

            try
            {
                // Get the latest record for the user in this meeting
                var latest = await _context.MeetingAttendees
                    .Where(a => a.MeetingId == request.MeetingId && a.UserId == request.UserId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    if (latest.AttendanceStatus == "Present" || latest.AttendanceStatus == "Joined")
                    {
                        return Ok("User already joined.");
                    }

                    if (latest.AttendanceStatus == "Left")
                    {
                        // Update existing record
                        latest.AttendanceStatus = "Joined";
                        latest.Role = request.Role; // Update role if needed

                        await _context.SaveChangesAsync();
                        return Ok("User rejoined the meeting.");
                    }
                }

                // Create new attendee record
                var newAttendee = new MeetingAttendee
                {
                    MeetingId = request.MeetingId,
                    UserId = request.UserId,
                    AttendanceStatus = "Joined",
                    Role = request.Role,
                };

                await _context.MeetingAttendees.AddAsync(newAttendee);
                await _context.SaveChangesAsync();

                return Ok("User joined the meeting.");
            }
            catch (Exception ex)
            {
                // Log the error
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeetingAttendee(int id, MeetingAttendeeUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var attendee = await _context.MeetingAttendees.FindAsync(id);
            if (attendee == null)
                return NotFound();

            attendee.AttendanceStatus = dto.AttendanceStatus;
            attendee.Role = dto.Role;
            // Optionally update MeetingId and UserId if needed, but usually not

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeetingAttendeeExists(id))
                    return NotFound();
                else
                    throw;
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

            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);

            // 3. Fetch meeting details
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == createDto.MeetingId);

            if (meeting == null)
                return BadRequest($"Meeting {createDto.MeetingId} not found");

            // 4. Block adding attendee after meeting ends
            if (currentTime > meeting.EndTime)
                return BadRequest($"Cannot add attendees. Meeting already ended at {meeting.EndTime}.");

            // 5. Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == createDto.UserId);
            if (!userExists)
                return BadRequest($"User Id {createDto.UserId} not found");

            // 6. Prevent duplicate attendee
            bool alreadyAttending = await _context.MeetingAttendees
                .AnyAsync(a => a.MeetingId == createDto.MeetingId && a.UserId == createDto.UserId);
            if (alreadyAttending)
                return Conflict("User is already an attendee for this meeting.");

            // 7. Map DTO to entity
            var meetingAttendee = _mapper.Map<MeetingAttendee>(createDto);

            // 8. Add and save to DB
            _context.MeetingAttendees.Add(meetingAttendee);
            await _context.SaveChangesAsync();

            // 9. Map saved entity to response DTO
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
        [HttpDelete("delete-for-meeting/{meetingId}")]
        [Authorize]
        public async Task<IActionResult> DeleteAllAttendeesForMeeting(int meetingId)
        {
            try
            {
                // Get user ID from claims - using multiple possible claim types
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                             User.FindFirstValue("uid") ??
                             User.FindFirstValue("sub");

                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user identification",
                        error = "USER_ID_MISSING"
                    });
                }

                // First verify the meeting exists
                var meeting = await _context.Meetings
                    .Include(m => m.Agenda)
                    .Include(m => m.Minutes)
                    .Include(m => m.Notifications)
                    .Include(m => m.MeetingAttendees)
                    .FirstOrDefaultAsync(m => m.Id == meetingId);

                if (meeting == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "Meeting not found",
                        error = "MEETING_NOT_FOUND"
                    });

                // Check if user is organizer
                if (meeting.UserId != currentUserId)
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Only meeting organizer can delete attendees",
                        error = "NOT_AUTHORIZED"
                    });

                // Check if meeting hasn't started yet
                var meetingDateTime = meeting.MeetingDate.ToDateTime(meeting.StartTime);
                if (DateTime.Now >= meetingDateTime)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete meeting that has already started",
                        error = "MEETING_ALREADY_STARTED"
                    });

                // Delete all related entities
                if (meeting.Agenda?.Any() == true)
                    _context.Agenda.RemoveRange(meeting.Agenda);

                if (meeting.Minutes?.Any() == true)
                    _context.Minutes.RemoveRange(meeting.Minutes);

                if (meeting.Notifications?.Any() == true)
                    _context.Notifications.RemoveRange(meeting.Notifications);

                if (meeting.MeetingAttendees?.Any() == true)
                    _context.MeetingAttendees.RemoveRange(meeting.MeetingAttendees);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Meeting deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting meeting attendees",
                    error = "SERVER_ERROR",
                    details = ex.Message
                });
            }
        }
        private bool MeetingAttendeeExists(int id)
        {
            return _context.MeetingAttendees.Any(e => e.Id == id);
        }
    }
}
