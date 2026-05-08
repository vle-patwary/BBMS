using BBMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YourApp.Models;

namespace BBMS.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<DonateBlood> DonateBloods { get; set; }
        public DbSet<RecordDonation> RecordDonations { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
    }
}