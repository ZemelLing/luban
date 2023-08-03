﻿using System.Collections.Concurrent;
using Luban.Core.CodeFormat;
using Luban.Core.DataLoader;
using Luban.Core.Datas;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Types;
using Luban.Core.TypeVisitors;
using Luban.Core.Utils;

namespace Luban.Core;

public class GenerationContext
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    public static GenerationContext Ins { get; private set; }

    public DefAssembly Assembly { get; set; }
    
    public GenerationArguments Arguments { get; set; }

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
    
    public bool NeedExportNotDefault(List<string> groups)
    {
        return groups.Any(g => Target.Groups.Contains(g));
    }
    
    public string TopModule => Target.TopModule;

    public List<DefTable> Tables => Assembly.GetAllTables();
    
    private List<DefTypeBase> ExportTypes { get; }
    
    public List<DefTable> ExportTables { get; }
    
    public List<DefBean> ExportBeans { get; }
    
    public List<DefEnum> ExportEnums { get; }
    
    private readonly Dictionary<string, RawExternalType> _externalTypesByTypeName = new();

    private bool _dataLoaded;

    public void LoadDatas()
    {
        lock(this)
        {
            if (_dataLoaded)
            {
                return;
            }
            DataLoaderManager.Ins.LoadDatas(this);
            _dataLoaded = true;
        }
    }

    public GenerationContext(DefAssembly assembly, GenerationArguments args)
    {
        Ins = this;
        Assembly = assembly;
        Arguments = args;
        
        Target = assembly.GetTarget(args.Target);

        InitTables(args);
        ExportTables = CalculateExportTables();
        ExportTypes = CalculateExportTypes();
        ExportBeans = ExportTypes.OfType<DefBean>().ToList();
        ExportEnums = ExportTypes.OfType<DefEnum>().ToList();
    }

    private void InitTables(GenerationArguments args)
    {
        if (!string.IsNullOrWhiteSpace(args.OutputTables))
        {
            foreach (var tableFullName in SplitTableList(args.OutputTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:tables 参数中 table:'{tableFullName}' 不存在");
                }
                _overrideOutputTables ??= new HashSet<string>();
                _overrideOutputTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(args.OutputIncludeTables))
        {
            foreach (var tableFullName in SplitTableList(args.OutputIncludeTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:include_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputIncludeTables.Add(tableFullName);
            }
        }
        if (!string.IsNullOrWhiteSpace(args.OutputExcludeTables))
        {
            foreach (var tableFullName in SplitTableList(args.OutputExcludeTables))
            {
                if (Assembly.GetCfgTable(tableFullName) == null)
                {
                    throw new Exception($"--output:exclude_tables 参数中 table:'{tableFullName}' 不存在");
                }
                _outputExcludeTables.Add(tableFullName);
            }
        }
    }
    
    
    private List<DefTable> CalculateExportTables()
    {
        return Assembly.TypeList.Where(t => t is DefTable ct
                                            && !_outputExcludeTables.Contains(t.FullName)
                                            && (_outputIncludeTables.Contains(t.FullName) || (_overrideOutputTables == null ? ct.NeedExport() : _overrideOutputTables.Contains(ct.FullName)))
        ).Select(t => (DefTable)t).ToList();
    }
    
    private List<DefTypeBase> CalculateExportTypes()
    {
        var refTypes = new Dictionary<string, DefTypeBase>();
        var types = Assembly.TypeList;
        // var targetService = CfgTargetService;
        // foreach (var refType in targetService.Refs)
        // {
        //     if (!this.Types.ContainsKey(refType))
        //     {
        //         throw new Exception($"service:'{targetService.Name}' ref:'{refType}' 类型不存在");
        //     }
        //     if (!refTypes.TryAdd(refType, this.Types[refType]))
        //     {
        //         throw new Exception($"service:'{targetService.Name}' ref:'{refType}' 重复引用");
        //     }
        // }
        foreach (var t in types)
        {
            if (!refTypes.ContainsKey(t.FullName))
            {
                if (t is DefBean bean && NeedExportNotDefault(t.Groups))
                {
                    TBean.Create(false, bean, null).Apply(RefTypeVisitor.Ins, refTypes);
                    refTypes.Add(t.FullName, t);
                }
                else if (t is DefEnum)
                {
                    refTypes.Add(t.FullName, t);
                }
            }
        }

        foreach (var table in ExportTables)
        {
            refTypes[table.FullName] = table;
            table.ValueTType.Apply(RefTypeVisitor.Ins, refTypes);
        }

        return refTypes.Values.ToList();
    }

    public bool HasEnv(string name)
    {
        return Assembly.Envs.ContainsKey(name);
    }
    
    public string GetEnv(string name)
    {
        return Assembly.Envs[name];
    }
    
    public string GetEnvOrDefault(string name, string defaultValue)
    {
        return Assembly.Envs.TryGetValue(name, out var value) ? value : defaultValue;
    }

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
    
    public void AddDataTable(DefTable table, List<Record> mainRecords, List<Record> patchRecords)
    {
        s_logger.Debug("AddDataTable name:{} record count:{}", table.FullName, mainRecords.Count);
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

    public string GetInputDataPath()
    {
        return Arguments.GetOption("", "inputDataDir", true);
    }
    
    public string GetOutputCodePath(string family)
    {
        return Arguments.GetOption(family, "outputCodeDir", true);
    }
    
    public string GetOutputDataPath(string family)
    {
        return Arguments.GetOption(family, "outputDataDir", true);
    }
    
    public string GetOption(string family, string name, bool useGlobalIfNotExits)
    {
        return Arguments.GetOption(family, name, useGlobalIfNotExits);
    }
    
    public bool TryGetOption(string family, string name, bool useGlobalIfNotExits, out string value)
    {
        return Arguments.TryGetOption(family, name, useGlobalIfNotExits, out value);
    }
    
    public string GetOptionOrDefault(string family, string name, bool useGlobalIfNotExits, string defaultValue)
    {
        return Arguments.TryGetOption(family, name, useGlobalIfNotExits, out string value) ? value : defaultValue;
    }
    
    public bool GetBoolOptionOrDefault(string family, string name, bool useGlobalIfNotExits, bool defaultValue)
    {
        if (Arguments.TryGetOption(family, name, useGlobalIfNotExits, out string value))
        {
            switch (value.ToLowerInvariant())
            {
                case "0":
                case "false": return false;
                case "1":
                case "true": return true;
                default: throw new Exception($"invalid bool option value:{value}");
            }   
        }
        return defaultValue;
    }

    public ICodeStyle GetCodeStyle(string family)
    {
        if (Arguments.TryGetOption(family, "codeStyle", true, out var codeStyleName))
        {
            return CodeFormatManager.Ins.GetCodeStyle(codeStyleName);
        }
        return null;
    }
}