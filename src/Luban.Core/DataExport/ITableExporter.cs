using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public interface ITableDataExporter
{
    OutputFile Export(DefTable table, List<Record> records);
}