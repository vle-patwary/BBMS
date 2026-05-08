using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BBMS.Models
{
    public class Hospital
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(20)]
        public string Contact { get; set; }

        [StringLength(300)]
        public string Address { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive
    }
}