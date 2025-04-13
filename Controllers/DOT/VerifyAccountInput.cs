using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT
{
    public class VerifyAccountInput
    {
        [Required]
        public string VerificationCode { get; set; }
    }
}
