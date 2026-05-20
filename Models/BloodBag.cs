using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class BloodBag
    {
        public int Id { get; set; }

        public string BagCode { get; set; } = string.Empty;

        [Required]
        public string BloodGroup { get; set; } = string.Empty;

        [Required]
        public string Component { get; set; } = string.Empty;
        // "Whole Blood", "Red Blood Cells", "Platelets", 
        // "Fresh Frozen Plasma", "Granulocytes"

        [Required]
        public int VolumeML { get; set; } = 450;

        [Required]
        public DateTime CollectionDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int DaysUntilExpiry =>
            (ExpiryDate.Date - DateTime.Today).Days;

        public string ExpiryStatus =>
            DaysUntilExpiry <= 0 ? "Expired" :
            DaysUntilExpiry <= 3 ? "Critical" :
            DaysUntilExpiry <= 7 ? "Expiring Soon" : "Good";

        public string? DonorId { get; set; }  // optional link to donor

        [Required]
        public string Status { get; set; } = "Available";
        // "Available", "Used", "Expired", "Discarded"

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}