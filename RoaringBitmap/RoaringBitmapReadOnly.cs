using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gabrielmi.RoaringBitmap;

/// <summary>
/// This Roaring Bitmap class is read-only and optimized for memory usage on low populated
/// but sparse bitmaps. It also increase the reading speed.
/// </summary>
public class RoaringBitmapReadOnly
{
    public const ushort MaxContainerSize = 4096;
    private const int EmptyContainer = -1;
    private const int BitsPerWord = 64;
    private const int BitsPerShort = 16;

    /// <summary>
    /// Maximum value stored in the bitmap.
    /// </summary>
    public uint MaxValue { get; private set; }

    /// <summary>
    /// Number of distinct values stored in the bitmap.
    /// </summary>
    public uint Cardinality { get; private set; }

    // Indicates whether each container is in bitmap mode.
    private readonly ulong[] _isBitmap;

    // Stores all container data (both arrays and bitmaps).
    private readonly ushort[] _values;

    // Offsets into the _values array for each container.
    private readonly int[] _containersOffsets;

    // Lengths of each container (number of elements or fixed size for bitmaps).
    private readonly ushort[] _containersLengths;

    public RoaringBitmapReadOnly(IEnumerable<uint> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var distinctValues = values.Distinct().OrderBy(x => x).ToArray();

        if (distinctValues.Length == 0)
        {
            // Initialize empty bitmap.
            MaxValue = 0;
            Cardinality = 0;
            _isBitmap = Array.Empty<ulong>();
            _containersOffsets = Array.Empty<int>();
            _containersLengths = Array.Empty<ushort>();
            _values = Array.Empty<ushort>();
            return;
        }

        // Initialize MaxValue and Cardinality.
        MaxValue = distinctValues[^1];
        Cardinality = (uint)distinctValues.Length;

        // Determine number of containers needed.
        ushort mostSignificantBitsMax = Util.HighBits(MaxValue);
        int containersCount = mostSignificantBitsMax + 1;

        // Initialize container metadata arrays.
        _isBitmap = new ulong[(containersCount + BitsPerWord - 1) / BitsPerWord];
        _containersOffsets = new int[containersCount];
        Array.Fill(_containersOffsets, EmptyContainer);
        _containersLengths = new ushort[containersCount];

        // Group values by container (high bits).
        var containersValues = distinctValues
            .GroupBy(Util.HighBits)
            .Select(group => group.ToArray())
            .ToArray();

        // Calculate total required size for _values array.
        int totalValuesLength = CountTotalValuesLength(containersValues);
        _values = new ushort[totalValuesLength];

        int currentOffset = 0;

        // Build containers.
        foreach (var containerValues in containersValues)
        {
            ushort containerIndex = Util.HighBits(containerValues[0]);
            _containersOffsets[containerIndex] = currentOffset;

            if (containerValues.Length >= MaxContainerSize)
            {
                // Switch to bitmap mode.
                SetContainerBitmap(containerIndex);
                _containersLengths[containerIndex] = (ushort)MaxContainerSize;
                ushort[] containerBitmap = CreateContainerBitmap(containerValues);

                // Copy the entire bitmap into _values.
                Array.Copy(containerBitmap, 0, _values, currentOffset, MaxContainerSize);
                currentOffset += MaxContainerSize;
            }
            else
            {
                // Store values in array mode.
                _containersLengths[containerIndex] = (ushort)containerValues.Length;
                foreach (var value in containerValues)
                {
                    _values[currentOffset++] = Util.LowBits(value);
                }
            }
        }
    }

    /// <summary>
    /// Checks whether the specified value exists in the bitmap.
    /// </summary>
    public bool Contains(uint value)
    {
        // Extract container index and value within the container.
        ushort containerIndex = Util.HighBits(value);
        ushort containerValue = Util.LowBits(value);

        // Check if container exists.
        if (!ContainerExists(containerIndex))
        {
            return false;
        }

        // Check if container is in bitmap mode or array mode.
        return IsBitmap(containerIndex)
            ? BitmapContains(containerIndex, containerValue)
            : ArrayContains(containerIndex, containerValue);
    }

    #region Private Helper Methods

    /// <summary>
    /// Determines if a container exists for the given index.
    /// </summary>
    private bool ContainerExists(ushort containerIndex)
    {
        return containerIndex < _containersOffsets.Length &&
                _containersOffsets[containerIndex] != EmptyContainer;
    }

    /// <summary>
    /// Determines if the specified container is in bitmap mode.
    /// </summary>
    private bool IsBitmap(ushort containerIndex)
    {
        return (_isBitmap[containerIndex / BitsPerWord] & (1UL << (containerIndex % BitsPerWord))) != 0;
    }

    /// <summary>
    /// Checks if a value exists within a bitmap-mode container.
    /// </summary>
    private bool BitmapContains(ushort containerIndex, ushort value)
    {
        var containerOffset = _containersOffsets[containerIndex];
        var chunk = _values[containerOffset + value / 16];
        return (_values[containerOffset + value / 16] & (1 << (value % 16))) != 0;
    }

    /// <summary>
    /// Checks if a value exists within an array-mode container.
    /// </summary>
    private bool ArrayContains(ushort containerIndex, ushort value)
    {
        int containerOffset = _containersOffsets[containerIndex];
        int containerSize = _containersLengths[containerIndex];
        return Array.BinarySearch(_values, containerOffset, containerSize, value) >= 0;
    }

    /// <summary>
    /// Marks a container as bitmap mode.
    /// </summary>
    private void SetContainerBitmap(ushort containerIndex)
    {
        _isBitmap[containerIndex / BitsPerWord] |= 1UL << (containerIndex % BitsPerWord);
    }

    /// <summary>
    /// Creates a bitmap for a container from its values.
    /// </summary>
    private ushort[] CreateContainerBitmap(uint[] containerValues)
    {
        // Allocate an array of MaxContainerSize ushorts.
        // Note: This is more memory than necessary but matches the original implementation.
        ushort[] containerBitmap = new ushort[MaxContainerSize];

        foreach (var value in containerValues)
        {
            ushort lowBits = Util.LowBits(value);

            containerBitmap[lowBits / 16] |= (ushort)(1 << (lowBits % 16));
        }

        return containerBitmap;
    }

    /// <summary>
    /// Calculates the total length required for the _values array.
    /// </summary>
    private int CountTotalValuesLength(uint[][] containersValues)
    {
        int total = 0;
        foreach (var containerValues in containersValues)
        {
            total += containerValues.Length >= MaxContainerSize
                ? MaxContainerSize // Bitmap mode containers use fixed size.
                : containerValues.Length; // Array mode containers use actual number of values.
        }
        return total;
    }

    #endregion
}
