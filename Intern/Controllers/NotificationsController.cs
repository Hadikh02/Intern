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
    public class NotificationsController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;

        public NotificationsController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Notifications
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
        {
            return await _context.Notifications.ToListAsync();
        }

        // GET: api/Notifications/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            return notification;
        }
        // GET: api/Notifications/user/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotificationsByUser(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        [Authorize] // Still keep authorization to ensure valid users
        [HttpPut("mark-as-read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                // Find the notification by ID only
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new
                    {
                        Message = $"Notification with ID {id} not found",
                        Status = 404
                    });
                }

                // Update the notification without user verification
                notification.IsRead = true;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while updating the notification",
                    Error = ex.Message,
                    Status = 500
                });
            }
        }
        // PUT: api/Notifications/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNotification(int id, Notification notification)
        {
            if (id != notification.Id)
            {
                return BadRequest();
            }

            _context.Entry(notification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationExists(id))
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

        // POST: api/Notifications
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<NotificationDto>> PostNotification([FromBody] NotificationDto notificationDto)
        {
            if (string.IsNullOrWhiteSpace(notificationDto.EventType))
                return BadRequest("EventType is required.");

            if (string.IsNullOrWhiteSpace(notificationDto.EventDescription))
                return BadRequest("EventDescription is required.");

            if (notificationDto.MeetingId <= 0)
                return BadRequest("MeetingId is required and must be greater than 0.");
            if(notificationDto.UserID <=0)
                return BadRequest("UserId is required and must be greater than 0.");


            var meeting = await _context.Meetings.FindAsync(notificationDto.MeetingId);
            if (meeting == null)
                return BadRequest($"Meeting with Id {notificationDto.MeetingId} not found.");

            var user = await _context.Users.FindAsync(notificationDto.UserID);
            if (user == null)
                return BadRequest($"User with Id {notificationDto.UserID} not found.");


            var notification = _mapper.Map<Notification>(notificationDto);

            // Set CreatedAt if not set
            if (notification.CreatedAt == default)
                notification.CreatedAt = DateTime.UtcNow;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<NotificationDto>(notification);

            return CreatedAtAction("GetNotification", new { id = notification.Id }, resultDto);
        }


        // DELETE: api/Notifications/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NotificationExists(int id)
        {
            return _context.Notifications.Any(e => e.Id == id);
        }
    }
}
