using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class Hospital
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hospital name is required.")]
        [Display(Name = "Hospital Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        public string Type { get; set; } = string.Empty;          // Government / Private / NGO

        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; } = string.Empty;      // e.g. Dhaka

        [Required(ErrorMessage = "Contact person is required.")]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty; // e.g. Dr. Rahman

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;         // e.g. 02-1234567

        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        public string? Email { get; set; }

        public string Status { get; set; } = "Active";            // Active / Inactive / Pending Approval

        [Display(Name = "Total Requests")]
        [Range(0, int.MaxValue)]
        public int Requests { get; set; } = 0;

        [Display(Name = "Units Supplied")]
        [Range(0, int.MaxValue)]
        public int UnitsSupplied { get; set; } = 0;

        public string? Notes { get; set; }

        // Auto-generated Hospital ID displayed in the table (e.g. HSP-0001)
        [Display(Name = "Hospital ID")]
        public string? HospitalCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}