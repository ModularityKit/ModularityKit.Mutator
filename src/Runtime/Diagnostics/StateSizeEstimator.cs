using System.Collections;
using System.Text;

namespace ModularityKit.Mutator.Runtime.Diagnostics;

/// <summary>
/// Provides a best-effort estimate of the in-memory size of a state object in bytes.
/// </summary>
internal static class StateSizeEstimator
{
    private static readonly IReadOnlyDictionary<Type, int> PrimitiveTypeSizes = new Dictionary<Type, int>
    {
        [typeof(bool)] = sizeof(bool),
        [typeof(byte)] = sizeof(byte),
        [typeof(sbyte)] = sizeof(sbyte),
        [typeof(char)] = sizeof(char),
        [typeof(short)] = sizeof(short),
        [typeof(ushort)] = sizeof(ushort),
        [typeof(int)] = sizeof(int),
        [typeof(uint)] = sizeof(uint),
        [typeof(long)] = sizeof(long),
        [typeof(ulong)] = sizeof(ulong),
        [typeof(float)] = sizeof(float),
        [typeof(double)] = sizeof(double),
        [typeof(decimal)] = sizeof(decimal),
        [typeof(Guid)] = 16
    };

    /// <summary>
    /// Estimates the size of the given state in bytes.
    /// </summary>
    /// <param name="state">The state object to estimate. Can be <see langword="null" />.</param>
    /// <returns>
    /// The estimated byte size: UTF-8 byte count for strings, byte length for primitive arrays,
    /// element count for collections, or <c>0</c> for unrecognized or null values.
    /// </returns>
    public static long Estimate(object? state)
    {
        if (state is null)
            return 0;

        if (state is string text)
            return Encoding.UTF8.GetByteCount(text);

        if (TryEstimateArraySize(state, out var arraySize))
            return arraySize;

        return state is ICollection collection ? collection.Count : 0;
    }

    /// <summary>
    /// Attempts to estimate the byte size of a primitive array.
    /// </summary>
    /// <param name="state">The object to inspect.</param>
    /// <param name="sizeInBytes">When successful, contains the estimated byte size of the array.</param>
    /// <returns><see langword="true" /> if <paramref name="state" /> is a primitive array with a known element size; otherwise <see langword="false" />.</returns>
    private static bool TryEstimateArraySize(object state, out long sizeInBytes)
    {
        sizeInBytes = 0;
        if (state is not Array array)
            return false;

        var elementType = state.GetType().GetElementType();
        if (elementType is null)
            return false;

        if (!PrimitiveTypeSizes.TryGetValue(elementType, out var elementSize))
            return false;

        sizeInBytes = array.LongLength * elementSize;
        return true;
    }
}
