using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    // Used by 'dotnet ef' and the PMC to create the DbContext for migrations.
    // Keeps migrations independent of WebApi Program.cs startup order.
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    // LocalDB
                    "Server=(localdb)\\MSSQLLocalDB;Database=PersonalMediaTrackerDb;Trusted_Connection=True;MultipleActiveResultSets=True;")
                .Options;

            return new AppDbContext(options);
        }
    }
}
