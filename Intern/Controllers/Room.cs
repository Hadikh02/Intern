using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms.ToListAsync();
        }

        // GET: api/Room/5
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

        [HttpPut("{id}")]
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        public async Task<IActionResult> PutRoom(int id, Room room)
        {
            if (id != room.Id)
            {
                return BadRequest();
            }
            if (string.IsNullOrEmpty(room.RoomNumber))
            {
                return BadRequest("Room number is required");
            }
            var roomNumberExists = await _context.Rooms
                .AnyAsync(r => r.RoomNumber == room.RoomNumber && r.Id != id);
            if (roomNumberExists)
            {
                return BadRequest($"The room number {room.RoomNumber} already exists");
            }
            if (string.IsNullOrEmpty(room.Location))
            {
                return BadRequest("Room location is required");
            }
            if (string.IsNullOrEmpty(room.Status))
            {
                return BadRequest("Room status is required");
            }
            if (room.Capacity <= 0)
            {
                return BadRequest("Room capacity is required and must be greater than 0");
            }

            _context.Entry(room).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(id))
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


        // POST: api/Room
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RoomDto>> PostRoom([FromBody] RoomDto roomDto)
        {
            if (string.IsNullOrEmpty(roomDto.RoomNumber))
                return BadRequest("Room number is required");

            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomNumber == roomDto.RoomNumber);
            if (roomExists)
                return BadRequest($"The room number {roomDto.RoomNumber} already exists");

            if (string.IsNullOrEmpty(roomDto.Location))
                return BadRequest("Room location is required");

            if (string.IsNullOrEmpty(roomDto.Status))
                return BadRequest("Room status is required");

            if (roomDto.Capacity <= 0)
                return BadRequest("Room capacity must be greater than 0");

            var room = _mapper.Map<Room>(roomDto);

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<RoomDto>(room);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, resultDto);
        }


        // DELETE: api/Room/5
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
