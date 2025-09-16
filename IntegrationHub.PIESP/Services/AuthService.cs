using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Security;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;



namespace IntegrationHub.PIESP.Services
{
    

    public class AuthService
    {
        private readonly PiespDbContext _context;
        private ILogger<AuthService> _logger;

        public AuthService(PiespDbContext context,ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }


        

        public async Task<User?> SetPinAsync(string badge, string newPin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.BadgeNumber == badge);
            if (user == null) return null;
            user.PinHash = PinHasher.Hash(newPin);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string badge, string pin)
        {
            
            try
            {
                _logger.LogInformation("Attempting login for badge: {BadgeNumber}", badge);
                var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.BadgeNumber == badge);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: Nie znaleziono użytkownika z numerem odznaki: {BadgeNumber}.", badge);
                    return null;
                }

                if (PinHasher.Verify(pin, user.PinHash))
                {
                    _logger.LogInformation("Login successful for badge: {BadgeNumber}", badge);
                    return user;
                }
                else
                {
                    _logger.LogWarning("Login failed: Nieprawidłowy kod PIN dla numeru odznaki: {BadgeNumber}.", badge);
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logging failed");
            }
            return null;




        }

        

        public string GenerateJwtToken(User user, string jwtKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.BadgeNumber),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            foreach (var role in user.Roles.Select(r=>r.Role.ToString()))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
        
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


    }
}
