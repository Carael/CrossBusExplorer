using System;
using System.Collections.Generic;
using System.Linq;
namespace CrossBusExplorer.Website.Extensions;

public static class IListExtensions
{
    public static void AddOrReplace<T>(this IList<T> list, Func<T, bool> func, T item)
    {
        var listItem = list.FirstOrDefault(func);
        if (listItem != null)
        {
            list[list.IndexOf(listItem)] = item;
        }
        else
        {
            list.Add(item);
        }
    }

    public static void RemoveNonExisting<T>(this IList<T> list, Func<T, bool> func)
    {
        var nonExistingItems = list.Where(func).ToArray();

        for (var i = 0; i < nonExistingItems.Length; i++)
        {
            list.Remove(nonExistingItems[i]);
        }
    }
}