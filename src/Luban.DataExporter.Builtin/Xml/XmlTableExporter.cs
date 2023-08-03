using System.Text.Json;
using System.Xml;
using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;
using Luban.Core.Utils;
using Luban.DataExporter.Builtin.Json;

namespace Luban.DataExporter.Builtin.Xml;

[TableExporter("xml")]
public class XmlTableExporter : TableExporterBase
{
    protected override string OutputFileExt => "xml";
    
    public void WriteAsArray(List<Record> datas, XmlWriter w)
    {
        w.WriteStartDocument();
        w.WriteStartElement("table");
        foreach (var d in datas)
        {
            w.WriteStartElement("record");
            d.Data.Apply(XmlDataVisitor.Ins, w);
            w.WriteEndElement();
        }
        w.WriteEndElement();
        w.WriteEndDocument();
    }

    public override OutputFile Export(DefTable table, List<Record> records)
    {
        var xwSetting = new XmlWriterSettings() { Indent = true };
        var ms = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(ms, xwSetting);
        WriteAsArray(records, xmlWriter);
        xmlWriter.Flush();
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = DataUtil.StreamToBytes(ms),
        };
    }
}