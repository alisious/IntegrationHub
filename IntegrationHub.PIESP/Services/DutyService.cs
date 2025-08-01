using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.PIESP.Services
{
    public class DutyService
    {
        private readonly PiespDbContext _context;

        public DutyService(PiespDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Duty> GetDutiesForUser(string badge, DateTime date)
        {
            return _context.Set<Duty>()
                .Where(d => d.BadgeNumber == badge && d.PlannedStartDate.Date == date.Date)
                .AsNoTracking()
                .ToList();
        }

        public Duty? GetDutyForUser(int dutyId, string badge)
        {
            return _context.Set<Duty>()
                .AsNoTracking()
                .FirstOrDefault(d => d.BadgeNumber == badge && d.Id == dutyId);
        }

        public Duty? GetCurrentDutyForUser(string badge)
        {
            return _context.Set<Duty>()
                .AsNoTracking()
                .FirstOrDefault(d => d.BadgeNumber == badge && d.Status == DutyStatus.InProgress);
        }




        public bool StartDuty(int id, DateTime dt)
        {
            var duty = _context.Set<Duty>().FirstOrDefault(d => d.Id == id && d.Status == DutyStatus.Planned);
            if (duty == null)
                return false;

            bool hasInProgress = _context.Set<Duty>()
                .Any(d => d.BadgeNumber == duty.BadgeNumber && d.Status == DutyStatus.InProgress);

            if (hasInProgress)
                throw new UserAlreadyOnDutyException(duty.BadgeNumber);

            duty.Status = DutyStatus.InProgress;
            duty.ActualStart = dt;
            _context.SaveChanges();
            return true;
        }

        public bool FinishDuty(int id, DateTime dt)
        {
            var duty = _context.Set<Duty>().FirstOrDefault(d => d.Id == id && d.Status == DutyStatus.InProgress);
            if (duty == null)
                return false;

            duty.Status = DutyStatus.Finished;
            duty.ActualEnd = dt;

            _context.SaveChanges();
            return true;
        }
    }
}
