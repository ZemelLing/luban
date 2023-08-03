﻿using System.Text;
using System.Text.Json;
using System.Xml;
using Luban.DataExporter.Builtin.Binary;
using Luban.DataExporter.Builtin.Bson;
using Luban.DataExporter.Builtin.FlatBuffers;
using Luban.DataExporter.Builtin.Json;
using Luban.DataExporter.Builtin.Lua;
using Luban.DataExporter.Builtin.MsgPack;
using Luban.DataExporter.Builtin.Protobuf;
using Luban.DataExporter.Builtin.Res;
using Luban.DataExporter.Builtin.Xml;
using Luban.DataExporter.Builtin.Yaml;

namespace Luban.DataExporter.Builtin;

public static class DataExporterUtil
{
    public static string ToTemplateOutputData(DefTable table, List<Record> records, string templateName)
    {
        Template template = StringTemplateManager.Ins.GetTemplate($"config/data/{templateName}");
        return template.RenderDatas(table, records.Select(r => r.Data).ToList());
    }

    public static object ToOutputData(DefTable table, List<Record> records, string dataType)
    {
        switch (dataType)
        {
            case "data_bin":
            {
                var buf = ThreadLocalTemporalByteBufPool.Alloc(1024 * 1024);
                BinaryDataVisitor.Ins.WriteList(table, records, buf);
                var bytes = buf.CopyData();
                ThreadLocalTemporalByteBufPool.Free(buf);
                return bytes;
            }
            case "data_bidx":
            {
                var buf = ThreadLocalTemporalByteBufPool.Alloc(1024 * 1024);
                BinaryIndexExportor.Ins.WriteList(table, records, buf);
                var bytes = buf.CopyData();
                ThreadLocalTemporalByteBufPool.Free(buf);
                return bytes;
            }
            case "data_json":
            case "data_json2":
            {
                // data_json与data_json2格式区别在于
                // data_json的map格式是 [[key1,value1],[] ..]
                // data_json2的map格式是 { key1:value1, ...}
                var ss = new MemoryStream();
                var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
                {
                    Indented = !table.Assembly.OutputCompactJson,
                    SkipValidation = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
                if (dataType == "data_json")
                {
                    JsonExportor.Ins.WriteAsArray(records, jsonWriter);
                }
                else
                {

                    Json2Exportor.Ins.WriteAsObject(table, records, jsonWriter);
                }
                jsonWriter.Flush();
                return DataUtil.StreamToBytes(ss);
            }
            case "data_yaml":
            {
                var node = YamlExportor.Ins.WriteAsArray(records);
                var ys = new YamlStream(new YamlDocument(node));

                var ms = new MemoryStream();
                var tw = new StreamWriter(ms);
                ys.Save(tw, false);
                tw.Flush();
                return DataUtil.StreamToBytes(ms);
            }
            case "data_bson":
            {
                var ss = new MemoryStream();
                var bsonWriter = new BsonDataWriter(ss);
                BsonDataVisitor.Ins.WriteAsArray(records, bsonWriter);
                bsonWriter.Flush();
                return DataUtil.StreamToBytes(ss);
            }
            case "data_xml":
            {
                var xwSetting = new XmlWriterSettings() { Indent = true };
                var ms = new MemoryStream();
                using var xmlWriter = XmlWriter.Create(ms, xwSetting);
                XmlExportor.Ins.WriteAsArray(records, xmlWriter);
                xmlWriter.Flush();
                return DataUtil.StreamToBytes(ms);
            }
            case "data_lua":
            {
                var content = new StringBuilder();

                switch (table.Mode)
                {
                    case ETableMode.ONE:
                    {
                        LuaExportor.Ins.ExportTableSingleton(table, records[0], content);
                        break;
                    }
                    case ETableMode.MAP:
                    {
                        LuaExportor.Ins.ExportTableMap(table, records, content);
                        break;
                    }
                    case ETableMode.LIST:
                    {
                        LuaExportor.Ins.ExportTableList(table, records, content);
                        break;
                    }
                    default:
                    {
                        throw new NotSupportedException();
                    }
                }
                return string.Join('\n', content);
            }
            case "data_protobuf_bin":
            {
                var ms = new MemoryStream();
                ProtobufBinExportor.Ins.WriteList(table, records, ms);
                return DataUtil.StreamToBytes(ms);
            }
            case "data_protobuf_json":
            {
                var ss = new MemoryStream();
                var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
                {
                    Indented = !table.Assembly.OutputCompactJson,
                    SkipValidation = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                });
                ProtobufJsonExportor.Ins.WriteAsTable(records, jsonWriter);
                jsonWriter.Flush();
                return DataUtil.StreamToBytes(ss);
            }
            case "data_msgpack":
            {
                var ms = new System.Buffers.ArrayBufferWriter<byte>();
                var writer = new MessagePackWriter(ms);
                MsgPackExportor.Ins.WriteList(table, records, ref writer);
                writer.Flush();
                return ms.WrittenSpan.ToArray();
            }
            case "data_flatbuffers_json":
            {
                var ss = new MemoryStream();
                var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
                {
                    Indented = !table.Assembly.OutputCompactJson,
                    SkipValidation = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                });
                FlatBuffersJsonExportor.Ins.WriteAsTable(records, jsonWriter);
                jsonWriter.Flush();
                return DataUtil.StreamToBytes(ss);
            }
            //case "data_erlang":
            //{
            //    var content = new StringBuilder();
            //    switch (table.Mode)
            //    {
            //        case ETableMode.ONE:
            //        {
            //            ErlangExport.Ins.ExportTableSingleton(table, records[0], content);
            //            break;
            //        }
            //        case ETableMode.MAP:
            //        {
            //            ErlangExport.Ins.ExportTableMap(table, records, content);
            //            break;
            //        }
            //        default:
            //        {
            //            throw new NotSupportedException();
            //        }
            //    }
            //    return content.ToString();
            //}
            default:
            {
                throw new ArgumentException($"not support datatype:{dataType}");
            }
        }
    }

    public static List<ResourceInfo> ExportResourceList(List<Record> records)
    {
        var resList = new List<ResourceInfo>();
        foreach (Record res in records)
        {
            ResourceExportor.Ins.Accept(res.Data, TBean.Create(false, res.Data.Type, null), resList);
        }
        return resList;
    }

    public static byte[] GenNotConvertTextList(NotConvertTextSet notConvertSet)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var e in notConvertSet.SortedEntry)
        {
            sb.Append(e.Key).Append('|').Append(e.Value).Append('\n');
        }
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }
}