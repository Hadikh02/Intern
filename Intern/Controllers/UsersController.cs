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
    public class UsersController : ControllerBase
    {
        private readonly InternContext _context;

        public UsersController(InternContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }
            if (string.IsNullOrWhiteSpace(user.FirstName))
            {
                return BadRequest("First name is required");
            }
            if (string.IsNullOrWhiteSpace(user.LastName))
            {
                return BadRequest("Last name is required");
            }
            var checkEmail = await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id);
            if (checkEmail)
            {
                return BadRequest("Please choose another email address");
            }
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("Password is required");
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (string.IsNullOrEmpty(user.FirstName))
            {
                return BadRequest("User first name is required");
            }
            if (string.IsNullOrEmpty(user.LastName))
            {
                return BadRequest("User last name is required");

            }
            if (string.IsNullOrEmpty(user.Email))
            {
                return BadRequest("User email is required");
            }
            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                return BadRequest("Please choose another email.");
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("User password is required");
            }
            if (string.IsNullOrEmpty(user.UserType))
            {
                return BadRequest("User type is required");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var checkMeetRlt = await _context.Meetings.AnyAsync(meet => meet.UserId == user.Id);
            if(checkMeetRlt)
            {
                return BadRequest($"The user id {user.Id} cannot be deleted since it exists in other tables");
            }
           
           
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
