using Luban.CodeGeneration.CSharp.TypeVisitors;
using Luban.Core.CodeFormat;
using Luban.Core.Defs;
using Luban.Core.Types;
using Luban.Core.Utils;
using Scriban.Runtime;

namespace Luban.CodeGeneration.CSharp.TemplateExtensions;

public class CsharpDotNetTemplateExtension : ScriptObject
{
    public static string DotNetJsonDeserialize(string bufName, string fieldName, string jsonFieldName, TType type)
    {
        if (type.IsNullable)
        {
            return $"{{ if ({bufName}.TryGetProperty(\"{jsonFieldName}\", out var _j) && _j.ValueKind != JsonValueKind.Null) {{ {type.Apply(DotNetJsonDeserializeVisitor.Ins, "_j", fieldName, 0)} }} else {{ {fieldName} = null; }} }}";
        }
        else
        {
            return type.Apply(DotNetJsonDeserializeVisitor.Ins, $"{bufName}.GetProperty(\"{jsonFieldName}\")", fieldName, 0);
        }
    }
}