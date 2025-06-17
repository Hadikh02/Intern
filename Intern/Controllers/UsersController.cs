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
        [AllowAnonymous]
        [HttpPost("register-auth")]
        public async Task<ActionResult<UserDto>> RegisterWithAuth([FromBody] UserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.FirstName) || userDto.FirstName.Trim().ToLower() == "string")
                return BadRequest("User first name is required");

            if (string.IsNullOrWhiteSpace(userDto.LastName) || userDto.LastName.Trim().ToLower() == "string")
                return BadRequest("User last name is required");

            if (string.IsNullOrWhiteSpace(userDto.Email) || userDto.Email.Trim().ToLower() == "string")
                return BadRequest("User email is required");

            if (string.IsNullOrWhiteSpace(userDto.Password) || userDto.Password.Trim() == "string")
                return BadRequest("User password is required");

            if (string.IsNullOrWhiteSpace(userDto.UserType) || userDto.UserType.Trim() =="string")
                return BadRequest("User type is required");

            var newUser = await _authService.RegisterAsync(userDto);

            if (newUser == null)
                return BadRequest("Email already exists.");

            var resultDto = _mapper.Map<UserDto>(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, resultDto);
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || loginDto.Email.Trim().ToLower() == "string")
                return BadRequest("User email is required");

            if (string.IsNullOrWhiteSpace(loginDto.Password) || loginDto.Password.Trim() == "string")
                return BadRequest("User password is required");

            var result = await _authService.LoginAsync(loginDto);
            if (result == null) return BadRequest("Invalid email or password");
            return Ok(result);
        }
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if(result == null || result.AccessToken == null || request.RefreshToken == null)
            {
                return Unauthorized("Invalid refresh token.");
            }
            return Ok(result);
        }

        [Authorize]
        [HttpGet("Authenticate")]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("you are authinticated");
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("you are an admin");
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
