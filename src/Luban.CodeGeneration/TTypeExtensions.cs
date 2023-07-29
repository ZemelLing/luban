using Luban.Job.Common.Types;
using Luban.Job.Common.TypeVisitors;

namespace Luban;

static class TTypeExtensions
{
    public static string CsUnderingDefineType(this TType type)
    {
        return type.Apply(CsUnderingDefineTypeName.Ins);
    }
}