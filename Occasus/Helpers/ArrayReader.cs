namespace Occasus.Helpers;

internal class ArrayReader<T>(T[] array)
{
    int index = 0;

    public int Skip(int skip)
    {
        return index += skip;
    }

    public ReadOnlySpan<T> Read(int length)
    {

        var readLength = Math.Min(length, array.Length - index);
        try
        {
            return array.AsSpan(index, readLength);
        }
        finally { index += readLength; }
    }
}
