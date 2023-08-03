using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;
using Luban.Core.Serialization;

namespace Luban.DataExporter.Builtin.Binary;

[TableExporter("bin")]
public class BinaryTableExporter : TableExporterBase
{
    protected override string OutputFileExt => "bytes";
    
    private void WriteList(DefTable table, List<Record> datas, ByteBuf x)
    {
        x.WriteSize(datas.Count);
        foreach (var d in datas)
        {
            d.Data.Apply(BinaryDataVisitor.Ins, x);
        }
    }

    public override OutputFile Export(DefTable table, List<Record> records)
    {
        var bytes = new ByteBuf();
        WriteList(table, records, bytes);
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = bytes.CopyData(),
        };
    }
}