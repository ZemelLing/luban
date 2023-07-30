using System.Collections.Concurrent;
using Luban.Core.Datas;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Types;
using Luban.Core.TypeVisitors;
using Luban.Core.Utils;

namespace Luban.Core;

public class GenerationContext
{
    public static GenerationContext Ins { get; private set; }

    public DefAssembly Assembly { get; set; }
    
    public GenerationArguments Arguments { get; set; }
        
    public string Language { get; set; }

    public RawTarget Target { get; set; }

    private HashSet<string> _overrideOutputTables;

    private readonly HashSet<string> _outputIncludeTables = new();

    private readonly HashSet<string> _outputExcludeTables = new();
    
    

    private readonly ConcurrentDictionary<string, TableDataInfo> _recordsByTables = new();
    
    public bool NeedExport(List<string> groups)
    {
        if (groups.Count == 0)
        {
            return true;
        }
        return groups.Any(g => Target.Groups.Contains(g));
    }
    
    public string TopModule => Target.TopModule;

    public List<DefTypeBase> ExportTypes { get; init; }
    public List<DefTable> ExportTables { get; init; }
    public ConcurrentBag<FileInfo> GenCodeFilesInOutputCodeDir { get; init; }
    public ConcurrentBag<FileInfo> GenDataFilesInOutputDataDir { get; init; }
    public ConcurrentBag<FileInfo> GenScatteredFiles { get; init; }
    public List<Task> Tasks { get; init; }
    
    private readonly Dictionary<string, RawExternalType> _externalTypesByTypeName = new();


    public List<string> CurrentExternalSelectors { get; private set; }

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
                if (!Assembly.ExternalSelectors.Contains(selector))
                {
                    throw new Exception($"未知 external selector:{selector}, 有效值应该为 '{StringUtil.CollectionToString(Assembly.ExternalSelectors)}'");
                }
            }
        }
    }

    public ExternalTypeMapper GetExternalTypeMapper(TType type, string language)
    {
        return GetExternalTypeMapper(type.Apply(RawDefineTypeNameVisitor.Ins), language);
    }

    public ExternalTypeMapper GetExternalTypeMapper(string typeName, string language)
    {
        RawExternalType rawExternalType = _externalTypesByTypeName.GetValueOrDefault(typeName);
        if (rawExternalType == null)
        {
            return null;
        }
        return rawExternalType.Mappers.Find(m => m.Language == language && CurrentExternalSelectors.Contains(m.Selector));
    }

    private static IEnumerable<string> SplitTableList(string tables)
    {
        return tables.Split(',').Select(t => t.Trim());
    }

    public GenerationContext()
    {
        Ins = this;
    }

    public void Init()
    {
        

        if (!string.IsNullOrWhiteSpace(Arguments.OutputTables))
        {
            foreach (var tableFullName in SplitTableList(Arguments.OutputTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:tables 参数中 table:'{tableFullName}' 不存在");
                }
                _overrideOutputTables ??= new HashSet<string>();
                _overrideOutputTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(Arguments.OutputIncludeTables))
        {
            foreach (var tableFullName in SplitTableList(Arguments.OutputIncludeTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:include_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputIncludeTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(Arguments.OutputExcludeTables))
        {
            foreach (var tableFullName in SplitTableList(Arguments.OutputExcludeTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:exclude_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputExcludeTables.Add(tableFullName);
            }
        }
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
        if (Arguments.ExcludeTags.Count == 0)
        {
            return tableDataInfo.FinalRecords;
        }
        else
        {
            var finalRecords = tableDataInfo.FinalRecords.Where(r => r.IsNotFiltered(Arguments.ExcludeTags)).ToList();
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
    
    public List<DefTable> GetExportTables()
    {
        return Assembly.TypeList.Where(t => t is DefTable ct
           && !_outputExcludeTables.Contains(t.FullName)
           && (_outputIncludeTables.Contains(t.FullName) || (_overrideOutputTables == null ? ct.NeedExport() : _overrideOutputTables.Contains(ct.FullName)))
        ).Select(t => (DefTable)t).ToList();
    }
    
    public List<DefTypeBase> GetExportTypes()
    {
        var refTypes = new Dictionary<string, DefTypeBase>();
        foreach (var refType in Target.Refs)
        {
            if (!Assembly.Types.ContainsKey(refType))
            {
                throw new Exception($"target:'{Target.Name}' ref:'{refType}' 类型不存在");
            }
            if (!refTypes.TryAdd(refType, Assembly.Types[refType]))
            {
                throw new Exception($"service:'{Target.Name}' ref:'{refType}' 重复引用");
            }
        }
        foreach (var type in Assembly.TypeList)
        {
            if (!refTypes.ContainsKey(type.FullName) && type is DefEnum)
            {
                refTypes.Add(type.FullName, type);
            }
        }

        foreach (var table in GetExportTables())
        {
            refTypes[table.FullName] = table;
            table.ValueTType.Apply(RefTypeVisitor.Ins, refTypes);
        }

        return refTypes.Values.ToList();
    }
}