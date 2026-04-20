using System;
using System.ComponentModel.DataAnnotations;

namespace YourProjectName.Models
{
    public class DonateBlood
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Blood group is required")]
        public string BloodGroup { get; set; } 

        [DataType(DataType.Date)]
        [Display(Name = "Last Donation Date")]
        public DateTime? LastDonationDate { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter valid BD number")]
        public string ContactNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ✅ Computed Property (NOT stored in DB)
        public bool IsActive
        {
            get
            {
                if (LastDonationDate == null)
                    return true; // Never donated → eligible

                return (DateTime.Now - LastDonationDate.Value).TotalDays >= 90;
            }
        }
    }
}