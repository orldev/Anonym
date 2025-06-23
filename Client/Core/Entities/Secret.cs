using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Client.Core.Extensions;

namespace Client.Core.Entities;

/// <summary>
/// Represents a sensitive secret container with validation, lifecycle management, and security features.
/// </summary>
/// <remarks>
/// This class handles sensitive information including secrets, unique identifiers,
/// and access tokens with proper validation and security constraints.
/// </remarks>
public class Secret
{
    /// <summary>
    /// Gets or sets the storage key used for persistence.
    /// </summary>
    /// <value>
    /// Default value is "secret". Change this to isolate secrets in different contexts.
    /// </value>
    /// <remarks>
    /// This value is ignored during JSON serialization for security.
    /// </remarks>
    [JsonIgnore]
    public static string Key { get; set; } = "secret";
    
    /// <summary>
    /// Gets or sets the lifecycle policy for this secret.
    /// </summary>
    /// <value>
    /// <see cref="Lifecycle.Temporary"/> for short-lived secrets (recommended)
    /// <see cref="Lifecycle.Boundless"/> for secrets without expiration (use cautiously)
    /// </value>
    public Lifecycle Lifecycle { get; set; }
    
    /// <summary>
    /// Gets or sets the expiration timestamp for temporary secrets.
    /// </summary>
    /// <value>
    /// <para>
    /// UTC timestamp when this secret will automatically expire.
    /// Defaults to 8 hours from creation time for new instances.
    /// </para>
    /// <para>
    /// Ignored when <see cref="Lifecycle"/> is set to <see cref="Lifecycle.Boundless"/>.
    /// </para>
    /// </value>
    /// <remarks>
    /// <security>
    /// Important:
    /// - Always use UTC to prevent timezone bypass attacks
    /// - Minimum recommended duration: 1 hour
    /// - Maximum recommended duration: 24 hours
    /// </security>
    /// </remarks>
    public DateTime Expiration { get; set; } = DateTime.UtcNow.AddHours(8);
    
    /// <summary>
    /// Gets or sets the secret value
    /// </summary>
    /// <value>
    /// The secret string value, which must be between 8 and 16 characters in length.
    /// </value>
    /// <example>
    /// A valid secret might look like: "MyS3cr3tP@ss"
    /// </example>
    [Required(ErrorMessage = "the field is required")]
    [StringLength(16, MinimumLength = 8, ErrorMessage = "must be between 8 and 16 characters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier associated with this secret.
    /// </summary>
    /// <value>
    /// Automatically initialized with a random alphanumeric string prefixed with '#'.
    /// Default length is 11 characters (including the '#' prefix).
    /// </value>
    /// <example>
    /// Auto-generated example: "#a3b7c9d2e1f"
    /// </example>
    [RegularExpression(@"^#[a-z0-9]{11}$", ErrorMessage = "must be # followed by 11 alphanumeric characters")]
    public string UserIdentifier { get; set; } = StringExtension.GetRandomName();
    
    /// <summary>
    /// Gets or sets the JWT access token (if applicable).
    /// </summary>
    /// <value>
    /// A JSON Web Token in standard format. This field is optional.
    /// Note: Tokens should be handled securely and not logged.
    /// </value>
    /// <remarks>
    /// <security>
    /// Warning: This field contains sensitive authentication material.
    /// Always use HTTPS for transmission and secure storage.
    /// </security>
    /// </remarks>
    public string? AccessToken { get; set; }
}

/// <summary>
/// Defines the lifecycle policy for secrets
/// </summary>
public enum Lifecycle
{
    /// <summary>
    /// Temporary secret (recommended)
    /// </summary>
    /// <remarks>
    /// Should be revoked/rotated after single use or short time period
    /// </remarks>
    Temporary,

    /// <summary>
    /// Persistent secret without expiration
    /// </summary>
    /// <remarks>
    /// Use only for legacy systems that cannot handle secret rotation.
    /// Requires additional monitoring for compromise.
    /// </remarks>
    Boundless
}
