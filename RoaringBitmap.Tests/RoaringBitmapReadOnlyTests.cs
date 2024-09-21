using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Gabrielmi.RoaringBitmap.Tests;

[TestFixture]
public class RoaringBitmapReadOnlyTests
{
    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenValuesIsNull()
    {
        // Arrange
        IEnumerable<uint> values = null;

        // Act
        Action act = () => new RoaringBitmapReadOnly(values);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("values");
    }

    [Test]
    public void Contains_ShouldReturnTrue_ForValuesInBitmap()
    {
        // Arrange
        var values = new uint[] { 1, 2, 3, 100_000, 200_000 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(1).Should().BeTrue();
        bitmap.Contains(2).Should().BeTrue();
        bitmap.Contains(3).Should().BeTrue();
        bitmap.Contains(100_000).Should().BeTrue();
        bitmap.Contains(200_000).Should().BeTrue();
    }

    [Test]
    public void Contains_ShouldReturnFalse_ForValuesNotInBitmap()
    {
        // Arrange
        var values = new uint[] { 1, 2, 3, 100_000, 200_000 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeFalse();
        bitmap.Contains(4).Should().BeFalse();
        bitmap.Contains(99_999).Should().BeFalse();
        bitmap.Contains(300_000).Should().BeFalse();
    }

    [Test]
    public void Constructor_ShouldHandleEmptyInput()
    {
        // Arrange
        var values = Array.Empty<uint>();
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeFalse();
        bitmap.Contains(100).Should().BeFalse();
        bitmap.MaxValue.Should().Be(0);
    }

    [Test]
    public void Contains_ShouldReturnTrue_ForLargeValuesInBitmap()
    {
        // Arrange
        var values = new uint[] { uint.MaxValue, uint.MaxValue - 1 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(uint.MaxValue).Should().BeTrue();
        bitmap.Contains(uint.MaxValue - 1).Should().BeTrue();
    }

    [Test]
    public void MaxValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var values = new uint[] { 1, 2, 3, 100_000, 200_000 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.MaxValue.Should().Be(200_000);
    }

    [Test]
    public void Constructor_ShouldIgnoreDuplicateValues()
    {
        // Arrange
        var values = new uint[] { 1, 1, 1, 2, 2, 3 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(1).Should().BeTrue();
        bitmap.Contains(2).Should().BeTrue();
        bitmap.Contains(3).Should().BeTrue();
        bitmap.Contains(4).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleValuesInSameContainer()
    {
        // Arrange
        var values = new uint[] { 65_536, 65_537, 65_538 }; // HighBits is 1
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(65_536).Should().BeTrue();
        bitmap.Contains(65_537).Should().BeTrue();
        bitmap.Contains(65_538).Should().BeTrue();
        bitmap.Contains(65_539).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleValuesInDifferentContainers()
    {
        // Arrange
        var values = new uint[] { 0, 65_536, 131_072 }; // HighBits 0,1,2
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeTrue();
        bitmap.Contains(65_536).Should().BeTrue();
        bitmap.Contains(131_072).Should().BeTrue();
        bitmap.Contains(65_537).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleBoundaryValues()
    {
        // Arrange
        var values = new uint[] { 0, uint.MaxValue };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeTrue();
        bitmap.Contains(uint.MaxValue).Should().BeTrue();
        bitmap.Contains(1).Should().BeFalse();
        bitmap.Contains(uint.MaxValue - 1).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleBitmapContainers()
    {
        // Arrange
        var values = Enumerable.Range(0, RoaringBitmapReadOnly.MaxContainerSize).Select(i => (uint)i);
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeTrue();
        bitmap.Contains((uint)(RoaringBitmapReadOnly.MaxContainerSize - 1)).Should().BeTrue();
        bitmap.Contains((uint)RoaringBitmapReadOnly.MaxContainerSize).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleArrayContainers()
    {
        // Arrange
        var values = Enumerable.Range(0, RoaringBitmapReadOnly.MaxContainerSize - 1).Select(i => (uint)i);
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(0).Should().BeTrue();
        bitmap.Contains((uint)(RoaringBitmapReadOnly.MaxContainerSize - 2)).Should().BeTrue();
        bitmap.Contains((uint)(RoaringBitmapReadOnly.MaxContainerSize - 1)).Should().BeFalse();
    }

    [Test]
    public void Contains_ShouldHandleNonConsecutiveValues()
    {
        // Arrange
        var values = new uint[] { 10, 20, 30, 100_000, 200_000 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(10).Should().BeTrue();
        bitmap.Contains(20).Should().BeTrue();
        bitmap.Contains(30).Should().BeTrue();
        bitmap.Contains(100_000).Should().BeTrue();
        bitmap.Contains(200_000).Should().BeTrue();
        bitmap.Contains(15).Should().BeFalse();
        bitmap.Contains(100_001).Should().BeFalse();
    }

    [Test]
    public void MaxValue_ShouldBeZero_WhenNoValues()
    {
        // Arrange
        var values = Array.Empty<uint>();
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.MaxValue.Should().Be(0);
    }

    [Test]
    public void MaxValue_ShouldBeSameValue_WhenAllValuesAreSame()
    {
        // Arrange
        var values = new uint[] { 42, 42, 42 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.MaxValue.Should().Be(42);
    }

    [Test]
    public void Cardinality_ShouldReturnCorrectCount()
    {
        // Arrange
        var values = new uint[] { 1, 2, 3, 4, 5 };
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Cardinality.Should().Be(5);
    }

    [Test]
    public void Contains_ShouldHandleEmptyContainersWithinBitmap()
    {
        // Arrange
        var values = new uint[] { 1, 65_536 * 2 }; // Containers at index 0 and 2
        var bitmap = new RoaringBitmapReadOnly(values);

        // Act & Assert
        bitmap.Contains(1).Should().BeTrue();
        bitmap.Contains(65_536).Should().BeFalse(); // Container index 1 is empty
        bitmap.Contains(65_536 * 2).Should().BeTrue();
    }
}
