using Luban.Core.Types;
using Scriban.Runtime;

namespace Luban.Core.Tmpl;

public class TypeTemplateExtends : ScriptObject
{
    public static bool NeedMarshalBoolPrefix(TType type)
    {
        return type.IsNullable;
    }
}