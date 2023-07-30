using Luban.Core.Types;
using Luban.Core.TypeVisitors;

namespace Luban;

static class TTypeExtensions
{
    public static string CsUnderingDefineType(this TType type)
    {
        return type.Apply(CsUnderingDefineTypeName.Ins);
    }
}