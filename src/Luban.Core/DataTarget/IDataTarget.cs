using Luban.Core.Defs;

namespace Luban.Core.DataTarget;

public interface IDataTarget
{
    bool AllTablesInOneFile { get; }
    
    OutputFile Export(DefTable table, List<Record> records);
    
    OutputFile ExportAllInOne(List<DefTable> tables);
}