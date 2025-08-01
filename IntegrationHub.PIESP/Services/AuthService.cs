using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Security;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



namespace IntegrationHub.PIESP.Services
{
    

    public class AuthService
    {
        private readonly PiespDbContext _context;

        public AuthService(PiespDbContext context)
        {
            _context = context;
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
            var user = await _context.Users
                .Include(u=>u.Roles)
                .FirstOrDefaultAsync(u => u.BadgeNumber == badge);
            if (user == null || string.IsNullOrEmpty(user.PinHash)) return null;

            return PinHasher.Verify(pin, user.PinHash) ? user : null;
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
