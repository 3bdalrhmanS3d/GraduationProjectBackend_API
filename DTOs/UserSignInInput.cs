using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTOs
{
    public class UserSignInInput
    {
        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }
}
