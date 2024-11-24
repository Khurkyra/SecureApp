using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApp.Data;
using SecureApp.DTOs;
using SecureApp.Helpers;
using SecureApp.Models;
using BCrypt.Net;

namespace SecureApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;
        public AuthController(AppDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                return BadRequest(new { message = "Username already exists" });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = hashedPassword,
                RoleId = 2 // Default role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User registered successfully" });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserDto userDto)
        {
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == userDto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _jwtHelper.GenerateToken(user);
            return Ok(new AuthResponseDto { Token = token, Role = user.Role.Name });
        }
    }
}
    
