using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Intern.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Intern.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;
        public UsersController(IAuthService authService, InternContext context, IMapper mapper)
        {
            _authService = authService;
            _context = context;
            _mapper = mapper;
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
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
                return NotFound("User email not found in token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }
        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserDto userDto)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            // Validations (same as before)
            if (string.IsNullOrWhiteSpace(userDto.FirstName) || !Regex.IsMatch(userDto.FirstName, "^[A-Z][a-z]+$"))
                return BadRequest("First name is invalid.");

            if (string.IsNullOrWhiteSpace(userDto.LastName) || !Regex.IsMatch(userDto.LastName, "^[A-Z][a-z]+$"))
                return BadRequest("Last name is invalid.");

            var email = userDto.Email.Trim();
            if (await _context.Users.AnyAsync(u => u.Email == email && u.Id != id))
                return BadRequest("Please choose another email address");

            var emailValidationResult = ValidateEmail(email);
            if (!emailValidationResult.IsValid)
                return BadRequest(emailValidationResult.ErrorMessage);

            // Update allowed fields only
            existingUser.FirstName = userDto.FirstName;
            existingUser.LastName = userDto.LastName;
            existingUser.Email = email;

            await _context.SaveChangesAsync();
            return NoContent();
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
        private (bool IsValid, string ErrorMessage) ValidateEmail(string email)
        {
            // 1. Basic format check (must contain @ and a dot in domain)
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!regex.IsMatch(email))
            {
                return (false, "Invalid email format. Example: yourname@example.com");
            }

            // 2. Extract TLD (text after the last dot)
            string tld = email.Split('.').Last().ToLower();

            // 3. Check if TLD is valid (use a predefined list)
            if (!IsValidTld(tld))
            {
                return (false, $"Invalid domain extension (.{tld}). Use a valid one like .com, .net, .org, etc.");
            }

            // 4. MailAddress validation (strict check)
            try
            {
                var mailAddress = new MailAddress(email);
                if (mailAddress.Address != email)
                {
                    return (false, "Email contains invalid characters.");
                }

                // 5. Additional checks (optional)
                if (email.Contains("..") || email.StartsWith(".") || email.EndsWith("."))
                {
                    return (false, "email cannot have consecutive, leading, or trailing dots (.)");
                }

                return (true, null);
            }
            catch
            {
                return (false, "Email address is not valid. Please check and try again.");
            }
        }

        // Check if TLD is valid (use a predefined list)
        private bool IsValidTld(string tld)
        {
            // List of valid TLDs (you can expand this)
            var validTlds = new HashSet<string>
    {
        // Generic TLDs
        "com", "net", "org", "io", "co", "gov", "edu", "info", "biz",
        // Country-code TLDs
        "uk", "us", "ca", "au", "de", "fr", "in", "jp", "br", "mx"
    };

            return validTlds.Contains(tld);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
