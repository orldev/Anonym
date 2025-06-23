using System.Text.Json;
using System.Text.Json.Serialization;
using Toolkit.Blazor.Extensions.File.Entities;

namespace Client.Core.Serializations;

/// <summary>
/// A custom JSON converter for <see cref="Binary"/> objects that enforces a size limit.
/// </summary>
/// <remarks>
/// This converter:
/// - Enforces a maximum size of 32KB for binary data
/// - Handles both object-form and raw base64 string representations
/// - Preserves MIME type and metadata when present
/// - Provides graceful error handling for oversized data
/// - Maintains consistent serialization/deserialization behavior
/// </remarks>
public class SizeLimitedJsonConverter : JsonConverter<Binary>
{
    /// <summary>
    /// The maximum allowed size for binary data (32KB).
    /// </summary>
    private const int MaxByteLength = 32 * 1024; // 32KB limit
    
    /// <summary>
    /// Reads and converts JSON to a <see cref="Binary"/> object.
    /// </summary>
    /// <param name="reader">The JSON reader providing the data.</param>
    /// <param name="typeToConvert">The type to convert to (always Binary).</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>A <see cref="Binary"/> object or null.</returns>
    /// <remarks>
    /// Supports two input formats:
    /// 1. Full object form with Bytes, MimeType, and optional Metadata
    /// 2. Simple base64 string (treated as application/octet-stream)
    /// </remarks>
    public override Binary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle both object form and raw base64 strings
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<Binary>(ref reader, options);
        }
        
        // Handle simple base64 string case
        var bytes = reader.GetBytesFromBase64();
        return new Binary(bytes, "application/octet-stream");
    }

    /// <summary>
    /// Writes a <see cref="Binary"/> object to JSON format.
    /// </summary>
    /// <param name="writer">The JSON writer to use.</param>
    /// <param name="value">The Binary value to convert.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <remarks>
    /// Behavior:
    /// - Null values are written as JSON null
    /// - Values exceeding <see cref="MaxByteLength"/> are written as error objects
    /// - Valid values are written with their MIME type and optional metadata
    /// - All exceptions are caught and logged before rethrowing
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, Binary? value, JsonSerializerOptions options)
    {
        try
        {
            // Fast path for null values
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Bytes.Length > MaxByteLength)
            {
                WriteErrorResponse(writer);
                return;
            }

            // Direct serialization without intermediate buffers
            writer.WriteStartObject();
            writer.WriteBase64String("Bytes", value.Bytes);
            writer.WriteString("MimeType", value.MimeType);

            // Handle metadata if present
            if (value.Metadata is {Count: > 0})
            {
                writer.WritePropertyName("Metadata");
                JsonSerializer.Serialize(writer, value.Metadata, options);
            }

            writer.WriteEndObject();
        }
        catch (Exception e)
        {
            writer.WriteNullValue();
            Console.WriteLine($"{e} {value?.MimeType}");
            throw;
        }
    }

    /// <summary>
    /// Writes an error response for oversized binary data.
    /// </summary>
    /// <param name="writer">The JSON writer to use.</param>
    /// <remarks>
    /// Produces a structured error object with:
    /// - Empty byte array
    /// - "error" MIME type
    /// </remarks>
    private static void WriteErrorResponse(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("MimeType", "error");
        writer.WriteString("Bytes", Array.Empty<byte>());
        writer.WriteEndObject();
    }
}