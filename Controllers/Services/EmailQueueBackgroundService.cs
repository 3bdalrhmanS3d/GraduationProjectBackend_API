namespace GraduationProjectBackendAPI.Controllers.Services
{
    public class EmailQueueBackgroundService : BackgroundService
    {
        private readonly EmailQueueService _emailQueueService;
        public EmailQueueBackgroundService(EmailQueueService emailQueueService)
        {
            _emailQueueService = emailQueueService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _emailQueueService.ProcessQueueAsync();
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
