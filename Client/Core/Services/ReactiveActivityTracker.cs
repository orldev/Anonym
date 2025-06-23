using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Client.Core.Services;

/// <summary>
/// Tracks activity state using Reactive Extensions (Rx) by monitoring a stream of data chunks.
/// </summary>
/// <typeparam name="T">The type of data chunks being tracked.</typeparam>
/// <remarks>
/// This tracker:
/// - Automatically sets <see cref="Enable"/> to true when activity is detected
/// - Automatically sets <see cref="Enable"/> to false after 5 seconds of inactivity
/// - Provides observable streams for both the data and enable state
/// - Implements proper disposal of resources
/// </remarks>
public class ReactiveActivityTracker<T> : IDisposable
{
    private readonly Subject<T> _subject = new();
    private readonly IDisposable _subscription;

    /// <summary>
    /// Occurs when the <see cref="Enable"/> state changes.
    /// </summary>
    public event Action? EnableChanged;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveActivityTracker{T}"/> class.
    /// </summary>
    /// <remarks>
    /// Sets up an observable pipeline that:
    /// 1. Starts with <see cref="Enable"/> = false
    /// 2. Sets to true immediately when activity is received
    /// 3. Automatically sets back to false after 5 seconds of inactivity
    /// 4. Only notifies when the state actually changes
    /// </remarks>
    public ReactiveActivityTracker()
    {
        var enableObservable = _subject
            .Select(_ => true) // Convert any activity to true
            .Merge(Observable.Return(false)) // Initial false state
            .Merge(_subject
                .Throttle(TimeSpan.FromSeconds(5)) // Reset to false after inactivity
                .Select(_ => false))
            .DistinctUntilChanged() // Only emit when state changes
            .Do(enable =>
            {
                Enable = enable;
                EnableChanged?.Invoke(); // Notify subscribers
            });
        
        _subscription = enableObservable.Subscribe();
    }

    /// <summary>
    /// Gets the current activity state.
    /// </summary>
    /// <value>
    /// true if activity occurred within the last 5 seconds; otherwise, false.
    /// </value>
    public bool Enable { get; private set; }

    /// <summary>
    /// Notifies the tracker of new activity.
    /// </summary>
    /// <param name="chunk">The data chunk representing the activity.</param>
    public void OnNext(T chunk) => _subject.OnNext(chunk);

    /// <summary>
    /// Gets an observable sequence of all data chunks being tracked.
    /// </summary>
    /// <returns>An <see cref="IObservable{T}"/> of the data stream.</returns>
    public IObservable<T> DataStream => _subject.AsObservable();

    /// <summary>
    /// Releases all resources used by the <see cref="ReactiveActivityTracker{T}"/>.
    /// </summary>
    public void Dispose()
    {
        _subscription.Dispose();
        _subject.Dispose();
    }
}