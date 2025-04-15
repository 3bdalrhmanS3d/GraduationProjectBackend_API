using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.User
{
    public class UserForgetPassInput
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }
    }
}
