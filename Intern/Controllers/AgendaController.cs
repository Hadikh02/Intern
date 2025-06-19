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
    public class AgendaController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;

        public AgendaController(InternContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Agenda
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agenda>>> GetAgenda()
        {
            return await _context.Agenda.ToListAsync();
        }

        // GET: api/Agenda/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Agenda>> GetAgenda(int id)
        {
            var agenda = await _context.Agenda.FindAsync(id);

            if (agenda == null)
            {
                return NotFound();
            }

            return agenda;
        }

        // PUT: api/Agenda/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgenda(int id, Agenda agenda)
        {
            if (id != agenda.Id)
            {
                return BadRequest();
            }

            _context.Entry(agenda).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgendaExists(id))
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

        // POST: api/Agenda
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<Agenda>> PostAgenda([FromBody] AgendaDto agendaDto)
        {
            if (string.IsNullOrWhiteSpace(agendaDto.Topic))
                return BadRequest("Topic is required");

            if (string.IsNullOrWhiteSpace(agendaDto.Description))
                return BadRequest("Description is required");

            if (string.IsNullOrWhiteSpace(agendaDto.Status))
                return BadRequest("Status is required");

            if (string.IsNullOrWhiteSpace(agendaDto.ItemNumber))
                return BadRequest("ItemNumber is required");

            if (agendaDto.MeetingId <= 0)
                return BadRequest("MeetingId is required and must be greater than 0");
            var meetingExists = await _context.Meetings.FindAsync(agendaDto.MeetingId);
            if (meetingExists == null)
                return BadRequest($"Meeting with Id {agendaDto.MeetingId} not found");


            var agenda = _mapper.Map<Agenda>(agendaDto);

            _context.Agenda.Add(agenda);
            await _context.SaveChangesAsync();

            // Map back to DTO to return
            var resultDto = _mapper.Map<AgendaDto>(agenda);

            return CreatedAtAction("GetAgenda", new { id = agenda.Id }, resultDto);
        }

        // DELETE: api/Agenda/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgenda(int id)
        {
            var agenda = await _context.Agenda.FindAsync(id);
            if (agenda == null)
            {
                return NotFound();
            }

            _context.Agenda.Remove(agenda);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AgendaExists(int id)
        {
            return _context.Agenda.Any(e => e.Id == id);
        }
    }
}
