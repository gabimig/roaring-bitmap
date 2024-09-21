namespace Gabrielmi.RoaringBitmap;

internal static class Util
{
    public static ushort HighBits(uint x)
    {
        return (ushort)(x >> 16);
    }

    public static ushort LowBits(uint x)
    {
        return (ushort)(x & 0xFFFF);
    }
}