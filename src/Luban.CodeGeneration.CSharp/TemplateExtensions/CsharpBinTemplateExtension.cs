using Luban.CodeGeneration.CSharp.TypeVisitors;
using Luban.Core.CodeFormat;
using Luban.Core.Defs;
using Luban.Core.Types;
using Luban.Core.Utils;
using Scriban.Runtime;

namespace Luban.CodeGeneration.CSharp.TemplateExtensions;

public class CsharpBinTemplateExtension : ScriptObject
{
    public static string Deserialize(string bufName, string fieldName, TType type)
    {
        return type.Apply(BinaryDeserializeVisitor.Ins, bufName, fieldName);
    }
}