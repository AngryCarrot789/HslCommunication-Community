namespace HslCommunication;

public static class ArrayUtils {
    /// <summary>
    /// Copies count number of bytes from source starting at the source index to the destination starting at the destination index
    /// </summary>
    /// <typeparam name="T">The type of array elements</typeparam>
    public static void CopyRange<T>(this T[] source, int srcIndex, T[] destination, int dstIndex, int count) {
        for (int i = 0; i < count; i++) {
            destination[dstIndex + i] = source[srcIndex + i];
        }
    }
    
    /// <summary>
    /// Copies count number of bytes from source the destination starting at the destination index
    /// </summary>
    /// <typeparam name="T">The type of array elements</typeparam>
    public static void CopyRange<T>(this T[] source, T[] destination, int dstIndex, int count) {
        for (int i = 0; i < count; i++) {
            destination[dstIndex + i] = source[i];
        }
    }
}