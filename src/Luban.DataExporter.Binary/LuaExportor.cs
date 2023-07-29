using Luban.Datas;
using Luban.DataSources;
using Luban.DataVisitors;
using Luban.Defs;
using System.Text;

namespace Luban.DataExporters;

class LuaExportor
{
    public static LuaExportor Ins { get; } = new LuaExportor();

    public void ExportTableSingleton(DefTable t, Record record, StringBuilder result)
    {
        result.Append("return ").AppendLine();
        result.Append(record.Data.Apply(ToLuaLiteralVisitor.Ins));
    }

    public void ExportTableMap(DefTable t, List<Record> records, StringBuilder s)
    {
        s.Append("return").AppendLine();
        s.Append('{').AppendLine();
        foreach (Record r in records)
        {
            DBean d = r.Data;
            s.Append($"[{d.GetField(t.Index).Apply(ToLuaLiteralVisitor.Ins)}] = ");
            s.Append(d.Apply(ToLuaLiteralVisitor.Ins));
            s.Append(',').AppendLine();
        }
        s.Append('}');
    }

    public void ExportTableList(DefTable t, List<Record> records, StringBuilder s)
    {
        s.Append("return").AppendLine();
        s.Append('{').AppendLine();
        foreach (Record r in records)
        {
            DBean d = r.Data;
            s.Append(d.Apply(ToLuaLiteralVisitor.Ins));
            s.Append(',').AppendLine();
        }
        s.Append('}');
    }
}