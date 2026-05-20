using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class BloodAllocation
    {
        public int Id { get; set; }

        public int BloodRequestId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public string RequesterName { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = string.Empty;
        public string Hospital { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        public string AllocatedBagCodes { get; set; } = string.Empty;
        public int QuantityAllocated { get; set; }

        public string Status { get; set; } = "Ready for Delivery";
        // "Ready for Delivery", "Delivered", "Cancelled"

        public DateTime AllocatedAt { get; set; } = DateTime.Now;
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveredBy { get; set; }
        public string? Notes { get; set; }
    }
}