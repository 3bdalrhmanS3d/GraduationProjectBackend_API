using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace GraduationProjectBackendAPI.Controllers.DOT.User
{
    public class PaymentRequestModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public string? TransactionId { get; set; }
    }
}
