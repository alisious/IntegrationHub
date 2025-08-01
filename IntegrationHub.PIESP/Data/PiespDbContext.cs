using Microsoft.EntityFrameworkCore;
using IntegrationHub.PIESP.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IntegrationHub.PIESP.Data
{
    public class PiespDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<SecurityCode> SecurityCodes { get; set; }
        public DbSet<Duty> Duties { get; set; }

        public PiespDbContext(DbContextOptions<PiespDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Wymuszenie unikalności BadgeNumber
            modelBuilder.Entity<User>()
                .HasIndex(u => u.BadgeNumber)
                .IsUnique();
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.Roles)
                .HasForeignKey(ur => ur.UserId);

        }
    }
}

