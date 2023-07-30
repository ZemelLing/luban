using System.Collections.Concurrent;
using Luban.Core.Datas;
using Luban.Core.RawDefs;
using Luban.Core.Types;
using Luban.Core.TypeVisitors;
using Luban.Core.Utils;

namespace Luban.Core.Defs;


public class AssemblyBuilder
{
    public RawDefines RawDefines { get; set; }
    
    public string Target { get; set; }
    
    public string OutputTables { get; set; }

    public string OutputIncludeTables { get; set; }
    
    public string OutputExcludeTables { get; set; }
}


public class DefAssembly
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly AsyncLocal<DefAssembly> _localAssembly = new();

    public static DefAssembly LocalAssebmly
    {
        get
        {
            return _localAssembly.Value;
        }
        set
        {
            _localAssembly.Value = value;
        }
    }

    public static bool IsUseUnityVectors => LocalAssebmly?.CsUseUnityVectors == true;

    public Dictionary<string, DefTypeBase> Types { get; } = new Dictionary<string, DefTypeBase>();

    public List<DefTypeBase> TypeList { get; } = new List<DefTypeBase>();

    private readonly Dictionary<string, DefTypeBase> _notCaseSenseTypes = new();

    private readonly HashSet<string> _namespaces = new();

    private readonly Dictionary<string, DefTypeBase> _notCaseSenseNamespaces = new();

    public string TopModule { get; protected set; }

    public bool SupportDatetimeType { get; protected set; } = false;

    public bool SupportNullable { get; protected set; } = true;

    public bool CsUseUnityVectors { get; set; }

    // public GenArgsBase Args { get; private set; }
    //
    // public NamingConvention NamingConventionModule { get; set; } = NamingConvention.LanguangeRecommend;
    //
    // public NamingConvention NamingConventionType { get; set; } = NamingConvention.LanguangeRecommend;
    //
    // public NamingConvention NamingConventionBeanMember { get; set; } = NamingConvention.LanguangeRecommend;
    //
    // public NamingConvention NamingConventionEnumMember { get; set; } = NamingConvention.LanguangeRecommend;
    //
    // public AccessConvention AccessConventionBeanMember { get; set; } = AccessConvention.LanguangeRecommend;

    public string CurrentLanguage { get; set; }

    public HashSet<string> ExternalSelectors { get; private set; }

    private Dictionary<string, RawExternalType> ExternalTypes { get; set; }

    private readonly Dictionary<string, RawExternalType> _externalTypesByTypeName = new();

    public List<string> CurrentExternalSelectors { get; private set; }

    public Dictionary<string, string> Options { get; private set; }

    public string EditorTopModule { get; private set; }

    public bool ContainsOption(string optionName)
    {
        return Options.ContainsKey(optionName);
    }

    public string GetOption(string optionName)
    {
        return Options.TryGetValue(optionName, out var value) ? value : null;
    }

    public string GetOptionOr(string optionName, string defaultValue)
    {
        return Options.TryGetValue(optionName, out var value) ? value : defaultValue;
    }

    private void SetCurrentExternalSelectors(string selectors)
    {
        if (string.IsNullOrEmpty(selectors))
        {
            CurrentExternalSelectors = new List<string>();
        }
        else
        {

            CurrentExternalSelectors = selectors.Split(',').Select(s => s.Trim()).ToList();
            foreach (var selector in CurrentExternalSelectors)
            {
                if (!ExternalSelectors.Contains(selector))
                {
                    throw new Exception($"未知 externalselector:{selector}, 有效值应该为 '{StringUtil.CollectionToString(ExternalSelectors)}'");
                }
            }
        }
    }

    public RawTarget Target { get; private set; }

    private readonly string _patchName;

    private readonly List<string> _excludeTags;

    public RawPatch TargetRawPatch { get; private set; }

    public TimeZoneInfo TimeZone { get; }

    public bool OutputCompactJson { get; set; }

    public string TableManagerName => Target.Manager;

    public List<string> ExcludeTags => _excludeTags;

    public DefAssembly(string patchName, TimeZoneInfo timezone, List<string> excludeTags)
    {
        this._patchName = patchName;
        this.TimeZone = timezone;
        this._excludeTags = excludeTags;
    }

    public bool NeedExport(List<string> groups)
    {
        if (groups.Count == 0)
        {
            return true;
        }
        return groups.Any(g => Target.Groups.Contains(g));
    }

    private readonly List<RawPatch> _patches = new List<RawPatch>();

    private readonly List<RawTarget> _cfgServices = new List<RawTarget>();

    private readonly Dictionary<string, DefRefGroup> _refGroups = new();

    private readonly ConcurrentDictionary<string, TableDataInfo> _recordsByTables = new();

    public Dictionary<string, DefTable> CfgTablesByName { get; } = new();

    public Dictionary<string, DefTable> CfgTablesByFullName { get; } = new Dictionary<string, DefTable>();

    // public RawTextTable RawTextTable { get; } = new RawTextTable();
    //
    // public TextTable ExportTextTable { get; private set; }
    //
    // public NotConvertTextSet NotConvertTextSet { get; private set; }

    // public bool NeedL10nTextTranslate => ExportTextTable != null;

    private HashSet<string> _overrideOutputTables;

    private readonly HashSet<string> _outputIncludeTables = new();

    private readonly HashSet<string> _outputExcludeTables = new();

    // public void InitL10n(string textValueFieldName)
    // {
    //     ExportTextTable = new TextTable(this, textValueFieldName);
    //     NotConvertTextSet = new NotConvertTextSet();
    // }

    public RawPatch GetPatch(string name)
    {
        return _patches.Find(b => b.Name == name);
    }

    public void AddCfgTable(DefTable table)
    {
        if (!CfgTablesByFullName.TryAdd(table.FullName, table))
        {
            throw new Exception($"table:'{table.FullName}' duplicated");
        }
        if (!CfgTablesByName.TryAdd(table.Name, table))
        {
            throw new Exception($"table:'{table.FullName} 与 table:'{CfgTablesByName[table.Name].FullName}' 的表名重复(不同模块下也不允许定义同名表，将来可能会放开限制)");
        }
    }

    public DefTable GetCfgTable(string name)
    {
        return CfgTablesByFullName.TryGetValue(name, out var t) ? t : null;
    }

    public void AddDataTable(DefTable table, List<Record> mainRecords, List<Record> patchRecords)
    {
        _recordsByTables[table.FullName] = new TableDataInfo(table, mainRecords, patchRecords);
    }

    public List<Record> GetTableAllDataList(DefTable table)
    {
        return _recordsByTables[table.FullName].FinalRecords;
    }

    public List<Record> GetTableExportDataList(DefTable table)
    {
        var tableDataInfo = _recordsByTables[table.FullName];
        if (_excludeTags.Count == 0)
        {
            return tableDataInfo.FinalRecords;
        }
        else
        {
            var finalRecords = tableDataInfo.FinalRecords.Where(r => r.IsNotFiltered(_excludeTags)).ToList();
            if (table.IsOneValueTable && finalRecords.Count != 1)
            {
                throw new Exception($"配置表 {table.FullName} 是单值表 mode=one,但数据个数:{finalRecords.Count} != 1");
            }
            return finalRecords;
        }
    }

    public static List<Record> ToSortByKeyDataList(DefTable table, List<Record> originRecords)
    {
        var sortedRecords = new List<Record>(originRecords);

        DefField keyField = table.IndexField;
        if (keyField != null && (keyField.CType is TInt || keyField.CType is TLong))
        {
            string keyFieldName = keyField.Name;
            sortedRecords.Sort((a, b) =>
            {
                DType keya = a.Data.GetField(keyFieldName);
                DType keyb = b.Data.GetField(keyFieldName);
                switch (keya)
                {
                    case DInt ai: return ai.Value.CompareTo((keyb as DInt).Value);
                    case DLong al: return al.Value.CompareTo((keyb as DLong).Value);
                    default: throw new NotSupportedException();
                }
            });
        }
        return sortedRecords;
    }

    public TableDataInfo GetTableDataInfo(DefTable table)
    {
        return _recordsByTables[table.FullName];
    }

    public List<DefTable> GetAllTables()
    {
        return TypeList.Where(t => t is DefTable).Cast<DefTable>().ToList();
    }

    public List<DefTable> GetExportTables()
    {
        return TypeList.Where(t => t is DefTable ct
                                   && !_outputExcludeTables.Contains(t.FullName)
                                   && (_outputIncludeTables.Contains(t.FullName) || (_overrideOutputTables == null ? ct.NeedExport : _overrideOutputTables.Contains(ct.FullName)))
        ).Select(t => (DefTable)t).ToList();
    }

    public List<DefTypeBase> GetExportTypes()
    {
        var refTypes = new Dictionary<string, DefTypeBase>();
        var targetService = Target;
        foreach (var refType in targetService.Refs)
        {
            if (!this.Types.ContainsKey(refType))
            {
                throw new Exception($"service:'{targetService.Name}' ref:'{refType}' 类型不存在");
            }
            if (!refTypes.TryAdd(refType, this.Types[refType]))
            {
                throw new Exception($"service:'{targetService.Name}' ref:'{refType}' 重复引用");
            }
        }
        foreach (var e in this.Types)
        {
            if (!refTypes.ContainsKey(e.Key) && (e.Value is DefEnum))
            {
                refTypes.Add(e.Key, e.Value);
            }
        }

        foreach (var table in GetExportTables())
        {
            refTypes[table.FullName] = table;
            table.ValueTType.Apply(RefTypeVisitor.Ins, refTypes);
        }

        return refTypes.Values.ToList();
    }

    private void AddRefGroup(RawRefGroup g)
    {
        if (_refGroups.ContainsKey(g.Name))
        {
            throw new Exception($"refgroup:{g.Name} 重复");
        }
        _refGroups.Add(g.Name, new DefRefGroup(g));
    }

    public DefRefGroup GetRefGroup(string groupName)
    {
        return _refGroups.TryGetValue(groupName, out var refGroup) ? refGroup : null;
    }

    private IEnumerable<string> SplitTableList(string tables)
    {
        return tables.Split(',').Select(t => t.Trim());
    }

    public void Load(AssemblyBuilder builder)
    {      
        LocalAssebmly = this;

        RawDefines defines = builder.RawDefines;
        
        this.TopModule = defines.TopModule;
        this.ExternalSelectors = defines.ExternalSelectors;
        this.ExternalTypes = defines.ExternalTypes;
        this.Options = defines.Options;
        this.EditorTopModule = GetOptionOr("editor.topmodule", TypeUtil.MakeFullName("editor", defines.TopModule));
        

        // SetCurrentExternalSelectors(args.ExternalSelectors);
        //
        // CsUseUnityVectors = args.CsUseUnityVectors;
        // NamingConventionModule = args.NamingConventionModule;
        // NamingConventionType = args.NamingConventionType;
        // NamingConventionBeanMember = args.NamingConventionBeanMember;
        // NamingConventionEnumMember = args.NamingConventionEnumMember;
        //
        // OutputCompactJson = args.OutputDataCompactJson;

        SupportDatetimeType = true;

        string targetName = builder.Target;
        Target = defines.Services.Find(s => s.Name == targetName);

        if (Target == null)
        {
            throw new ArgumentException($"target:{targetName} not exists");
        }

        if (!string.IsNullOrWhiteSpace(_patchName))
        {
            TargetRawPatch = defines.Patches.Find(b => b.Name == _patchName);
            if (TargetRawPatch == null)
            {
                throw new Exception($"patch '{_patchName}' not in valid patch set");
            }
        }

        this._patches.AddRange(defines.Patches);

        foreach (var g in defines.RefGroups)
        {
            AddRefGroup(g);
        }

        foreach (var e in defines.Enums)
        {
            AddType(new DefEnum(e));
        }

        foreach (var b in defines.Beans)
        {
            AddType(new DefBean(b));
        }

        foreach (var p in defines.Tables)
        {
            var table = new DefTable(p);
            AddType(table);
            AddCfgTable(table);
        }

        if (!string.IsNullOrWhiteSpace(builder.OutputTables))
        {
            foreach (var tableFullName in SplitTableList(builder.OutputTables))
            {
                if (GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:tables 参数中 table:'{tableFullName}' 不存在");
                }
                _overrideOutputTables ??= new HashSet<string>();
                _overrideOutputTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(builder.OutputIncludeTables))
        {
            foreach (var tableFullName in SplitTableList(builder.OutputIncludeTables))
            {
                if (GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:include_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputIncludeTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(builder.OutputExcludeTables))
        {
            foreach (var tableFullName in SplitTableList(builder.OutputExcludeTables))
            {
                if (GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:exclude_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputExcludeTables.Add(tableFullName);
            }
        }

        _cfgServices.AddRange(defines.Services);

        foreach (var type in TypeList)
        {
            type.Assembly = this;
        }

        foreach (var type in TypeList)
        {
            type.PreCompile();
        }
        foreach (var type in TypeList)
        {
            type.Compile();
        }

        foreach (var type in TypeList)
        {
            type.PostCompile();
        }

        foreach (var externalType in defines.ExternalTypes.Values)
        {
            AddExternalType(externalType);
        }
    }
    
    
    public ExternalTypeMapper GetExternalTypeMapper(TType type)
    {
        return GetExternalTypeMapper(type.Apply(RawDefineTypeNameVisitor.Ins));
    }

    public ExternalTypeMapper GetExternalTypeMapper(string typeName)
    {
        RawExternalType rawExternalType = _externalTypesByTypeName.GetValueOrDefault(typeName);
        if (rawExternalType == null)
        {
            return null;
        }
        return rawExternalType.Mappers.Find(m => m.Lan == CurrentLanguage && CurrentExternalSelectors.Contains(m.Selector));
    }

    public RawExternalType GetExternalType(string typeName)
    {
        return _externalTypesByTypeName.GetValueOrDefault(typeName);
    }

    private static readonly HashSet<string> s_internalOriginTypes = new HashSet<string>
    {
        "datetime",
    };

    public void AddExternalType(RawExternalType type)
    {
        string originTypeName = type.OriginTypeName;
        if (!Types.ContainsKey(originTypeName) && !s_internalOriginTypes.Contains(originTypeName))
        {
            throw new LoadDefException($"externaltype:'{type.Name}' originTypeName:'{originTypeName}' 不存在");
        }
        if (!_externalTypesByTypeName.TryAdd(originTypeName, type))
        {
            throw new LoadDefException($"type:'{originTypeName} 被重复映射. externaltype1:'{type.Name}' exteraltype2:'{_externalTypesByTypeName[originTypeName].Name}'");
        }
    }

    public void AddType(DefTypeBase type)
    {
        string fullName = type.FullName;
        if (Types.ContainsKey(fullName))
        {
            throw new Exception($"type:'{fullName}' duplicate");
        }

        if (!_notCaseSenseTypes.TryAdd(fullName.ToLower(), type))
        {
            throw new Exception($"type:'{fullName}' 和 type:'{_notCaseSenseTypes[fullName.ToLower()].FullName}' 类名小写重复. 在win平台有问题");
        }

        string namespaze = type.Namespace;
        if (_namespaces.Add(namespaze) && !_notCaseSenseNamespaces.TryAdd(namespaze.ToLower(), type))
        {
            throw new Exception($"type:'{fullName}' 和 type:'{_notCaseSenseNamespaces[namespaze.ToLower()].FullName}' 命名空间小写重复. 在win平台有问题，请修改定义并删除生成的代码目录后再重新生成");
        }

        Types.Add(fullName, type);
        TypeList.Add(type);
    }

    public DefTypeBase GetDefType(string fullName)
    {
        return Types.TryGetValue(fullName, out var type) ? type : null;
    }

    public DefTypeBase GetDefType(string module, string type)
    {
        if (Types.TryGetValue(TypeUtil.MakeFullName(module, type), out var t))
        {
            return t;
        }
        else if (Types.TryGetValue(type, out t))
        {
            return t;
        }
        else
        {
            return null;
        }
    }

    private readonly Dictionary<(DefTypeBase, bool), TType> _cacheDefTTypes = new Dictionary<(DefTypeBase, bool), TType>();

    protected TType GetOrCreateTEnum(DefEnum defType, bool nullable, Dictionary<string, string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            if (_cacheDefTTypes.TryGetValue((defType, nullable), out var t))
            {
                return t;
            }
            else
            {
                return _cacheDefTTypes[(defType, nullable)] = TEnum.Create(nullable, defType, tags);
            }
        }
        else
        {
            return TEnum.Create(nullable, defType, tags); ;
        }
    }

    TType GetOrCreateTBean(DefTypeBase defType, bool nullable, Dictionary<string, string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            if (_cacheDefTTypes.TryGetValue((defType, nullable), out var t))
            {
                return t;
            }
            else
            {
                return _cacheDefTTypes[(defType, nullable)] = TBean.Create(nullable, (DefBean)defType, tags);
            }
        }
        else
        {
            return TBean.Create(nullable, (DefBean)defType, tags);
        }
    }

    public TType GetDefTType(string module, string type, bool nullable, Dictionary<string, string> tags)
    {
        var defType = GetDefType(module, type);
        switch (defType)
        {
            case DefBean d: return GetOrCreateTBean(d, nullable, tags);
            case DefEnum d: return GetOrCreateTEnum(d, nullable, tags);
            default: return null;
        }
    }

    public List<T> GetDefTypesByType<T>() where T : DefTypeBase
    {
        return Types.Values.Where(v => typeof(T).IsAssignableFrom(v.GetType())).Select(v => (T)v).ToList();
    }

    public TType CreateType(string module, string type, bool containerElementType)
    {
        type = DefUtil.TrimBracePairs(type);
        int sepIndex = DefUtil.IndexOfBaseTypeEnd(type);
        if (sepIndex > 0)
        {
            string containerTypeAndTags = DefUtil.TrimBracePairs(type.Substring(0, sepIndex));
            var elementTypeAndTags = type.Substring(sepIndex + 1);
            var (containerType, containerTags) = DefUtil.ParseTypeAndVaildAttrs(containerTypeAndTags);
            return CreateContainerType(module, containerType, containerTags, elementTypeAndTags.Trim());
        }
        else
        {
            return CreateNotContainerType(module, type, containerElementType);
        }
    }

    protected TType CreateNotContainerType(string module, string rawType, bool containerElementType)
    {
        bool nullable;
        // 去掉 rawType 两侧的匹配的 ()
        rawType = DefUtil.TrimBracePairs(rawType);
        var (type, tags) = DefUtil.ParseTypeAndVaildAttrs(rawType);

        if (type.EndsWith('?'))
        {
            if (!SupportNullable)
            {
                throw new Exception($"not support nullable type:'{module}.{type}'");
            }
            if (containerElementType)
            {
                throw new Exception($"container element type can't be nullable type:'{module}.{type}'");
            }
            nullable = true;
            type = type.Substring(0, type.Length - 1);
        }
        else
        {
            nullable = false;
        }
        switch (type)
        {
            case "bool": return TBool.Create(nullable, tags);
            case "uint8":
            case "byte": return TByte.Create(nullable, tags);
            case "int16":
            case "short": return TShort.Create(nullable, tags);
            case "int32":
            case "int": return TInt.Create(nullable, tags);
            case "int64":
            case "long": return TLong.Create(nullable, tags, false);
            case "bigint": return TLong.Create(nullable, tags, true);
            case "float32":
            case "float": return TFloat.Create(nullable, tags);
            case "float64":
            case "double": return TDouble.Create(nullable, tags);
            case "string": return TString.Create(nullable, tags);
            case "text": return TText.Create(nullable, tags);
            case "time":
            case "datetime": return SupportDatetimeType ? TDateTime.Create(nullable, tags) : throw new NotSupportedException($"只有配置支持datetime数据类型");
            default:
            {
                var dtype = GetDefTType(module, type, nullable, tags);
                if (dtype != null)
                {
                    return dtype;
                }
                else
                {
                    throw new ArgumentException($"invalid type. module:'{module}' type:'{type}'");
                }
            }
        }
    }

    protected TMap CreateMapType(string module, Dictionary<string, string> tags, string keyValueType, bool isTreeMap)
    {
        int typeSepIndex = DefUtil.IndexOfElementTypeSep(keyValueType);
        if (typeSepIndex <= 0 || typeSepIndex >= keyValueType.Length - 1)
        {
            throw new ArgumentException($"invalid map element type:'{keyValueType}'");
        }
        return TMap.Create(false, tags,
            CreateNotContainerType(module, keyValueType.Substring(0, typeSepIndex).Trim(), true),
            CreateType(module, keyValueType.Substring(typeSepIndex + 1).Trim(), true), isTreeMap);
    }

    protected TType CreateContainerType(string module, string containerType, Dictionary<string, string> containerTags, string elementType)
    {
        switch (containerType)
        {
            case "array":
            {
                return TArray.Create(false, containerTags, CreateType(module, elementType, true));
            }
            case "list": return TList.Create(false, containerTags, CreateType(module, elementType, true), true);
            case "set":
            {
                TType type = CreateType(module, elementType, true);
                if (type.IsCollection)
                {
                    throw new Exception("set的元素不支持容器类型");
                }
                return TSet.Create(false, containerTags, type, false);
            }
            case "map": return CreateMapType(module, containerTags, elementType, false);
            default:
            {
                throw new ArgumentException($"invalid container type. module:'{module}' container:'{containerType}' element:'{elementType}'");
            }
        }
    }
}