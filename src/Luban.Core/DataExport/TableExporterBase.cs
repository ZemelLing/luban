using System.Reflection;
using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public abstract class TableExporterBase : ITableExporter
{
    public bool AllTablesInOneFile => GetType().GetCustomAttribute<TableExporterAttribute>().AllTablesInOneFile;

    public abstract OutputFile Export(DefTable table, List<Record> records);
    
    public virtual OutputFile ExportAllInOne(List<DefTable> tables)
    {
        throw new NotSupportedException();
    }
}