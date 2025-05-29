using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTO.User
{
    public class VerifyAccountInput
    {
        [Required]
        public string VerificationCode { get; set; }
    }
}
