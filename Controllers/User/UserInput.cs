using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.User
{
    public class UserInput
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [Compare("PasswordHash", ErrorMessage = "The fields Password and PasswordConfirmation should be equals")]
        public string userConfPassword { get; set; }
    }

}
