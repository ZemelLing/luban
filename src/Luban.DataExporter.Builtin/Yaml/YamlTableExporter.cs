using System.Text.Json;
using System.Xml;
using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;
using Luban.Core.Utils;
using Luban.DataExporter.Builtin.Json;
using Luban.DataExporter.Builtin.Yaml;
using YamlDotNet.RepresentationModel;

namespace Luban.DataExporter.Builtin.Xml;

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