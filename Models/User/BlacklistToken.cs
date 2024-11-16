namespace GraduationProjectBackendAPI.Models.User
{
    public class BlacklistToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
