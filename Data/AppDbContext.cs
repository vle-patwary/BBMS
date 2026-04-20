using BBMS.Models;  // ✅ Fix this to your actual project namespace
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YourProjectName.Models;

namespace BBMS.Data
{
    public class AppDbContext :IdentityDbContext<IdentityUser>   {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<DonateBlood> DonateBloods { get; set; }
        public DbSet<RecordDonation> RecordDonations { get; set; }
        public DbSet<RequestBlood> RequestBloods { get; set; }
    }
}