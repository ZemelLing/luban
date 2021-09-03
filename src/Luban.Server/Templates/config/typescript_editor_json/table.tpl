{{ 
    name = x.name
    key_type = x.key_ttype
    key_type1 =  x.key_ttype1
    key_type2 =  x.key_ttype2
    value_type =  x.value_ttype
}}
{{x.typescript_namespace_begin}}
{{~if x.comment != '' ~}}
/**
 * {{x.comment}}
 */
{{~end~}}
export class {{name}}{
    {{~if x.is_map_table ~}}
    private _dataMap: Map<{{ts_define_type key_type}}, {{ts_define_type value_type}}>
    private _dataList: {{ts_define_type value_type}}[]
    constructor(_json_: any) {
        this._dataMap = new Map<{{ts_define_type key_type}}, {{ts_define_type value_type}}>()
        this._dataList = []
        for(var _json2_ of _json_) {
            let _v: {{ts_define_type value_type}}
            {{ts_json_constructor '_v' '_json2_' value_type}}
            this._dataList.push(_v)
            this._dataMap.set(_v.{{x.index_field.ts_style_name}}, _v)
        }
    }

    getDataMap(): Map<{{ts_define_type key_type}}, {{ts_define_type value_type}}> { return this._dataMap; }
    getDataList(): {{ts_define_type value_type}}[] { return this._dataList; }

    get(key: {{ts_define_type key_type}}): {{ts_define_type value_type}} | undefined { return this._dataMap.get(key); }

    {{~else~}}

     private _data: {{ts_define_type value_type}}
    constructor(_json_: any) {
        if (_json_.length != 1) throw new Error('table mode=one, but size != 1')
        {{ts_json_constructor 'this._data' '_json_[0]' value_type}}
    }

    getData(): {{ts_define_type value_type}} { return this._data; }

    {{~ for field in value_type.bean.hierarchy_export_fields ~}}
{{~if field.comment != '' ~}}
    /**
     * {{field.comment}}
     */
{{~end~}}
     get  {{field.ts_style_name}}(): {{ts_define_type field.ctype}} { return this._data.{{field.ts_style_name}}; }
    {{~end~}}
    {{end}}
}
{{x.typescript_namespace_end}}