﻿using Luban;
{{
    parent_def_type = __bean.parent_def_type
    export_fields = __bean.export_fields
    hierarchy_export_fields = __bean.hierarchy_export_fields
}}

{{namespace_with_grace_begin __namespace_with_top_module}}
{{~if __bean.comment != '' ~}}
/// <summary>
/// {{escape_comment __bean.comment}}
/// </summary>
{{~end~}}
public {{class_modifier __bean}} partial class {{__name}} : {{if parent_def_type}} {{__bean.parent}} {{else}} Luban.BeanBase {{end}}
{
    public {{__name}}(ByteBuf _buf) {{if parent_def_type}} : base(_buf) {{end}}
    {
        {{~ for field in export_fields ~}}
        {{deserialize '_buf' (format_property_name __code_style field.name) field.ctype}}
        {{~end~}}
    }

    public static {{__name}} Deserialize{{__name}}(ByteBuf _buf)
    {
    {{~if __bean.is_abstract_type~}}
        switch (_buf.ReadInt())
        {
        {{~for child in __bean.hierarchy_not_abstract_children~}}
            case {{child.full_name}}.__ID__: return new {{child.full_name}}(_buf);
        {{~end~}}
            default: throw new SerializationException();
        }
    {{~else~}}
        return new {{__bean.full_name}}(_buf);
    {{~end~}}
    }

    {{~ for field in export_fields ~}}
{{~if field.comment != '' ~}}
    /// <summary>
    /// {{field.escape_comment}}
    /// </summary>
{{~end~}}
    public readonly {{declaring_type_name field.ctype}} {{format_property_name __code_style field.name}};
    {{~end~}}

{{~if !__bean.is_abstract_type~}}
    public const int __ID__ = {{__bean.id}};
    public override int GetTypeId() => __ID__;
{{~end~}}

    public override string ToString()
    {
        return "{{full_name}}{ "
    {{~for field in hierarchy_export_fields ~}}
        + "{{format_field_name __code_style field.name}}:" + {{to_pretty_string (format_property_name __code_style field.name) field.ctype}} + ","
    {{~end~}}
        + "}";
    }
}

{{namespace_with_grace_end __namespace_with_top_module}}