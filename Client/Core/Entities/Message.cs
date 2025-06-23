using System.Text.Json.Serialization;
using Client.Core.Serializations;
using Toolkit.Blazor.Extensions.File.Entities;

namespace Client.Core.Entities;

/// <summary>
/// Represents a message entity with context and metadata.
/// </summary>
/// <param name="context">The binary content of the message</param>
/// <param name="direction">The direction of the message (default: Outgoing)</param>
public class Message(Binary context, MessageDirection direction = MessageDirection.Outgoing)
{
    /// <summary>
    /// Gets or sets the unique session identifier for the message.
    /// </summary>
    /// <remarks>
    /// Automatically initialized with a new GUID when the message is created.
    /// </remarks>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the timestamp of when the message was created.
    /// </summary>
    /// <remarks>
    /// Automatically set to the current UTC time when the message is created.
    /// </remarks>
    public DateTime DateTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets or sets the direction of the message (Outgoing or Incoming).
    /// </summary>
    /// <remarks>
    /// Defaults to Outgoing if not specified.
    /// </remarks>
    public MessageDirection Direction { get; set; } = direction;
    
    /// <summary>
    /// Gets or sets the delivery status of the message.
    /// </summary>
    /// <remarks>
    /// Defaults to Unknown when the message is created.
    /// </remarks>
    public MessageDeliveryStatus DeliveryStatus { get; set; } = MessageDeliveryStatus.Unknown;
    
    /// <summary>
    /// Gets or sets the binary content of the message.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="SizeLimitedJsonConverter"/> for JSON serialization,
    /// which enforces size limits on the binary data.
    /// </remarks>
    [JsonConverter(typeof(SizeLimitedJsonConverter))]
    public Binary? Context { get; set; } = context;
}

/// <summary>
/// Represents the delivery status of a message.
/// </summary>
public enum MessageDeliveryStatus
{
    /// <summary>
    /// The message status is unknown (default state).
    /// </summary>
    Unknown,
    
    /// <summary>
    /// The message has been sent but not yet acknowledged.
    /// </summary>
    Sent,
    
    /// <summary>
    /// The message has been received by the destination.
    /// </summary>
    Received
}

/// <summary>
/// Represents the direction of message flow in the system.
/// </summary>
public enum MessageDirection
{
    /// <summary>
    /// Message is being sent from the local system (outbound).
    /// </summary>
    Outgoing,
    
    /// <summary>
    /// Message is being received by the local system (inbound).
    /// </summary>
    Incoming
}