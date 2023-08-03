using Luban.Core.Datas;
using Luban.Core.Utils;

namespace Luban.Core.DataVisitors;

public class ToStringVisitor2 : ToStringVisitor
{
    public new static ToStringVisitor2 Ins { get; } = new();

    public override string Accept(DString type)
    {
        return DataUtil.EscapeString(type.Value);
    }
}