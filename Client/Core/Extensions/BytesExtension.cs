using System.Buffers;
using System.Reactive.Linq;

namespace Client.Core.Extensions;

/// <summary>
/// Provides extension methods for byte array operations, including reactive streaming capabilities.
/// </summary>
public static class BytesExtension 
{
    /// <summary>
    /// Creates an observable sequence from a byte array with configurable chunking and progress reporting.
    /// </summary>
    /// <param name="sourceData">The source byte array to stream.</param>
    /// <param name="progressCallback">
    /// Optional callback that receives progress updates (0-100). 
    /// -1 indicates error, 100 indicates completion.
    /// </param>
    /// <param name="chunkSize">Size of chunks to emit (default: 8192 bytes).</param>
    /// <param name="progressBatchPercent">
    /// Progress update frequency in percent (default: 5% increments).
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="useBackpressure">
    /// Whether to introduce small delays between chunks (default: false).
    /// </param>
    /// <returns>
    /// An <see cref="IObservable{T}"/> that emits byte array chunks.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Key Features:
    /// - Memory-efficient chunking using <see cref="ArrayPool{T}"/>
    /// - Batched progress reporting to minimize callback overhead
    /// - Guaranteed completion (100%) and error (-1) progress signals
    /// - Cancellation support
    /// - Optional backpressure
    /// </para>
    /// <para>
    /// Behavior:
    /// 1. Splits source data into chunks of specified size
    /// 2. Uses array pooling to minimize allocations
    /// 3. Reports progress at specified percentage intervals
    /// 4. Automatically cleans up resources
    /// 5. Handles errors gracefully
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var observable = largeByteArray.Create(
    ///     progress => {
    ///         Console.WriteLine($"Progress: {progress}%");
    ///         return Task.CompletedTask;
    ///     },
    ///     chunkSize: 4096,
    ///     progressBatchPercent: 10);
    /// </code>
    /// </example>
    public static IObservable<byte[]> Create(
        this byte[] sourceData,
        Func<int, Task>? progressCallback = null,
        int chunkSize = 8192,
        int progressBatchPercent = 5,
        CancellationToken cancellationToken = default,
        bool useBackpressure = false)
    {
        return Observable.Create<byte[]>(async observer =>
        {
            try
            {
                var lastReportedProgress = -1;

                for (var offset = 0; offset < sourceData.Length; offset += chunkSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentChunkSize = Math.Min(chunkSize, sourceData.Length - offset);
                    var chunk = ArrayPool<byte>.Shared.Rent(currentChunkSize);
                    try
                    {
                        Array.Copy(sourceData, offset, chunk, 0, currentChunkSize);
                        observer.OnNext(chunk.AsMemory(0, currentChunkSize).ToArray());

                        if (progressCallback != null)
                        {
                            var currentProgress = (int)((long)(offset + currentChunkSize) * 100 / sourceData.Length);
                        
                            if (currentProgress >= 100 || 
                                currentProgress / progressBatchPercent > lastReportedProgress / progressBatchPercent)
                            {
                                lastReportedProgress = currentProgress;
                                _ = progressCallback(currentProgress).ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(chunk);
                    }

                    if (useBackpressure)
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
                
                if (progressCallback != null && lastReportedProgress <= 100)
                    await progressCallback(0).ConfigureAwait(false);
                
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                if (progressCallback != null)
                    await progressCallback(-1).ConfigureAwait(false);
                observer.OnError(ex);
            }
        });
    }
}