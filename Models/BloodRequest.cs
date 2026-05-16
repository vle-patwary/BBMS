using System;
using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class BloodRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequesterName { get; set; } = "";

        [Required]
        public string BloodGroup { get; set; } = "";

        [Required]
        public DateTime RequiredDate { get; set; }

        public string? Hospital { get; set; }
        public string? ContactNumber { get; set; }
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public string? MobileNumber { get; set; }

        public int Quantity { get; set; } = 1;

        // Server-side auto-filled
        public string InvoiceNumber { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "Pending";

        // Units (default 1)
        public int UnitsRequired { get; set; } = 1;

        // Total cost = Quantity * 165
        public int TotalCost { get; set; } = 165;
        public string? IdentityUserId { get; set; } // ← add this
    }
}