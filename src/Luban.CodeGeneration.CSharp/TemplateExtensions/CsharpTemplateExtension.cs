using Luban.CodeGeneration.CSharp.TypeVisitors;
using Luban.Core.CodeFormat;
using Luban.Core.Defs;
using Luban.Core.Types;
using Luban.Core.Utils;
using Scriban.Runtime;

namespace Luban.CodeGeneration.CSharp.TemplateExtensions;

public class CsharpTemplateExtension : ScriptObject
{
    public static string ImplDataType(DefBean type, DefBean parent)
    {
        return DataUtil.GetImplTypeName(type, parent);
    }

    public static string CsRecursiveResolve(DefField field, string tables, ICodeStyle codeStyle)
    {
        return field.CType.Apply(RecursiveResolveVisitor.Ins,  codeStyle.FormatField(field.Name), tables);
    }
}