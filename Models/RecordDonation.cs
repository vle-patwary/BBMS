using System;
using System.ComponentModel.DataAnnotations;

namespace YourProjectName.Models
{
    public class RecordDonation
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // DONOR INFORMATION
        // =========================

        [Required]
        [Display(Name = "Donor ID")]
        public string DonorId { get; set; }

        [Required]
        [Display(Name = "Donor Name")]
        public string DonorName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; }

        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        // =========================
        // DONATION DETAILS
        // =========================

        [Required]
        public string BloodGroup { get; set; }

        [Required]
        public string DonationType { get; set; }

        [Required]
        [Range(100, 600)]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DonationDate { get; set; }

        [Required]
        public string Location { get; set; }

        public int? DonationNumber { get; set; }

        // =========================
        // HEALTH SCREENING
        // =========================

        [Range(5, 20)]
        public double? HemoglobinLevel { get; set; }

        public string BloodPressure { get; set; }

        [Range(45, 200)]
        public int? Weight { get; set; }

        public string ScreeningResult { get; set; }

        // =========================
        // PROCESSING
        // =========================

        [Display(Name = "Staff ID")]
        public string StaffId { get; set; }

        public string Status { get; set; }

        public string Remarks { get; set; }
    }
}