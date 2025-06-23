namespace Client.Core.Entities;

/// <summary>
/// Represents a conversation dialogue between users in the system.
/// </summary>
public class Dialogue
{
    /// <summary>
    /// Gets or sets the unique identifier of the user participating in the dialogue.
    /// </summary>
    /// <remarks>
    /// This is a required field that uniquely identifies the user in the system.
    /// </remarks>
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user participating in the dialogue.
    /// </summary>
    /// <remarks>
    /// This field is optional and may be used for display purposes.
    /// If not provided, the system may use an alternative identifier.
    /// </remarks>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the list of messages exchanged in the dialogue.
    /// </summary>
    /// <remarks>
    /// The messages are stored in chronological order, with the most recent message
    /// typically appearing at the end of the collection.
    /// The collection is initialized as an empty list by default.
    /// </remarks>
    public List<Message> Messages { get; set; } = [];
}