using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class BloodStock
    {
        public int Id { get; set; }

        [Required]
        public string BloodGroup { get; set; }

        [Required]
        public int Units { get; set; }

        [Required]
        public string Status { get; set; }  // "sufficient", "low", "critical"

        [Required]
        public DateTime CollectionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? Notes { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}