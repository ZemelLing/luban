using System.Text;
using Luban.Core;
using Luban.Core.Datas;
using Luban.Core.DataTarget;
using Luban.Core.Defs;

namespace Luban.DataExporter.Builtin.Lua;

[DataTarget("lua")]
public class LuaDataTarget : DataTargetBase
{
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

    protected override string OutputFileExt => "lua";
    
    public override OutputFile ExportTable(DefTable table, List<Record> records)
    {
        var ss = new StringBuilder();
        if (table.IsMapTable)
        {
            ExportTableMap(table, records, ss);
        }
        else if (table.IsSingletonTable)
        {
            ExportTableSingleton(table, records[0], ss);
        }
        else
        {
            ExportTableList(table, records, ss);
        }
        return new OutputFile()
        {
            File = $"{table.OutputDataFile}.{OutputFileExt}",
            Content = ss.ToString(),
        };
    }
}