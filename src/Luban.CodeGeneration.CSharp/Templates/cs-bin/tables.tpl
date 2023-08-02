using Luban;

{{namespace_with_grace_begin __namespace}}
public partial class {{__name}}
{
    {{~for table in __tables ~}}
{{~if table.comment != '' ~}}
    /// <summary>
    /// {{escape_comment table.comment}}
    /// </summary>
{{~end~}}
    public {{table.full_name}} {{format_property_name __code_style table.name}} {get; }
    {{~end~}}

    public {{__name}}(System.Func<string, ByteBuf> loader)
    {
        var tables = new System.Collections.Generic.Dictionary<string, object>();
        {{~for table in __tables ~}}
        {{format_property_name __code_style table.name}} = new {{table.full_name}}(loader("{{table.output_data_file}}")); 
        tables.Add("{{table.full_name}}", {{format_property_name __code_style table.name}});
        {{~end~}}
    }
}

{{namespace_with_grace_end __namespace}}