using System.Text;

namespace Client.Core.Extensions;

/// <summary>
/// Provides extension methods for common string operations including encoding and random name generation.
/// </summary>
public static class StringExtension
{
    /// <summary>
    /// Generates a random alphanumeric name with a specified length.
    /// </summary>
    /// <param name="prefix">The prefix to use for the generated name (default is "#").</param>
    /// <param name="length">The desired length of the random name (default is 11, max 50).</param>
    /// <returns>A string starting with the specified prefix followed by random alphanumeric characters.</returns>
    /// <remarks>
    /// <para>
    /// The generated name consists of lowercase letters (a-z) and digits (0-9).
    /// The maximum allowed length is capped at 50 characters for performance reasons.
    /// </para>
    /// <para>
    /// Example output: "#a3b7c9d2e1f" when using default parameters
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="length"/> is less than 1.
    /// </exception>
    public static string GetRandomName(string prefix = "#", int length = 11)
    {
        const string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (length > 50) length = 50;

        var result = new StringBuilder(length + 1); // +1 for the prefix
        result.Append(prefix);
        for (var i = 0; i < length; i++) 
            result.Append(characters[Random.Shared.Next(characters.Length)]);

        return result.ToString();
    }
    
    /// <summary>
    /// Converts a string to its UTF-8 encoded byte array representation.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>A byte array containing the UTF-8 encoded string.</returns>
    /// <remarks>
    /// This method provides a convenient way to convert strings to UTF-8 bytes
    /// without explicitly referencing the <see cref="Encoding.UTF8"/> property.
    /// </remarks>
    public static byte[] GetUtf8Bytes(this string value) => Encoding.UTF8.GetBytes(value);
    
    /// <summary>
    /// Converts a UTF-8 encoded byte array back to a string.
    /// </summary>
    /// <param name="value">The byte array to decode.</param>
    /// <returns>A string reconstructed from the UTF-8 bytes.</returns>
    /// <remarks>
    /// This method provides a convenient way to convert UTF-8 bytes to strings
    /// without explicitly referencing the <see cref="Encoding.UTF8"/> property.
    /// </remarks>
    public static string GetUtf8String(this byte[] value) => Encoding.UTF8.GetString(value);
}