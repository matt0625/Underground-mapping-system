using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    public class UserDataContext : DbContext
    {
        // configuring the database we are using
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source = TubeTrekker.db");

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // create composite primary key for the user journey link table
            modelBuilder.Entity<UserJourneyLink>().HasKey(userjourneylink => new {userjourneylink.UserID, userjourneylink.JourneyID});
        }

        // properties of these classes are tables in the database
        public DbSet<User> Users { get; set; } // e.g. user is the schema of this table (the properties of user are the fields of the table)
        public DbSet<UserJourneyLink> Links { get; set; }

        public DbSet<UserJourneys> UserJourneys { get; set; }

        public DbSet<Fare> Fare { get; set; }

    }
}
