using System.Text.Json;
using Google.Protobuf;
using Luban.Core;
using Luban.Core.DataTarget;
using Luban.Core.Defs;
using Luban.Core.Serialization;
using Luban.Core.Utils;
using Luban.DataExporter.Builtin.Binary;
using Luban.DataExporter.Builtin.Json;

namespace Luban.DataExporter.Builtin.Protobuf;

[DataTarget("protobuf-json")]
public class ProtobufJsonDataTarget : JsonDataTarget
{
    protected override string OutputFileExt => "json";
    
    protected override JsonDataVisitor ImplJsonDataVisitor => ProtobufJsonDataVisitor.Ins;
    
    public void WriteAsTable(List<Record> datas, Utf8JsonWriter x)
    {
        x.WriteStartObject();
        // 如果修改了这个名字，请同时修改table.tpl
        x.WritePropertyName("data_list");
        x.WriteStartArray();
        foreach (var d in datas)
        {
            d.Data.Apply(ProtobufJsonDataVisitor.Ins, x);
        }
        x.WriteEndArray();
        x.WriteEndObject();
    }

    public override OutputFile Export(DefTable table, List<Record> records)
    {                  
        var ss = new MemoryStream();
        var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
        {
            Indented = !UseCompactJson,
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