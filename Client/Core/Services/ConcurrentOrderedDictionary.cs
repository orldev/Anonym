using System.Collections.Concurrent;

namespace Client.Core.Services;

/// <summary>
/// A thread-safe dictionary that maintains insertion order while supporting efficient key-based access,
/// with optional automatic promotion of accessed items.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
/// <remarks>
/// This collection combines the fast lookups of a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// with the ordered characteristics of a linked list, using a <see cref="ReaderWriterLockSlim"/>
/// to synchronize access to the ordering structure.
/// </remarks>
public sealed class ConcurrentOrderedDictionary<TKey, TValue> : IDisposable 
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _dictionary = new();
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _orderedList = [];
    private readonly ReaderWriterLockSlim _listLock = new(LockRecursionPolicy.NoRecursion);
    private readonly ConcurrentQueue<TKey> _promotionQueue = new();
    private readonly Timer? _promotionTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the dictionary with automatic promotion enabled.
    /// </summary>
    /// <remarks>
    /// The promotion timer runs every 100ms to move recently accessed items to the front.
    /// </remarks>
    public ConcurrentOrderedDictionary()
    {
        _promotionTimer = new Timer(_ => ProcessPromotions(), null, 100, 100);
    }

    /// <summary>
    /// Gets an ordered sequence of all keys in the dictionary.
    /// </summary>
    /// <remarks>
    /// The enumeration is thread-safe and returns keys in their current order.
    /// </remarks>
    public IEnumerable<TKey> Keys
    {
        get
        {
            _listLock.EnterReadLock();
            try
            {
                foreach (var item in _orderedList)
                {
                    yield return item.Key;
                }
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets an ordered sequence of all values in the dictionary.
    /// </summary>
    /// <remarks>
    /// The enumeration is thread-safe and returns values in their current order.
    /// </remarks>
    public IEnumerable<TValue> Values
    {
        get
        {
            _listLock.EnterReadLock();
            try
            {
                foreach (var item in _orderedList)
                {
                    yield return item.Value;
                }
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }
    }
    
    /// <summary>
    /// Attempts to add a key/value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add</param>
    /// <param name="value">The value of the element to add</param>
    /// <returns>true if the element was added; false if the key already exists</returns>
    /// <remarks>
    /// New items are added to the front of the ordered list.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value)
    {
        var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(
            new KeyValuePair<TKey, TValue>(key, value));

        if (!_dictionary.TryAdd(key, newNode))
            return false;

        _listLock.EnterWriteLock();
        try { _orderedList.AddFirst(newNode); }
        finally { _listLock.ExitWriteLock(); }
        return true;
    }

    /// <summary>
    /// Inserts a key/value pair at the specified position in the ordered sequence.
    /// </summary>
    /// <param name="index">The zero-based insertion index</param>
    /// <param name="key">The key of the element to add</param>
    /// <param name="value">The value of the element to add</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists</exception>
    /// <remarks>
    /// If index is 0, adds to the front; if index >= Count, adds to the end.
    /// </remarks>
    public void Insert(int index, TKey key, TValue value)
    {
        var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(
            new KeyValuePair<TKey, TValue>(key, value));

        if (!_dictionary.TryAdd(key, newNode))
            throw new ArgumentException("Key already exists");

        _listLock.EnterWriteLock();
        try
        {
            if (index <= 0) _orderedList.AddFirst(newNode);
            else if (index >= _orderedList.Count) _orderedList.AddLast(newNode);
            else
            {
                var current = _orderedList.First;
                for (var i = 0; i < index - 1 && current != null; i++)
                    current = current.Next;
                if (current != null) _orderedList.AddAfter(current, newNode);
            }
        }
        catch
        {
            _dictionary.TryRemove(key, out _);
            throw;
        }
        finally
        {
            _listLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Attempts to remove and return the value with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove</param>
    /// <param name="value">When successful, contains the removed value</param>
    /// <returns>true if the element was found and removed; otherwise false</returns>
    public bool TryRemove(TKey key, out TValue? value)
    {
        if (!_dictionary.TryRemove(key, out var node))
        {
            value = default;
            return false;
        }

        _listLock.EnterWriteLock();
        try { _orderedList.Remove(node); }
        finally { _listLock.ExitWriteLock(); }

        value = node.Value.Value;
        return true;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get</param>
    /// <param name="value">When successful, contains the retrieved value</param>
    /// <returns>true if the key was found; otherwise false</returns>
    /// <remarks>
    /// Successful lookups automatically enqueue the item for promotion.
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (_dictionary.TryGetValue(key, out var node))
        {
            value = node.Value.Value;
            _promotionQueue.Enqueue(key); // Schedule promotion
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Adds a key/value pair if the key doesn't exist, or updates the existing value.
    /// </summary>
    /// <param name="key">The key to add or update</param>
    /// <param name="updateAction">Action that modifies the value</param>
    /// <param name="value">The resulting value after the operation</param>
    /// <exception cref="ArgumentNullException">Thrown if key or updateAction is null</exception>
    /// <remarks>
    /// New items are added to the front. Updated items are scheduled for promotion.
    /// </remarks>
    public void AddOrUpdate(TKey key, Action<TValue> updateAction, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(updateAction);

        LinkedListNode<KeyValuePair<TKey, TValue>>? resultNode = null;

        _dictionary.AddOrUpdate(key,
            // Add case
            k => {
                var newValue = Activator.CreateInstance<TValue>();
                updateAction(newValue);
                resultNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(
                    new KeyValuePair<TKey, TValue>(k, newValue));
            
                _listLock.EnterWriteLock();
                try { _orderedList.AddFirst(resultNode); }
                finally { _listLock.ExitWriteLock(); }
            
                return resultNode;
            },
            // Update case
            (k, existingNode) => {
                updateAction(existingNode.Value.Value);
                resultNode = existingNode;
            
                _promotionQueue.Enqueue(k); // Schedule promotion
                return existingNode;
            });
        
        value = resultNode!.Value.Value;
    }
    
    /// <summary>
    /// Gets an ordered sequence of all key/value pairs in the dictionary.
    /// </summary>
    /// <returns>An ordered enumeration of key/value pairs</returns>
    /// <remarks>
    /// The enumeration is thread-safe and reflects the current order of items.
    /// </remarks>
    public IEnumerable<KeyValuePair<TKey, TValue>> GetOrderedItems()
    {
        _listLock.EnterReadLock();
        try
        {
            foreach (var item in _orderedList)
                yield return item;
        }
        finally
        {
            _listLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _listLock.EnterWriteLock();
        try
        {
            _dictionary.Clear();
            _orderedList.Clear();
            while (_promotionQueue.TryDequeue(out _)) {}
        }
        finally
        {
            _listLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the dictionary.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate</param>
    /// <returns>true if the dictionary contains the key; otherwise false</returns>
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <summary>
    /// Releases all resources used by the dictionary.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _promotionTimer?.Dispose();
        _listLock.Dispose();
    }

    // Internal promotion methods
    private void ProcessPromotions()
    {
        if (_listLock.TryEnterWriteLock(0)) // Non-blocking attempt
        {
            try
            {
                while (_promotionQueue.TryDequeue(out var key) && 
                       _dictionary.TryGetValue(key, out var node))
                {
                    _orderedList.Remove(node);
                    _orderedList.AddFirst(node);
                }
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }
    }
}