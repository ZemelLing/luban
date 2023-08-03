using System.Reflection;
using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public abstract class TableExporterBase : ITableExporter
{
    public const string FamilyPrefix = "tableExporter";
    
    public bool AllTablesInOneFile => GetType().GetCustomAttribute<TableExporterAttribute>().AllTablesInOneFile;
    
    protected abstract string OutputFileExt { get; }

    public abstract OutputFile Export(DefTable table, List<Record> records);
    
    public virtual OutputFile ExportAllInOne(List<DefTable> tables)
    {
        throw new NotSupportedException();
    }
}