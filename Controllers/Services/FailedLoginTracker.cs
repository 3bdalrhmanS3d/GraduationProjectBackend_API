namespace GraduationProjectBackendAPI.Controllers.Services
{
    public class FailedLoginTracker
    {
       private readonly Dictionary<string, (int Attempts, DateTime LockoutEnd)> _failedLoginAttempts = new();

        public void RecordFailedAttempt(string email)
        {
            if (!_failedLoginAttempts.TryGetValue(email, out var attemptData))
            {
                attemptData = (0, DateTime.UtcNow);
            }

            _failedLoginAttempts[email] = (attemptData.Attempts + 1, DateTime.UtcNow);
        }

        public void LockUser(string email)
        {
            _failedLoginAttempts[email] = (5, DateTime.UtcNow.AddMinutes(15));
        }

        public void ResetFailedAttempts(string email)
        {
            _failedLoginAttempts.Remove(email);
        }

        public Dictionary<string, (int Attempts, DateTime LockoutEnd)> GetFailedAttempts()
        {
            return _failedLoginAttempts;
        }
        
    }
}
