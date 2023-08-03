using System.Text;
using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Datas;
using Luban.Core.Defs;
using Luban.DataExporter.Builtin.Lua;
using MessagePack;

namespace Luban.DataExporter.Builtin.MsgPack;

[TableExporter("msgpack")]
public class MsgPackTableExporter : TableExporterBase
{
    protected override string OutputFileExt => "bytes";


    public void WriteList(DefTable table, List<Record> records, ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(records.Count);
        foreach (var record in records)
        {
            MsgPackDataVisitor.Ins.Accept(record.Data, ref writer);
        }
    }
    
    public override OutputFile Export(DefTable table, List<Record> records)
    {
        var ss = new StringBuilder();
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = ss.ToString(),
        };
    }
}