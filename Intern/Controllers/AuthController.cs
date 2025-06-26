using AutoMapper;
using Intern.DTOs;
using Intern.Models;
using Intern.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Intern.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly InternContext _context;
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService, InternContext context, IMapper mapper)
        {
            _authService = authService;
            _context = context;
            _mapper = mapper;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("register-auth")]
        public async Task<ActionResult<UserDto>> RegisterWithAuth([FromBody] UserDto userDto)
        {
            // Existing validation checks for FirstName, LastName...
            if (string.IsNullOrWhiteSpace(userDto.FirstName) || userDto.FirstName.Trim().ToLower() == "string")
                return BadRequest("First name is required");
            if (!Regex.IsMatch(userDto.FirstName, "^[A-Z][a-z]+$"))
                return BadRequest("First name must start with a capital letter followed by lowercase letters only (e.g., 'John').");


            if (string.IsNullOrWhiteSpace(userDto.LastName) || userDto.LastName.Trim().ToLower() == "string")
                return BadRequest("Last name is required");
            if(!Regex.IsMatch(userDto.LastName, "^[A-Z][a-z]+$"))
                return BadRequest("Last name must start with a capital letter followed by lowercase letters only (e.g., 'John').");


            // Enhanced email validation
            if (string.IsNullOrWhiteSpace(userDto.Email) || userDto.Email.Trim().ToLower() == "string")
                return BadRequest("User email is required");

            var email = userDto.Email.Trim();
            var emailValidationResult = ValidateEmail(email);
            if (!emailValidationResult.IsValid)
            {
                return BadRequest(emailValidationResult.ErrorMessage);
            }

            // Rest of your existing code...
            if (string.IsNullOrWhiteSpace(userDto.Password) || userDto.Password.Trim() == "string")
                return BadRequest("User password is required");

            // Password strength checks...
            bool passwordLength = userDto.Password.Length >= 8;
            bool hasUppercase = userDto.Password.Any(char.IsUpper);
            bool hasLowercase = userDto.Password.Any(char.IsLower);
            bool hasDigit = userDto.Password.Any(char.IsDigit);
            bool hasSpecialChar = userDto.Password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUppercase)
                return BadRequest("Your password must include at least one uppercase letter (A-Z) to make it stronger.");
            if (!hasLowercase)
                return BadRequest("Please include at least one lowercase letter (a-z) in your password.");
            if (!hasDigit)
                return BadRequest("Add at least one number (0-9) to your password for better security.");
            if (!hasSpecialChar)
                return BadRequest("Your password needs at least one special character (e.g., !, @, #, $) to be secure.");
            if (!passwordLength)
                return BadRequest("Your password must be at least 8 characters long.");
            
            if (string.IsNullOrWhiteSpace(userDto.UserType) || userDto.UserType.Trim() == "string")
                return BadRequest("User type is required");
            if (userDto.UserType != "Admin" && userDto.UserType != "Employee")
            {
                return BadRequest("UserType must be either 'Admin' or 'Employee'.");
            }


            var newUser = await _authService.RegisterAsync(userDto);
            if (newUser == null)
                return BadRequest("Choose another email.");

            var resultDto = _mapper.Map<UserDto>(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, resultDto);
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
            if (result == null || result.AccessToken == null || request.RefreshToken == null)
            {
                return Unauthorized("Invalid refresh token.");
            }
            return Ok(result);
        }
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("Authenticate")]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("you are authinticated");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("you are an admin");
        }
    }
}
