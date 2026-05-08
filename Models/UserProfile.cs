using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BBMS.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        // Links to ASP.NET Identity user
        public string IdentityUserId { get; set; }

        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }

        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string BloodGroup { get; set; }
        public string DonorStatus { get; set; } = "Available";
        public DateTime? LastDonationDate { get; set; }
    }
}