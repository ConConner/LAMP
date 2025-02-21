using System.Collections.Generic;

namespace LAMP.Utilities;

public static class ListOp
{
    public static void Switch<T>(this List<T> l, int firstIndex, int secondIndex)
    {
        T firstItem = l[firstIndex];
        l[firstIndex] = l[secondIndex];
        l[secondIndex] = firstItem;
    }
}
