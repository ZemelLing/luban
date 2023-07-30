using Luban.Core.Defs;

namespace Luban.Core.Utils;

public static class DefExtensions
{
    public static bool NeedExport(this DefField field)
    {
        return GenerationContext.Ins.NeedExport(field.Groups);
    }
    
    public static bool NeedExport(this DefTable table)
    {
        return GenerationContext.Ins.NeedExport(table.Groups);
    }
}