using System;
using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        public string Contact { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime JoinDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; }
    }
}