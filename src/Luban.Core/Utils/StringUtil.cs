using System.Collections;

namespace Luban.Core.Utils;

public static class StringUtil
{
    public static string CollectionToString(IEnumerable collection)
    {
        return string.Join(",", collection);
    }
}