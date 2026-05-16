using System;
using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        public string Contact { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public DateTime JoinDate { get; set; } = DateTime.Now;

        // Links to ASP.NET Identity user
        public string? IdentityUserId { get; set; }
    }
}