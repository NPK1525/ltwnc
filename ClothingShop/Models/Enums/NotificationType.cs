namespace ClothingShop.Models.Enums
{
    public enum NotificationType
    {
        Success,
        Info,
        Warning,
        Danger
    }

    public static class NotificationTypeExtensions
    {
        public static string ToBootstrapClass(this NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "success",
                NotificationType.Info => "info",
                NotificationType.Warning => "warning",
                NotificationType.Danger => "danger",
                _ => "info"
            };
        }

        public static NotificationType FromString(string type)
        {
            return type?.ToLower() switch
            {
                "success" => NotificationType.Success,
                "info" => NotificationType.Info,
                "warning" => NotificationType.Warning,
                "danger" => NotificationType.Danger,
                _ => NotificationType.Info
            };
        }
    }
}
