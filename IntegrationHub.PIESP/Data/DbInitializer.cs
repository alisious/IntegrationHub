using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Security;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.PIESP.Data
{
    public static class DbInitializer
    {
        public static void Seed(PiespDbContext context)
        {
            //This method is used to seed the database with initial data.
            

            // Usuń dane istniejące
            context.UserRoles.RemoveRange(context.UserRoles);
            context.Users.RemoveRange(context.Users);
            context.Duties.RemoveRange(context.Duties);
            context.SaveChanges();

            //Dodaj użytkowników
            var user1 = new User
            {
                UserName = "kpr. Jan Kowalski",
                BadgeNumber = "1111",
                PinHash = PinHasher.Hash("1111"),
                Roles = new List<UserRole>
                {
                    new UserRole { Role = RoleType.User }
                }
            };

            var user2 = new User
            {
                UserName = "mjr Tomasz Nowak",
                BadgeNumber = "2222",
                PinHash = PinHasher.Hash("2222"),
                Roles = new List<UserRole>
                {
                    new UserRole { Role = RoleType.User },
                    new UserRole { Role = RoleType.Supervisor }
                }
            };

            context.Users.AddRange(user1, user2);

            //Dodaj służby
            if (!context.Set<Duty>().Any())
            {
                context.AddRange(
                    new Duty
                    {
                        BadgeNumber = "1111",
                        Type = "Patrol pieszy",
                        PlannedStartDate = DateTime.Today,
                        PlannedStartTime = new TimeSpan(8, 0, 0),
                        Unit = "OŻW Bydgoszcz",
                        Status = DutyStatus.Planned
                    },
                    new Duty
                    {
                        BadgeNumber = "1111",
                        Type = "Patrol zapobiegawczy",
                        PlannedStartDate = DateTime.Today.AddDays(1),
                        PlannedStartTime = new TimeSpan(14, 0, 0),
                        Unit = "OŻW Bydgoszcz",
                        Status = DutyStatus.Planned
                    },
                    new Duty
                    {
                        BadgeNumber = "1111",
                        Type = "Kontrola ruchu",
                        PlannedStartDate = DateTime.Today.AddDays(2),
                        PlannedStartTime = new TimeSpan(6, 30, 0),
                        Unit = "OŻW Bydgoszcz",
                        Status = DutyStatus.Planned
                    },
                    new Duty
                    {
                        BadgeNumber = "1111",
                        Type = "Zabezpieczenie wydarzenia",
                        PlannedStartDate = DateTime.Today.AddDays(3),
                        PlannedStartTime = new TimeSpan(10, 0, 0),
                        Unit = "OŻW Bydgoszcz",
                        Status = DutyStatus.Planned
                    }
                );
            }



            context.SaveChanges();
        }
    }
}
