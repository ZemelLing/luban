using System.Collections;

namespace Luban.Utils;

public static class StringUtil
{
    public static string CollectionToString(IEnumerable collection)
    {
        return string.Join(",", collection);
    }
}