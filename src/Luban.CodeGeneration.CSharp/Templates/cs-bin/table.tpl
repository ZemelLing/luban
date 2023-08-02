using Luban;

{{ 
    key_type = __table.key_ttype
    value_type =  __table.value_ttype
    
    func index_type_name
        ret (declaring_type_name $0.type)
    end
    
    func table_union_map_type_name
        ret 'System.Collections.Generic.Dictionary<(' + (array.each __table.index_list @index_type_name | array.join ', ') + '), ' + (declaring_type_name value_type)  + '>'
    end
    
    func record_key_field_name
        ret $0 + (format_property_name __code_style __table.key_field.name)
    end
    
    func table_key_list
        varName = $0
        indexList = __table.index_list |array.each do; ret varName + '.' + (format_property_name __code_style $0.index_field.name); end;
        ret array.join indexList ', '
    end
    
    func table_param_def_list
        paramList = __table.index_list |array.each do; ret (declaring_type_name $0.type) + ' ' + $0.index_field.name; end
        ret array.join paramList ', '
    end
    
    func table_param_name_list
        paramList = __table.index_list |array.each do; ret $0.index_field.name; end
        ret array.join paramList ', '
    end
}}
{{namespace_with_grace_begin __namespace_with_top_module}}
{{~if __table.comment != '' ~}}
/// <summary>
/// {{escape_comment __table.comment}}
/// </summary>
{{~end~}}
public partial class {{__name}}
{
    {{~if __table.is_map_table ~}}
    private readonly System.Collections.Generic.Dictionary<{{declaring_type_name key_type}}, {{declaring_type_name value_type}}> _dataMap;
    private readonly System.Collections.Generic.List<{{declaring_type_name value_type}}> _dataList;
    
    public {{__name}}(ByteBuf _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<{{declaring_type_name key_type}}, {{declaring_type_name value_type}}>();
        _dataList = new System.Collections.Generic.List<{{declaring_type_name value_type}}>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            {{declaring_type_name value_type}} _v;
            {{deserialize '_buf' '_v' value_type}}
            _dataList.Add(_v);
            _dataMap.Add(_v.{{format_property_name __code_style __table.index_field.name}}, _v);
        }
    }

    public System.Collections.Generic.Dictionary<{{declaring_type_name key_type}}, {{declaring_type_name value_type}}> DataMap => _dataMap;
    public System.Collections.Generic.List<{{declaring_type_name value_type}}> DataList => _dataList;

{{~if value_type.is_dynamic~}}
    public T GetOrDefaultAs<T>({{declaring_type_name key_type}} key) where T : {{declaring_type_name value_type}} => _dataMap.TryGetValue(key, out var v) ? (T)v : null;
    public T GetAs<T>({{declaring_type_name key_type}} key) where T : {{declaring_type_name value_type}} => (T)_dataMap[key];
{{~end~}}
    public {{declaring_type_name value_type}} GetOrDefault({{declaring_type_name key_type}} key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public {{declaring_type_name value_type}} Get({{declaring_type_name key_type}} key) => _dataMap[key];
    public {{declaring_type_name value_type}} this[{{declaring_type_name key_type}} key] => _dataMap[key];

        {{~else if __table.is_list_table ~}}
    private readonly System.Collections.Generic.List<{{declaring_type_name value_type}}> _dataList;

    {{~if __table.is_union_index~}}
    private {{table_union_map_type_name}} _dataMapUnion;
    {{~else if !__table.index_list.empty?~}}
    {{~for idx in __table.index_list~}}
    private System.Collections.Generic.Dictionary<{{declaring_type_name idx.type}}, {{declaring_type_name value_type}}> _dataMap_{{idx.index_field.name}};
    {{~end~}}
    {{~end~}}

    public {{__name}}(ByteBuf _buf)
    {
        _dataList = new System.Collections.Generic.List<{{declaring_type_name value_type}}>();
        
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            {{declaring_type_name value_type}} _v;
            {{deserialize '_buf' '_v' value_type}}
            _dataList.Add(_v);
        }
    {{~if __table.is_union_index~}}
        _dataMapUnion = new {{table_union_map_type_name}}();
        foreach(var _v in _dataList)
        {
            _dataMapUnion.Add(({{table_key_list "_v"}}), _v);
        }
    {{~else if !__table.index_list.empty?~}}
    {{~for idx in __table.index_list~}}
        _dataMap_{{idx.index_field.name}} = new System.Collections.Generic.Dictionary<{{declaring_type_name idx.type}}, {{declaring_type_name value_type}}>();
    {{~end~}}
    foreach(var _v in _dataList)
    {
    {{~for idx in __table.index_list~}}
        _dataMap_{{idx.index_field.name}}.Add(_v.{{format_property_name __code_style idx.index_field.name}}, _v);
    {{~end~}}
    }
    {{~end~}}
    }

    public System.Collections.Generic.List<{{declaring_type_name value_type}}> DataList => _dataList;

    {{~if __table.is_union_index~}}
    public {{declaring_type_name value_type}} Get({{table_param_def_list}}) => _dataMapUnion.TryGetValue(({{table_param_name_list}}), out {{declaring_type_name value_type}} __v) ? __v : null;
    {{~else if !__table.index_list.empty? ~}}
        {{~for idx in __table.index_list~}}
    public {{declaring_type_name value_type}} GetBy{{format_property_name __code_style idx.index_field.name}}({{declaring_type_name idx.type}} key) => _dataMap_{{idx.index_field.name}}.TryGetValue(key, out {{declaring_type_name value_type}} __v) ? __v : null;
        {{~end~}}
    {{~end~}}
    {{~else~}}

     private readonly {{declaring_type_name value_type}} _data;

    public {{__name}}(ByteBuf _buf)
    {
        int n = _buf.ReadSize();
        if (n != 1) throw new SerializationException("table mode=one, but size != 1");
        {{deserialize '_buf' '_data' value_type}}
    }


    {{~ for field in value_type.def_bean.hierarchy_export_fields ~}}
{{~if field.comment != '' ~}}
    /// <summary>
    /// {{field.escape_comment}}
    /// </summary>
{{~end~}}
     public {{declaring_type_name field.ctype}} {{format_property_name __code_style field.name}} => _data.{{format_property_name __code_style field.name}};
    {{~end~}}

    {{~end~}}
}

{{namespace_with_grace_end __namespace_with_top_module}}