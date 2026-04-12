using System.ComponentModel.DataAnnotations;

namespace BBMS.Models
{
    public class LogInViewModel
    {
      
        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress(ErrorMessage = "Check Email Format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
