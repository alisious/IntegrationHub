using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Data;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.PIESP.Services
{
    public class SupervisorService
    {
        private readonly PiespDbContext _context;

        public SupervisorService(PiespDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateCodeAsync(string badge)
        {
            var code = new Random().Next(100000, 999999).ToString();
            var entry = new SecurityCode { BadgeNumber = badge, Code = code, Expiry = DateTime.Now.AddMinutes(10) };
            _context.SecurityCodes.Add(entry);
            await _context.SaveChangesAsync();
            return code;
        }

        public async Task<bool> ValidateSecurityCodeAsync(string badge, string securityCode)
        {
            var entry = await _context.SecurityCodes
                .FirstOrDefaultAsync(c => c.BadgeNumber == badge && c.Code == securityCode && c.Expiry > DateTime.Now);

            if (entry == null) return false;

            _context.SecurityCodes.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoleAsync(string badgeNumber, RoleType role)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.BadgeNumber == badgeNumber);

            if (user == null)
                return false;

            if (user.Roles.Any(r => r.Role == role))
                return true; // already has this role

            user.Roles.Add(new UserRole { Role = role });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeRoleAsync(string badgeNumber, RoleType role)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.BadgeNumber == badgeNumber);

            if (user == null)
                return false;

            var roleToRemove = user.Roles.FirstOrDefault(r => r.Role == role);
            if (roleToRemove == null)
                return false;

            user.Roles.Remove(roleToRemove);
            _context.UserRoles.Remove(roleToRemove);
            await _context.SaveChangesAsync();
            return true;
        }


    }
}
