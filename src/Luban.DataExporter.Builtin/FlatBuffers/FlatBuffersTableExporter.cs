using System.Text.Json;
using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;
using Luban.Core.Serialization;
using Luban.Core.Utils;
using Luban.DataExporter.Builtin.Binary;

namespace Luban.DataExporter.Builtin.FlatBuffers;

[TableExporter("flatbuffers")]
public class FlatBuffersTableExporter : TableExporterBase
{
    protected override string OutputFileExt => "json";
    
    private void WriteAsTable(List<Record> datas, Utf8JsonWriter x)
    {
        x.WriteStartObject();
        // 如果修改了这个名字，请同时修改table.tpl
        x.WritePropertyName("data_list");
        x.WriteStartArray();
        foreach (var d in datas)
        {
            d.Data.Apply(FlatBuffersJsonDataVisitor.Ins, x);
        }
        x.WriteEndArray();
        x.WriteEndObject();
    }

    public override OutputFile Export(DefTable table, List<Record> records)
    {                  
        var ss = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
        {
            Indented = !JsonTableExporter.UseCompactJson,
            SkipValidation = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        WriteAsTable(records, jsonWriter);
        jsonWriter.Flush();
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = DataUtil.StreamToBytes(ss),
        };
    }
}