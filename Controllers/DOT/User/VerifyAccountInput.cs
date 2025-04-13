using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.User
{
    public class VerifyAccountInput
    {
        [Required]
        public string VerificationCode { get; set; }
    }
}
