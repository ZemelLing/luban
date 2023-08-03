using System.Reflection;
using Luban.Core.Defs;

namespace Luban.Core.DataTarget;

public abstract class DataTargetBase : IDataTarget
{
    public const string FamilyPrefix = "tableExporter";
    
    public bool AllTablesInOneFile => GetType().GetCustomAttribute<DataTargetAttribute>().AllTablesInOneFile;
    
    protected abstract string OutputFileExt { get; }

    public abstract OutputFile Export(DefTable table, List<Record> records);
    
    public virtual OutputFile ExportAllInOne(List<DefTable> tables)
    {
        throw new NotSupportedException();
    }
}