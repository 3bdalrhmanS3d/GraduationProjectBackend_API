namespace GraduationProjectBackendAPI.DTO.User
{
    public enum NotificationTemplateType
    {
        AccountActivated,
        AccountDeactivated,
        AccountDeleted,
        AccountRestored,
        GeneralAnnouncement
    }

    public class AdminSendNotificationInput
    {
        public int UserId { get; set; }
        public NotificationTemplateType? TemplateType { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}
