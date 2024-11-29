using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApp.Data;
using SecureApp.DTOs;
using SecureApp.Helpers;
using SecureApp.Models;
using SecureApp.Services;
using Microsoft.Extensions.Logging;

namespace SecureApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationService _validationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, JwtHelper jwtHelper, ValidationService validationService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _validationService = validationService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            if (!_validationService.IsUsernameValid(userDto.Username))
                return BadRequest(new { message = "El nombre de usuario solo puede contener letras y números." });

            if (!_validationService.IsPasswordStrong(userDto.Password))
                return BadRequest(new { message = "La contraseña no cumple con los requisitos de seguridad. La contraseña debe tener al menos 8 caracteres, incluyendo al menos una letra mayúscula, una letra minúscula, un número y un carácter especial (como @, $, !, %, , ?, &)." });

            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                return BadRequest(new { message = "El nombre de usuario ya existe." });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = hashedPassword,
                RoleId = 2,
                FailedLoginAttempts = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario registrado exitosamente: {userDto.Username}");
            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserDto userDto)
        {
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == userDto.Username);

            if (user == null)
            {
                _logger.LogWarning($"Intento de inicio de sesión fallido para usuario: {userDto.Username}");
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            if (user.FailedLoginAttempts >= 5)
            {
                _logger.LogWarning($"Usuario bloqueado temporalmente: {user.Username}");
                return BadRequest(new { message = "Cuenta bloqueada temporalmente. Intenta más tarde." });
            }

            if (!BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                await _context.SaveChangesAsync();
                _logger.LogWarning($"Intento fallido para usuario: {user.Username}. Intentos fallidos: {user.FailedLoginAttempts}");
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var token = _jwtHelper.GenerateToken(user, ipAddress);

            _logger.LogInformation($"Inicio de sesión exitoso para usuario: {user.Username}");
            return Ok(new AuthResponseDto { Token = token, Role = user.Role.Name });
        }
    }
}
