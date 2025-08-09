using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Student_Wellness_Journal_App.Models;
using System.Collections.Generic;

namespace Student_Wellness_Journal_App.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<JournalEntry> JournalEntries { get; set; }
    }
}
