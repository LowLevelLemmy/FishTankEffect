using System.Collections;
using System.Collections.Generic;

public static class ExtensionsMethods
{
    public static bool IsNullOrEmpty<T>(this T[] array)
    {
        return array == null || array.Length == 0;
    }
}
