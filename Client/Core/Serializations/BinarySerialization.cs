using Toolkit.Blazor.Extensions.File.Entities;

namespace Client.Core.Serializations;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="Binary"/> objects to/from byte arrays.
/// </summary>
/// <remarks>
/// The serialization format is:
/// 1. MIME type (length-prefixed string)
/// 2. Binary data length (4-byte integer)
/// 3. Binary data (variable length)
/// 4. Metadata count (4-byte integer)
/// 5. Key-value pairs (length-prefixed strings)
/// </remarks>
public static class BinarySerialization
{
    /// <summary>
    /// Serializes a <see cref="Binary"/> object to a byte array.
    /// </summary>
    /// <param name="binary">The binary object to serialize.</param>
    /// <returns>A byte array containing the serialized data.</returns>
    /// <remarks>
    /// The serialization process:
    /// 1. Writes the MIME type as a length-prefixed string
    /// 2. Writes the binary data length followed by the data itself
    /// 3. Writes the metadata count (0 if null)
    /// 4. Writes each metadata key-value pair as length-prefixed strings
    /// Uses memory-efficient streaming approach with <see cref="BinaryWriter"/>.
    /// </remarks>
    public static byte[] SerializeToBytes(this Binary binary)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        // Write MimeType (length-prefixed)
        writer.Write(binary.MimeType);
        
        // Write Bytes array (length-prefixed)
        writer.Write(binary.Bytes.Length);
        if (binary.Bytes.Length > 0)
            writer.Write(binary.Bytes);
        
        // Write Metadata count
        writer.Write(binary.Metadata?.Count ?? 0);
        
        // Write Metadata entries
        if (binary.Metadata == null) return ms.ToArray();
        
        foreach (var kvp in binary.Metadata)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a byte array back to a <see cref="Binary"/> object.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <returns>A reconstructed <see cref="Binary"/> object.</returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the input data is truncated or malformed.
    /// </exception>
    /// <remarks>
    /// The deserialization process:
    /// 1. Reads the MIME type (length-prefixed string)
    /// 2. Reads the binary data length and then the data itself
    /// 3. Reads the metadata count
    /// 4. Reconstructs the metadata dictionary if count > 0
    /// Maintains exact fidelity with the original serialization format.
    /// </remarks>
    public static Binary DeserializeToBinary(this byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var mimeType = reader.ReadString();
        var bytesLength = reader.ReadInt32();
        var bytes = bytesLength > 0 ? reader.ReadBytes(bytesLength) : [];
        
        var metadataCount = reader.ReadInt32();
        var metadata = metadataCount > 0 ? new Dictionary<string, string>() : null;
        
        for (var i = 0; i < metadataCount; i++)
        {
            metadata!.Add(reader.ReadString(), reader.ReadString());
        }
        
        return new Binary(bytes, mimeType) { Metadata = metadata };
    }
}