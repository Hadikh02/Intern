﻿using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Intern.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;

        public RoomController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Room
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms.ToListAsync();
        }

        // GET: api/Room/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            return room;
        }
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoom(int id, [FromBody] RoomDto roomDto)
        {
            if (id != roomDto.Id)
                return BadRequest("Room ID mismatch.");

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound();

            // Validation
            if (string.IsNullOrEmpty(roomDto.RoomNumber))
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        RoomNumber = new[] { "Room number is required" }
                    }
                });

            bool isValidRoomNumber = Regex.IsMatch(roomDto.RoomNumber, @"^[A-Z]{1}\d+$");
            if (!isValidRoomNumber)
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        RoomNumber = new[] { "Invalid room number format. It should start with an uppercase letter followed by digits, like 'A102'." }
                    }
                });

            var roomNumberExists = await _context.Rooms.AnyAsync(r => r.RoomNumber == roomDto.RoomNumber && r.Id != id);
            if (roomNumberExists)
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        RoomNumber = new[] { $"The room number {roomDto.RoomNumber} already exists" }
                    }
                });

            if (string.IsNullOrEmpty(roomDto.Location))
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        Location = new[] { "Room location is required" }
                    }
                });

            if (!Regex.IsMatch(roomDto.Location, @"^\d+[A-Za-z]*\s[A-Za-z]+$"))
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        Location = new[] { "Location must start with a number followed by a space and letters (e.g., '1st Floor')." }
                    }
                });

            if (roomDto.Capacity <= 0)
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = new
                    {
                        Capacity = new[] { "Room capacity must be greater than 0" }
                    }
                });

            // Update all fields
            room.RoomNumber = roomDto.RoomNumber;
            room.Location = roomDto.Location;
            room.Capacity = roomDto.Capacity;
            room.HasVideo = roomDto.HasVideo;
            room.HasProjector = roomDto.HasProjector;

            _context.Entry(room).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/Room
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<RoomDto>> PostRoom([FromBody] RoomDto roomDto)
        {
            if (string.IsNullOrWhiteSpace(roomDto.RoomNumber))
                return BadRequest("Room number is required");

            bool isValidRoomNumber = Regex.IsMatch(roomDto.RoomNumber, @"^[A-Z]{1}\d+$");

            if (isValidRoomNumber == false)
            {
                return BadRequest("Invalid room number format. It should start with an uppercase letter followed by digits, like 'A102'.");
            }


            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomNumber == roomDto.RoomNumber);
            if (roomExists)
                return BadRequest($"The room number {roomDto.RoomNumber} already exists");

            if (string.IsNullOrWhiteSpace(roomDto.Location))
                return BadRequest("Room location is required");

            if (!Regex.IsMatch(roomDto.Location, @"^\d+[A-Za-z]*\s[A-Za-z]+$"))
            {
                return BadRequest("Location must start with a number followed by a space and letters (e.g., '1st Floor').");
            }

            if (roomDto.Capacity <= 0)
                return BadRequest("Room capacity must be greater than 0");

            var room = _mapper.Map<Room>(roomDto);

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<RoomDto>(room);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, resultDto);
        }


        // DELETE: api/Room/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var checkMeet = await _context.Meetings.AnyAsync(meet => meet.RoomId ==  room.Id);
            if (checkMeet)
            {
                return BadRequest($"Room with id {room.Id} cannot be deleted since it is booked");
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}
