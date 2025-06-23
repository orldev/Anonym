namespace Client.Views.Templates;

/// <summary>
/// Provides notification management and expiration functionality.
/// </summary>
/// <remarks>
/// This class:
/// - Maintains a collection of active notifications
/// - Automatically removes expired notifications
/// - Supports manual notification removal
/// - Implements automatic refresh notifications
/// - Disposes of resources properly
/// </remarks>
public class NotificationProvider : IDisposable
{
    /// <summary>
    /// Gets the list of active notifications.
    /// </summary>
    public readonly List<Notification> Notifications = [];
    
    /// <summary>
    /// Event that triggers when the notification state changes.
    /// </summary>
    public Func<Task>? OnRefresh;

    private readonly System.Timers.Timer _timer;
    private readonly int _secondsToLive;
    
    /// <summary>
    /// Initializes a new instance of the NotificationProvider.
    /// </summary>
    /// <param name="secondsToLive">Default lifetime for notifications in seconds (default: 4).</param>
    /// <param name="updateStateInterval">Interval for checking expired notifications (defaults to secondsToLive).</param>
    public NotificationProvider(int secondsToLive = 4, TimeSpan? updateStateInterval = null)
    {
        _secondsToLive = secondsToLive;
        _timer = new System.Timers.Timer(updateStateInterval ?? TimeSpan.FromSeconds(secondsToLive));
        _timer.Elapsed += ( sender, e ) => RemoveExpired();
    }
    
    /// <summary>
    /// Adds a new notification to the collection.
    /// </summary>
    /// <param name="value">The notification to add.</param>
    /// <remarks>
    /// Starts the expiration timer if not already running.
    /// Triggers a state change notification.
    /// </remarks>
    public void OnBuild(Notification value)
    {
        Notifications.Add(value);
        _timer.Start();
        StateHasChanged();
    }
    
    /// <summary>
    /// Removes a specific notification from the collection.
    /// </summary>
    /// <param name="value">The notification to remove.</param>
    /// <remarks>
    /// Triggers a state change notification.
    /// </remarks>
    public void OnRemove(Notification value)
    {
        Notifications.Remove(value);
        StateHasChanged();
    }
    
    /// <summary>
    /// Removes notifications that have exceeded their lifetime.
    /// </summary>
    /// <remarks>
    /// Stops the timer if no notifications remain.
    /// Triggers a state change notification.
    /// </remarks>
    private void RemoveExpired()
    {
        for (var i = Notifications.Count - 1; i >= 0; i--)
        {
            var difference = DateTime.Now - Notifications[i].CreatedAt;
            
            if (difference >= TimeSpan.FromSeconds(_secondsToLive))
                Notifications.RemoveAt(i);
        }
        
        if (Notifications.Count == 0)
            _timer.Stop();

        StateHasChanged();
    }
    
    /// <summary>
    /// Notifies subscribers of state changes.
    /// </summary>
    private void StateHasChanged() => OnRefresh?.Invoke();

    /// <summary>
    /// Disposes of the timer resource.
    /// </summary>
    public void Dispose() => _timer.Dispose();
}

/// <summary>
/// Provides standard notification messages.
/// </summary>
public static class NotificationText
{
    /// <summary>
    /// Authentication failure message for security-related notifications.
    /// </summary>
    public const string AuthenticationFailure = "Authentication failed on first attempt. In accordance with security best practices, the system will perform a complete data refresh to maintain system integrity.";

    /// <summary>
    /// Resource loading failure message for connection issues.
    /// </summary>
    public const string LoadResourceFailure = "Failed to load resource: Failed to connect to the server.\"";
}

/// <summary>
/// Represents a single notification message.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the notification message content.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the notification.
    /// Defaults to <see cref="NotificationLevel.Info"/>.
    /// </summary>
    public NotificationLevel Level { get; set; } = NotificationLevel.Info;
    
    /// <summary>
    /// Gets the creation timestamp of the notification.
    /// Automatically set to the current time when created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Defines the severity levels for notifications.
/// </summary>
public enum NotificationLevel
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Success confirmation.
    /// </summary>
    Success,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error condition.
    /// </summary>
    Error
}