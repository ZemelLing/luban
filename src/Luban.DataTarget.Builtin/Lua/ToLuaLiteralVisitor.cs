using System.Text;
using Luban.Core.Datas;
using Luban.Core.DataVisitors;
using Luban.Core.Defs;
using Luban.Core.Utils;
using Luban.DataLoader.Builtin;

namespace Luban.DataExporter.Builtin.Lua;

public class ToLuaLiteralVisitor : ToLiteralVisitorBase
{
    public static ToLuaLiteralVisitor Ins { get; } = new();

    public override string Accept(DString type)
    {
        return DataUtil.EscapeLuaStringWithQuote(type.Value);
    }

    public override string Accept(DText type)
    {
        return DataUtil.EscapeLuaStringWithQuote(type.Key);
    }

    public override string Accept(DBean type)
    {
        var x = new StringBuilder();
        if (type.Type.IsAbstractType)
        {
            x.Append($"{{ {FieldNames.LUA_TYPE_NAME_KEY}='{DataUtil.GetImplTypeName(type)}',");
        }
        else
        {
            x.Append('{');
        }

        int index = 0;
        foreach (var f in type.Fields)
        {
            var defField = (DefField)type.ImplType.HierarchyFields[index++];
            if (f == null || !defField.NeedExport())
            {
                continue;
            }
            x.Append(defField.Name).Append('=');
            x.Append(f.Apply(this));
            x.Append(',');
        }
        x.Append('}');
        return x.ToString();
    }


    private void Append(List<DType> datas, StringBuilder x)
    {
        x.Append('{');
        foreach (var e in datas)
        {
            x.Append(e.Apply(this));
            x.Append(',');
        }
        x.Append('}');
    }

    public override string Accept(DArray type)
    {
        var x = new StringBuilder();
        Append(type.Datas, x);
        return x.ToString();
    }

    public override string Accept(DList type)
    {
        var x = new StringBuilder();
        Append(type.Datas, x);
        return x.ToString();
    }

    public override string Accept(DSet type)
    {
        var x = new StringBuilder();
        Append(type.Datas, x);
        return x.ToString();
    }

    public override string Accept(DMap type)
    {
        var x = new StringBuilder();
        x.Append('{');
        foreach (var e in type.Datas)
        {
            x.Append('[');
            x.Append(e.Key.Apply(this));
            x.Append(']');
            x.Append('=');
            x.Append(e.Value.Apply(this));
            x.Append(',');
        }
        x.Append('}');
        return x.ToString();
    }
}