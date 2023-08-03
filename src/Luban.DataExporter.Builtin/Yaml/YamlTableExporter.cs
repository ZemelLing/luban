using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;
using Luban.Core.Utils;
using YamlDotNet.RepresentationModel;

namespace Luban.DataExporter.Builtin.Yaml;

[TableExporter("yaml")]
public class YamlTableExporter : TableExporterBase
{
    protected override string OutputFileExt => "yml";
    
    public YamlNode WriteAsArray(List<Record> datas)
    {

        var seqNode = new YamlSequenceNode();
        foreach (var d in datas)
        {
            seqNode.Add(d.Data.Apply(YamlDataVisitor.Ins));
        }
        return seqNode;
    }

    public override OutputFile Export(DefTable table, List<Record> records)
    {
        var node = WriteAsArray(records);
        var ys = new YamlStream(new YamlDocument(node));
        var ms = new MemoryStream();
        var tw = new StreamWriter(ms);
        ys.Save(tw, false);
        tw.Flush();
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = DataUtil.StreamToBytes(ms),
        };
    }
}