using Luban.Job.Common.Types;
using Luban.Job.Common.TypeVisitors;

namespace Luban.TypeVisitors;

public class CsEditorDefineTypeName : CsDefineTypeName
{
    public static new CsEditorDefineTypeName Ins { get; } = new CsEditorDefineTypeName();

    protected override ITypeFuncVisitor<string> UnderlyingVisitor => CsEditorUnderlyingDefineTypeName.Ins;

    public override string Accept(TDateTime type)
    {
        return "string";
    }

    public override string Accept(TText type)
    {
        return CfgConstStrings.EditorTextTypeName;
    }
}