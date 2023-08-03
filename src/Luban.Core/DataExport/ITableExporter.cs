using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public interface ITableExporter
{
    bool AllTablesInOneFile { get; }
    
    OutputFile Export(DefTable table, List<Record> records);
    
    OutputFile ExportAllInOne(List<DefTable> tables);
}