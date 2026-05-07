// Models/RecordDonation.cs
using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class RecordDonation
    {
        public int Id { get; set; }

        // Linked to DonateBlood
        [Required]
        public string DonorId { get; set; } = string.Empty;

        // Pre-filled from DonateBloods
        [Required]
        public string DonorName { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required]
        public string BloodGroup { get; set; } = string.Empty;

        // Donation Details
        [Required]
        public string DonationType { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime DonationDate { get; set; } = DateTime.Now;

        [Required]
        public string Location { get; set; } = string.Empty;

        public int DonationNumber { get; set; } = 1;

        // Health Screening
        public decimal? HemoglobinLevel { get; set; }
        public string BloodPressure { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public string ScreeningResult { get; set; } = string.Empty;

        // Processing
        public string StaffId { get; set; } = string.Empty;
        public string Status { get; set; } = "Collected";
        public string Remarks { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}