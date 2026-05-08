// Models/DonateBlood.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BBMS.Models
{
    public class DonateBlood
    {
        public int Id { get; set; }

        public string DonorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Blood Group is required")]
        public string BloodGroup { get; set; }

        public DateTime? LastDonationDate { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Contact Number is required")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Must be 11 digits")]
        public string ContactNumber { get; set; }
        [Required(ErrorMessage = "Age is required")]
        public int Age { get; set; }

        public int TotalDonations { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Not stored in DB — calculated automatically
        [NotMapped]
        public string Eligibility
        {
            get
            {
                if (LastDonationDate == null)
                    return "Eligible"; // Never donated before = eligible

                var daysSince = (DateTime.Now - LastDonationDate.Value).TotalDays;
                return daysSince >= 90 ? "Eligible" : "Deferred";
            }
        }
    }
}