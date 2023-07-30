using System.Xml.Linq;
using Luban.Core.RawDefs;

namespace Luban.Plugin.Loader;

public class DefLoader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    
    public string RootDir { get; private set; }

    public bool IsBeanDefaultCompatible { get; protected set; }

    public bool IsBeanFieldMustDefineId { get; protected set; }

    private readonly Dictionary<string, Action<XElement>> _rootDefineHandlers = new Dictionary<string, Action<XElement>>();
    private readonly Dictionary<string, Action<string, XElement>> _moduleDefineHandlers = new();

    protected readonly Stack<string> _namespaceStack = new Stack<string>();

    protected string TopModule { get; private set; }

    protected readonly List<RawEnum> _enums = new List<RawEnum>();
    protected readonly List<RawBean> _beans = new List<RawBean>();
    protected readonly HashSet<string> _externalSelectors = new();
    protected readonly Dictionary<string, RawExternalType> _externalTypes = new();

    protected readonly Dictionary<string, string> _options = new();
    
    private readonly List<string> _importExcelTableFiles = new();
    private readonly List<string> _importExcelEnumFiles = new();
    private readonly List<string> _importExcelBeanFiles = new();


    private readonly List<RawPatch> _patches = new();

    private readonly List<RawTable> _cfgTables = new List<RawTable>();

    private readonly List<RawTarget> _cfgServices = new List<RawTarget>();

    private readonly List<RawGroup> _cfgGroups = new List<RawGroup>();

    private readonly List<string> _defaultGroups = new List<string>();

    private readonly List<RawRefGroup> _refGroups = new();

    public DefLoader()
    {
        
        _rootDefineHandlers.Add("topmodule", SetTopModule);
        _rootDefineHandlers.Add("option", AddOption);
        _rootDefineHandlers.Add("externalselector", AddExternalSelector);

        _moduleDefineHandlers.Add("module", AddModule);
        _moduleDefineHandlers.Add("enum", AddEnum);
        _moduleDefineHandlers.Add("bean", AddBean);
        _moduleDefineHandlers.Add("externaltype", AddExternalType);
        
        RegisterRootDefineHandler("importexcel", AddImportExcel);
        RegisterRootDefineHandler("patch", AddPatch);
        RegisterRootDefineHandler("service", AddService);
        RegisterRootDefineHandler("group", AddGroup);

        RegisterModuleDefineHandler("table", AddTable);
        RegisterModuleDefineHandler("refgroup", AddRefGroup);


        IsBeanFieldMustDefineId = false;
    }
    
    
    public string RootXml => _rootXml;
    private string _rootXml;

    public void LoadAsync(string rootXml)
    {
        _rootXml = rootXml;

        RootDir = FileUtil.GetParent(rootXml);

        XElement doc = XmlUtil.Open(rootXml);

        foreach (XElement e in doc.Elements())
        {
            var tagName = e.Name.LocalName;
            if (tagName == "import")
            {
                AddImport(XmlUtil.GetRequiredAttribute(e, "name"));
                continue;
            }
            if (_rootDefineHandlers.TryGetValue(tagName, out var handler))
            {
                handler(e);
            }
            else
            {
                throw new LoadDefException($"定义文件:{rootXml} 非法 tag:{tagName}");
            }
        }
    }

    protected void RegisterRootDefineHandler(string name, Action<XElement> handler)
    {
        _rootDefineHandlers.Add(name, handler);
    }

    protected void RegisterModuleDefineHandler(string name, Action<string, XElement> handler)
    {
        _moduleDefineHandlers.Add(name, handler);
    }

    protected string CurNamespace => _namespaceStack.Count > 0 ? _namespaceStack.Peek() : "";
    

    #region root handler

    private void SetTopModule(XElement e)
    {
        this.TopModule = XmlUtil.GetOptionalAttribute(e, "name");
    }

    private static readonly List<string> _optionRequireAttrs = new List<string> { "name", "value", };

    private void AddOption(XElement e)
    {
        ValidAttrKeys(_rootXml, e, null, _optionRequireAttrs);
        string name = XmlUtil.GetRequiredAttribute(e, "name");
        if (!_options.TryAdd(name, XmlUtil.GetRequiredAttribute(e, "value")))
        {
            throw new LoadDefException($"option name:'{name}' duplicate");
        }
    }

    private void AddImport(string xmlFile)
    {
        var rootFileName = FileUtil.GetFileName(_rootXml);

        var xmlFullPath = FileUtil.Combine(RootDir, xmlFile);
        s_logger.Trace("import {file} {full_path}", xmlFile, xmlFullPath);

        var fileOrDirContent = await Agent.GetFileOrDirectoryAsync(xmlFullPath, ".xml");

        if (fileOrDirContent.IsFile)
        {
            s_logger.Trace("== file:{file}", xmlFullPath);
            AddModule(xmlFullPath, XmlUtil.Open(xmlFullPath, await Agent.GetFromCacheOrReadAllBytesAsync(xmlFullPath, fileOrDirContent.Md5)));
        }
        else
        {
            // 如果是目录,则递归导入目录下的所有 .xml 定义文件
            foreach (var subFile in fileOrDirContent.SubFiles)
            {
                var subFileName = subFile.FilePath;
                s_logger.Trace("sub import xmlfile:{file} root file:{root}", subFileName, rootFileName);
                // 有时候 root 定义文件会跟 module定义文件放在一个目录. 当以目录形式导入子module时，不希望导入它
                if (FileUtil.GetFileName(subFileName) == rootFileName)
                {
                    s_logger.Trace("ignore import root file:{root}", subFileName);
                    continue;
                }
                string subFullPath = subFileName;
                AddModule(subFullPath, XmlUtil.Open(subFullPath, await Agent.GetFromCacheOrReadAllBytesAsync(subFullPath, subFile.MD5)));
            }
        }
    }

    #endregion


    #region module handler

    private void AddModule(string defineFile, XElement me)
    {
        var name = XmlUtil.GetOptionalAttribute(me, "name")?.Trim();
        //if (string.IsNullOrEmpty(name))
        //{
        //    throw new LoadDefException($"xml:{CurImportFile} contains module which's name is empty");
        //}

        _namespaceStack.Push(_namespaceStack.Count > 0 ? TypeUtil.MakeFullName(_namespaceStack.Peek(), name) : name);

        // 加载所有module定义,允许嵌套
        foreach (XElement e in me.Elements())
        {
            var tagName = e.Name.LocalName;
            if (_moduleDefineHandlers.TryGetValue(tagName, out var handler))
            {
                if (tagName != "module")
                {
                    handler(defineFile, e);
                }
                else
                {
                    handler(defineFile, e);
                }
            }
            else
            {
                throw new LoadDefException($"定义文件:{defineFile} module:{CurNamespace} 不支持 tag:{tagName}");
            }
        }
        _namespaceStack.Pop();
    }

    protected void AddBean(string defineFile, XElement e)
    {
        AddBean(defineFile, e, "");
    }

    private static readonly List<string> _beanOptinsAttrs1 = new List<string> { "compatible", "value_type", "comment", "tags", "externaltype" };
    private static readonly List<string> _beanRequireAttrs1 = new List<string> { "id", "name" };

    private static readonly List<string> _beanOptinsAttrs2 = new List<string> { "id", "parent", "compatible", "value_type", "comment", "tags"};
    private static readonly List<string> _beanRequireAttrs2 = new List<string> { "name" };


    protected void TryGetUpdateParent(XElement e, ref string parent)
    {
        string selfDefParent = XmlUtil.GetOptionalAttribute(e, "parent");
        if (!string.IsNullOrEmpty(selfDefParent))
        {
            if (!string.IsNullOrEmpty(parent))
            {
                throw new Exception($"嵌套在'{parent}'中定义的子bean:'{XmlUtil.GetRequiredAttribute(e, "name")}' 不能再定义parent:{selfDefParent} 属性");
            }
            parent = selfDefParent;
        }
    }

    static string CreateType(XElement e, string key)
    {
        return XmlUtil.GetRequiredAttribute(e, key);
    }

    protected void ValidAttrKeys(string defineFile, XElement e, List<string> optionKeys, List<string> requireKeys)
    {
        foreach (var k in e.Attributes())
        {
            var name = k.Name.LocalName;
            if (!requireKeys.Contains(name) && (optionKeys != null && !optionKeys.Contains(name)))
            {
                throw new LoadDefException($"定义文件:{defineFile} module:{CurNamespace} 定义:{e} 包含未知属性 attr:{name}");
            }
        }
        foreach (var k in requireKeys)
        {
            if (e.Attribute(k) == null)
            {
                throw new LoadDefException($"定义文件:{defineFile} module:{CurNamespace} 定义:{e} 缺失属性 attr:{k}");
            }
        }
    }

    private static readonly List<string> _enumOptionalAttrs = new List<string> { "flags", "comment", "tags", "unique" };
    private static readonly List<string> _enumRequiredAttrs = new List<string> { "name" };


    private static readonly List<string> _enumItemOptionalAttrs = new List<string> { "value", "alias", "comment", "tags" };
    private static readonly List<string> _enumItemRequiredAttrs = new List<string> { "name" };

    protected void AddEnum(string defineFile, XElement e)
    {
        ValidAttrKeys(defineFile, e, _enumOptionalAttrs, _enumRequiredAttrs);
        var en = new RawEnum()
        {
            Name = XmlUtil.GetRequiredAttribute(e, "name").Trim(),
            Namespace = CurNamespace,
            Comment = XmlUtil.GetOptionalAttribute(e, "comment"),
            IsFlags = XmlUtil.GetOptionBoolAttribute(e, "flags"),
            Tags = XmlUtil.GetOptionalAttribute(e, "tags"),
            IsUniqueItemId = XmlUtil.GetOptionBoolAttribute(e, "unique", true),
        };

        foreach (XElement item in e.Elements())
        {
            ValidAttrKeys(defineFile, item, _enumItemOptionalAttrs, _enumItemRequiredAttrs);
            en.Items.Add(new EnumItem()
            {
                Name = XmlUtil.GetRequiredAttribute(item, "name"),
                Alias = XmlUtil.GetOptionalAttribute(item, "alias"),
                Value = XmlUtil.GetOptionalAttribute(item, "value"),
                Comment = XmlUtil.GetOptionalAttribute(item, "comment"),
                Tags = XmlUtil.GetOptionalAttribute(item, "tags"),
            });
        }
        s_logger.Trace("add enum:{@enum}", en);
        _enums.Add(en);
    }

    private static readonly List<string> _selectorRequiredAttrs = new List<string> { "name" };
    private void AddExternalSelector(XElement e)
    {
        ValidAttrKeys(_rootXml, e, null, _selectorRequiredAttrs);
        string name = XmlUtil.GetRequiredAttribute(e, "name");
        if (!_externalSelectors.Add(name))
        {
            throw new LoadDefException($"定义文件:{_rootXml} externalselector name:{name} 重复");
        }
        s_logger.Trace("add selector:{}", name);
    }

    private static readonly List<string> _externalRequiredAttrs = new List<string> { "name", "origin_type_name" };
    private void AddExternalType(string defineFile, XElement e)
    {
        ValidAttrKeys(_rootXml, e, null, _externalRequiredAttrs);
        string name = XmlUtil.GetRequiredAttribute(e, "name");

        if (_externalTypes.ContainsKey(name))
        {
            throw new LoadDefException($"定义文件:{_rootXml} externaltype:{name} 重复");
        }

        var et = new RawExternalType()
        {
            Name = name,
            OriginTypeName = XmlUtil.GetRequiredAttribute(e, "origin_type_name"),
        };
        var mappers = new Dictionary<string, ExternalTypeMapper>();
        foreach (XElement mapperEle in e.Elements())
        {
            var tagName = mapperEle.Name.LocalName;
            if (tagName == "mapper")
            {
                var mapper = CreateMapper(defineFile, name, mapperEle);
                string uniqKey = $"{mapper.Lan}##{mapper.Selector}";
                if (mappers.ContainsKey(uniqKey))
                {
                    throw new LoadDefException($"定义文件:{_rootXml} externaltype name:{name} mapper(lan='{mapper.Lan}',selector='{mapper.Selector}') 重复");
                }
                mappers.Add(uniqKey, mapper);
                et.Mappers.Add(mapper);
                s_logger.Trace("add mapper. externaltype:{} mapper:{@}", name, mapper);
            }
            else
            {
                throw new LoadDefException($"定义文件:{defineFile} externaltype:{name} 非法 tag:'{tagName}'");
            }
        }
        _externalTypes.Add(name, et);
    }

    private static readonly List<string> _mapperOptionalAttrs = new List<string> { };
    private static readonly List<string> _mapperRequiredAttrs = new List<string> { "lan", "selector" };
    private ExternalTypeMapper CreateMapper(string defineFile, string externalType, XElement e)
    {
        ValidAttrKeys(_rootXml, e, _mapperOptionalAttrs, _mapperRequiredAttrs);
        var m = new ExternalTypeMapper()
        {
            Lan = XmlUtil.GetRequiredAttribute(e, "lan"),
            Selector = XmlUtil.GetRequiredAttribute(e, "selector"),
        };
        foreach (XElement attrEle in e.Elements())
        {
            var tagName = attrEle.Name.LocalName;
            switch (tagName)
            {
                case "target_type_name":
                {
                    m.TargetTypeName = attrEle.Value;
                    break;
                }
                case "create_external_object_function":
                {
                    m.CreateExternalObjectFunction = attrEle.Value;
                    break;
                }
                default: throw new LoadDefException($"定义文件:{defineFile} externaltype:{externalType} 非法 tag:{tagName}");
            }
        }
        if (string.IsNullOrWhiteSpace(m.TargetTypeName))
        {
            throw new LoadDefException($"定义文件:{defineFile} externaltype:{externalType} lan:{m.Lan} selector:{m.Selector} 没有定义 'target_type_name'");
        }
        return m;
    }
    #endregion

    public RawAssembly BuildDefines()
    {
        var defines = new RawAssembly()
        {
            Patches = _patches,
            Tables = _cfgTables,
            Targets = _cfgServices,
            Groups = _cfgGroups,
            RefGroups = _refGroups,
        };
        
        defines.TopModule = TopModule;
        defines.Enums = _enums;
        defines.Beans = _beans;
        defines.ExternalSelectors = _externalSelectors;
        defines.ExternalTypes = _externalTypes;
        defines.Options = _options;
        return defines;
    }

    private static readonly List<string> _excelImportRequireAttrs = new List<string> { "name", "type" };
    private void AddImportExcel(XElement e)
    {
        ValidAttrKeys(RootXml, e, null, _excelImportRequireAttrs);
        var importName = XmlUtil.GetRequiredAttribute(e, "name");
        if (string.IsNullOrWhiteSpace(importName))
        {
            throw new Exception("importexcel 属性name不能为空");
        }
        var type = XmlUtil.GetRequiredAttribute(e, "type");
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new Exception($"importexcel name:'{importName}' type属性不能为空");
        }
        switch (type)
        {
            case "table": this._importExcelTableFiles.Add(importName); break;
            case "enum": this._importExcelEnumFiles.Add(importName); break;
            case "bean": this._importExcelBeanFiles.Add(importName); break;
            default: throw new Exception($"importexcel name:'{importName}' type:'{type}' 不合法. 有效值为 table|enum|bean");
        }
    }

    private static readonly List<string> _patchRequireAttrs = new List<string> { "name" };
    private void AddPatch(XElement e)
    {
        ValidAttrKeys(RootXml, e, null, _patchRequireAttrs);
        var patchName = e.Attribute("name").Value;
        if (string.IsNullOrWhiteSpace(patchName))
        {
            throw new Exception("patch 属性name不能为空");
        }
        if (this._patches.Any(b => b.Name == patchName))
        {
            throw new Exception($"patch '{patchName}' 重复");
        }
        _patches.Add(new RawPatch(patchName));
    }

    private static readonly List<string> _groupOptionalAttrs = new List<string> { "default" };
    private static readonly List<string> _groupRequireAttrs = new List<string> { "name" };

    private void AddGroup(XElement e)
    {
        ValidAttrKeys(RootXml, e, _groupOptionalAttrs, _groupRequireAttrs);
        List<string> groupNames = CreateGroups(e.Attribute("name").Value);

        foreach (var g in groupNames)
        {
            if (_cfgGroups.Any(cg => cg.Names.Contains(g)))
            {
                throw new Exception($"group名:'{g}' 重复");
            }
        }

        if (XmlUtil.GetOptionBoolAttribute(e, "default"))
        {
            this._defaultGroups.AddRange(groupNames);
        }
        _cfgGroups.Add(new RawGroup() { Names = groupNames });
    }

    private readonly List<string> _serviceAttrs = new List<string> { "name", "manager", "group" };

    private void AddService(XElement e)
    {
        var name = XmlUtil.GetRequiredAttribute(e, "name");
        var manager = XmlUtil.GetRequiredAttribute(e, "manager");
        List<string> groups = CreateGroups(XmlUtil.GetOptionalAttribute(e, "group"));
        var refs = new List<string>();

        s_logger.Trace("service name:{name} manager:{manager}", name, manager);
        ValidAttrKeys(RootXml, e, _serviceAttrs, _serviceAttrs);
        foreach (XElement ele in e.Elements())
        {
            string tagName = ele.Name.LocalName;
            s_logger.Trace("service {service_name} tag: {name} {value}", name, tagName, ele);
            switch (tagName)
            {
                case "ref":
                {
                    refs.Add(XmlUtil.GetRequiredAttribute(ele, "name"));
                    break;
                }
                default:
                {
                    throw new Exception($"service:'{name}' tag:'{tagName}' 非法");
                }
            }
        }
        if (!ValidGroup(groups, out var invalidGroup))
        {
            throw new Exception($"service:'{name}' group:'{invalidGroup}' 不存在");
        }
        _cfgServices.Add(new RawTarget() { Name = name, Manager = manager, Groups = groups, Refs = refs });
    }

    private static List<string> CreateGroups(string s)
    {
        return s.Split(',', ';').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    private bool ValidGroup(List<string> groups, out string invalidGroup)
    {
        foreach (var g in groups)
        {
            if (!this._cfgGroups.Any(cg => cg.Names.Contains(g)))
            {
                invalidGroup = g;
                return false;
            }
        }
        invalidGroup = null;
        return true;
    }

    private TableMode ConvertMode(string defineFile, string tableName, string modeStr, string indexStr)
    {
        TableMode mode;
        string[] indexs = indexStr.Split(',', '+');
        switch (modeStr)
        {
            case "1":
            case "one":
            case "single":
            case "singleton":
            {
                if (!string.IsNullOrWhiteSpace(indexStr))
                {
                    throw new Exception($"定义文件:{defineFile} table:'{tableName}' mode={modeStr} 是单例表，不支持定义index属性");
                }
                mode = TableMode.ONE;
                break;
            }
            case "map":
            {
                if (!string.IsNullOrWhiteSpace(indexStr) && indexs.Length > 1)
                {
                    throw new Exception($"定义文件:'{defineFile}' table:'{tableName}' 是单主键表，index:'{indexStr}'不能包含多个key");
                }
                mode = TableMode.MAP;
                break;
            }
            case "list":
            {
                mode = TableMode.LIST;
                break;
            }
            case "":
            {
                if (string.IsNullOrWhiteSpace(indexStr) || indexs.Length == 1)
                {
                    mode = TableMode.MAP;
                }
                else
                {
                    mode = TableMode.LIST;
                }
                break;
            }
            default:
            {
                throw new ArgumentException($"不支持的 mode:{modeStr}");
            }
        }
        return mode;
    }

    private readonly List<string> _tableOptionalAttrs = new List<string> { "index", "mode", "group", "patch_input", "comment", "define_from_file", "output", "options" };
    private readonly List<string> _tableRequireAttrs = new List<string> { "name", "value", "input" };

    private void AddTable(string defineFile, XElement e)
    {
        ValidAttrKeys(defineFile, e, _tableOptionalAttrs, _tableRequireAttrs);
        string name = XmlUtil.GetRequiredAttribute(e, "name");
        string module = CurNamespace;
        string valueType = XmlUtil.GetRequiredAttribute(e, "value");
        bool defineFromFile = XmlUtil.GetOptionBoolAttribute(e, "define_from_file");
        string index = XmlUtil.GetOptionalAttribute(e, "index");
        string group = XmlUtil.GetOptionalAttribute(e, "group");
        string comment = XmlUtil.GetOptionalAttribute(e, "comment");
        string input = XmlUtil.GetRequiredAttribute(e, "input");
        string patchInput = XmlUtil.GetOptionalAttribute(e, "patch_input");
        string mode = XmlUtil.GetOptionalAttribute(e, "mode");
        string tags = XmlUtil.GetOptionalAttribute(e, "tags");
        string output = XmlUtil.GetOptionalAttribute(e, "output");
        string options = XmlUtil.GetOptionalAttribute(e, "options");
        AddTable(defineFile, name, module, valueType, index, mode, group, comment, defineFromFile, input, patchInput, tags, output, options);
    }

    private void AddTable(string defineFile, string name, string module, string valueType, string index, string mode, string group,
        string comment, bool defineFromExcel, string input, string patchInput, string tags, string outputFileName, string options)
    {
        var p = new RawTable()
        {
            Name = name,
            Namespace = module,
            ValueType = valueType,
            LoadDefineFromFile = defineFromExcel,
            Index = index,
            Groups = CreateGroups(group),
            Comment = comment,
            Mode = ConvertMode(defineFile, name, mode, index),
            Tags = tags,
            OutputFile = outputFileName,
            Options = options,
        };
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception($"定义文件:{defineFile} table:'{p.Name}' name:'{p.Name}' 不能为空");
        }
        if (string.IsNullOrWhiteSpace(valueType))
        {
            throw new Exception($"定义文件:{defineFile} table:'{p.Name}' value_type:'{valueType}' 不能为空");
        }
        if (p.Groups.Count == 0)
        {
            p.Groups = this._defaultGroups;
        }
        else if (!ValidGroup(p.Groups, out var invalidGroup))
        {
            throw new Exception($"定义文件:{defineFile} table:'{p.Name}' group:'{invalidGroup}' 不存在");
        }
        p.InputFiles.AddRange(input.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(patchInput))
        {
            foreach (var subPatchStr in patchInput.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var nameAndDirs = subPatchStr.Split(':');
                if (nameAndDirs.Length != 2)
                {
                    throw new Exception($"定义文件:{defineFile} table:'{p.Name}' patch_input:'{subPatchStr}' 定义不合法");
                }
                var patchDirs = nameAndDirs[1].Split(',', ';').ToList();
                if (!p.PatchInputFiles.TryAdd(nameAndDirs[0], patchDirs))
                {
                    throw new Exception($"定义文件:{defineFile} table:'{p.Name}' patch_input:'{subPatchStr}' 子patch:'{nameAndDirs[0]}' 重复");
                }
            }
        }

        _cfgTables.Add(p);
    }

    private async Task<RawBean> LoadTableValueTypeDefineFromFileAsync(RawTable rawTable, string dataDir)
    {
        var inputFileInfos = await DataLoaderUtil.CollectInputFilesAsync(rawTable.InputFiles, dataDir);
        var file = inputFileInfos[0];
        RawSheetTableDefInfo tableDefInfo;
        if (!ExcelTableValueTypeDefInfoCacheManager.Instance.TryGetTableDefInfo(file.MD5, file.SheetName, out tableDefInfo))
        {
            var source = new ExcelRowColumnDataSource();
            var stream = new MemoryStream(await this.Agent.GetFromCacheOrReadAllBytesAsync(file.ActualFile, file.MD5));
            tableDefInfo = source.LoadTableDefInfo(file.OriginFile, file.SheetName, stream);
            ExcelTableValueTypeDefInfoCacheManager.Instance.AddTableDefInfoToCache(file.MD5, file.SheetName, tableDefInfo);
        }

        var (valueType, tags) = DefUtil.ParseType(rawTable.ValueType);
        var ns = TypeUtil.GetNamespace(valueType);
        string valueTypeNamespace = string.IsNullOrEmpty(ns) ? rawTable.Namespace : ns;
        string valueTypeName = TypeUtil.GetName(valueType);
        RawBean parentRawBean = null;
        if (tags.TryGetValue("parent", out string parentType))
        {
            var parentNs = TypeUtil.GetNamespace(parentType);
            string parentNamespace = string.IsNullOrEmpty(parentNs) ? rawTable.Namespace : parentNs;
            string parentName = TypeUtil.GetName(parentType);
            parentType = string.Join(".", parentNamespace, parentName);
            parentRawBean = _beans.FirstOrDefault(x => x.FullName == parentType);
        }
        var cb = new RawBean() { Namespace = valueTypeNamespace, Name = valueTypeName, Comment = "", Parent = parentType };
        if (parentRawBean != null)
        {
            foreach (var parentField in parentRawBean.Fields)
            {
                if (!tableDefInfo.FieldInfos.Any(x => x.Key == parentField.Name && x.Value.Type == parentField.Type))
                {
                    throw new Exception($"table:'{rawTable.Name}' file:{file.OriginFile} title:缺失父类字段：'{parentField.Type} {parentField.Name}'");
                }
            }
        }

        foreach (var (name, f) in tableDefInfo.FieldInfos)
        {
            if (parentRawBean != null && parentRawBean.Fields.Any(x => x.Name == name && x.Type == f.Type))
            {
                continue;
            }
            var cf = new RawField() { Name = name, Id = 0 };

            string[] attrs = f.Type.Trim().Split('&').Select(s => s.Trim()).ToArray();

            if (attrs.Length == 0 || string.IsNullOrWhiteSpace(attrs[0]))
            {
                throw new Exception($"table:'{rawTable.Name}' file:{file.OriginFile} title:'{name}' type missing!");
            }

            cf.Comment = f.Desc;
            cf.Type = attrs[0];
            for (int i = 1; i < attrs.Length; i++)
            {
                var pair = attrs[i].Split('=', 2);
                if (pair.Length != 2)
                {
                    throw new Exception($"table:'{rawTable.Name}' file:{file.OriginFile} title:'{name}' attr:'{attrs[i]}' is invalid!");
                }
                var attrName = pair[0].Trim();
                var attrValue = pair[1].Trim();
                switch (attrName)
                {
                    case "index":
                    case "ref":
                    case "path":
                    case "range":
                    case "sep":
                    case "regex":
                    {
                        throw new Exception($"table:'{rawTable.Name}' file:{file.OriginFile} title:'{name}' attr:'{attrName}' 属于type的属性，必须用#分割，尝试'{cf.Type}#{attrs[i]}'");
                    }
                    case "group":
                    {
                        cf.Groups = attrValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        break;
                    }
                    case "comment":
                    {
                        cf.Comment = attrValue;
                        break;
                    }
                    case "tags":
                    {
                        cf.Tags = attrValue;
                        break;
                    }
                    default:
                    {
                        throw new Exception($"table:'{rawTable.Name}' file:{file.OriginFile} title:'{name}' attr:'{attrs[i]}' is invalid!");
                    }
                }
            }

            if (!string.IsNullOrEmpty(f.Groups))
            {
                cf.Groups = f.Groups.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            cb.Fields.Add(cf);
        }
        return cb;
    }

    private async Task LoadTableValueTypeDefinesFromFileAsync(string dataDir)
    {
        var loadTasks = new List<Task<RawBean>>();
        foreach (var table in this._cfgTables.Where(t => t.LoadDefineFromFile))
        {
            loadTasks.Add(Task.Run(async () => await this.LoadTableValueTypeDefineFromFileAsync(table, dataDir)));
        }

        foreach (var task in loadTasks)
        {
            this._beans.Add(await task);
        }
    }

    private async Task LoadTableListFromFileAsync(string dataDir)
    {
        if (this._importExcelTableFiles.Count == 0)
        {
            return;
        }
        var inputFileInfos = await DataLoaderUtil.CollectInputFilesAsync(this.Agent, this._importExcelTableFiles, dataDir);

        var defTableRecordType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__TableRecord__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            TypeId = 0,
            IsSerializeCompatible = false,
            Fields = new List<RawField>
            {
                new RawField() { Name = "full_name", Type = "string" },
                new RawField() { Name = "value_type", Type = "string" },
                new RawField() { Name = "index", Type = "string" },
                new RawField() { Name = "mode", Type = "string" },
                new RawField() { Name = "group", Type = "string" },
                new RawField() { Name = "comment", Type = "string" },
                new RawField() { Name = "define_from_file", Type = "bool" },
                new RawField() { Name = "input", Type = "string" },
                new RawField() { Name = "output", Type = "string" },
                new RawField() { Name = "patch_input", Type = "string" },
                new RawField() { Name = "tags", Type = "string" },
                new RawField() { Name = "options", Type = "string" },
            }
        })
        {
            Assembly = new DefAssembly("", null, new List<string>(), Agent),
        };
        defTableRecordType.PreCompile();
        defTableRecordType.Compile();
        defTableRecordType.PostCompile();
        var tableRecordType = TBean.Create(false, defTableRecordType, null);

        foreach (var file in inputFileInfos)
        {
            var source = new ExcelRowColumnDataSource();
            var bytes = await this.Agent.GetFromCacheOrReadAllBytesAsync(file.ActualFile, file.MD5);
            (var actualFile, var sheetName) = FileUtil.SplitFileAndSheetName(FileUtil.Standardize(file.OriginFile));
            var records = DataLoaderUtil.LoadCfgRecords(tableRecordType, actualFile, sheetName, bytes, true, null);
            foreach (var r in records)
            {
                DBean data = r.Data;
                //s_logger.Info("== read text:{}", r.Data);
                string fullName = (data.GetField("full_name") as DString).Value.Trim();
                string name = TypeUtil.GetName(fullName);
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(name))
                {
                    throw new Exception($"file:{file.ActualFile} 定义了一个空的table类名");
                }
                string module = TypeUtil.GetNamespace(fullName);
                string valueType = (data.GetField("value_type") as DString).Value.Trim();
                string index = (data.GetField("index") as DString).Value.Trim();
                string mode = (data.GetField("mode") as DString).Value.Trim();
                string group = (data.GetField("group") as DString).Value.Trim();
                string comment = (data.GetField("comment") as DString).Value.Trim();
                bool isDefineFromExcel = (data.GetField("define_from_file") as DBool).Value;
                string inputFile = (data.GetField("input") as DString).Value.Trim();
                string patchInput = (data.GetField("patch_input") as DString).Value.Trim();
                string tags = (data.GetField("tags") as DString).Value.Trim();
                string outputFile = (data.GetField("output") as DString).Value.Trim();
                string options = (data.GetField("options") as DString).Value.Trim();
                AddTable(file.OriginFile, name, module, valueType, index, mode, group, comment, isDefineFromExcel, inputFile, patchInput, tags, outputFile, options);
            };
        }
    }

    private async Task LoadEnumListFromFileAsync(string dataDir)
    {
        if (this._importExcelEnumFiles.Count == 0)
        {
            return;
        }
        var inputFileInfos = await DataLoaderUtil.CollectInputFilesAsync(this.Agent, this._importExcelEnumFiles, dataDir);


        var ass = new DefAssembly("", null, new List<string>(), Agent);

        var enumItemType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__EnumItem__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            TypeId = 0,
            IsSerializeCompatible = false,
            Fields = new List<RawField>
            {
                new RawField() { Name = "name", Type = "string" },
                new RawField() { Name = "alias", Type = "string" },
                new RawField() { Name = "value", Type = "string" },
                new RawField() { Name = "comment", Type = "string" },
                new RawField() { Name = "tags", Type = "string" },
            }
        })
        {
            Assembly = ass,
        };
        ass.AddType(enumItemType);
        enumItemType.PreCompile();
        enumItemType.Compile();
        enumItemType.PostCompile();

        var defTableRecordType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__EnumInfo__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            TypeId = 0,
            IsSerializeCompatible = false,
            Fields = new List<RawField>
            {
                new RawField() { Name = "full_name", Type = "string" },
                new RawField() { Name = "comment", Type = "string" },
                new RawField() { Name = "flags", Type = "bool" },
                new RawField() { Name = "tags", Type = "string" },
                new RawField() { Name = "unique", Type = "bool" },
                new RawField() { Name = "items", Type = "list,__EnumItem__" },
            }
        })
        {
            Assembly = ass,
        };
        ass.AddType(defTableRecordType);
        defTableRecordType.PreCompile();
        defTableRecordType.Compile();
        defTableRecordType.PostCompile();
        var tableRecordType = TBean.Create(false, defTableRecordType, null);

        foreach (var file in inputFileInfos)
        {
            var source = new ExcelRowColumnDataSource();
            var bytes = await this.Agent.GetFromCacheOrReadAllBytesAsync(file.ActualFile, file.MD5);
            (var actualFile, var sheetName) = FileUtil.SplitFileAndSheetName(FileUtil.Standardize(file.OriginFile));
            var records = DataLoaderUtil.LoadCfgRecords(tableRecordType, actualFile, sheetName, bytes, true, null);

            foreach (var r in records)
            {
                DBean data = r.Data;
                //s_logger.Info("== read text:{}", r.Data);
                string fullName = (data.GetField("full_name") as DString).Value.Trim();
                string name = TypeUtil.GetName(fullName);
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(name))
                {
                    throw new Exception($"file:{file.ActualFile} 定义了一个空的enum类名");
                }
                string module = TypeUtil.GetNamespace(fullName);

                DList items = (data.GetField("items") as DList);

                var curEnum = new RawEnum()
                {
                    Name = name,
                    Namespace = module,
                    IsFlags = (data.GetField("flags") as DBool).Value,
                    Tags = (data.GetField("tags") as DString).Value,
                    Comment = (data.GetField("comment") as DString).Value,
                    IsUniqueItemId = (data.GetField("unique") as DBool).Value,
                    Items = items.Datas.Cast<DBean>().Select(d => new EnumItem()
                    {
                        Name = (d.GetField("name") as DString).Value,
                        Alias = (d.GetField("alias") as DString).Value,
                        Value = (d.GetField("value") as DString).Value,
                        Comment = (d.GetField("comment") as DString).Value,
                        Tags = (d.GetField("tags") as DString).Value,
                    }).ToList(),
                };
                this._enums.Add(curEnum);
            };
        }
    }

    private async Task LoadBeanListFromFileAsync(string dataDir)
    {
        if (this._importExcelBeanFiles.Count == 0)
        {
            return;
        }
        var inputFileInfos = await DataLoaderUtil.CollectInputFilesAsync(this.Agent, this._importExcelBeanFiles, dataDir);


        var ass = new DefAssembly("", null, new List<string>(), Agent);

        var defBeanFieldType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__FieldInfo__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            TypeId = 0,
            IsSerializeCompatible = false,
            Fields = new List<RawField>
            {
                new RawField() { Name = "name", Type = "string" },
                new RawField() { Name = "type", Type = "string" },
                new RawField() { Name = "group", Type = "string" },
                new RawField() { Name = "comment", Type = "string" },
                new RawField() { Name = "tags", Type = "string" },
            }
        })
        {
            Assembly = ass,
        };

        defBeanFieldType.PreCompile();
        defBeanFieldType.Compile();
        defBeanFieldType.PostCompile();

        ass.AddType(defBeanFieldType);

        var defTableRecordType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__BeanInfo__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            TypeId = 0,
            IsSerializeCompatible = false,
            Fields = new List<RawField>
            {
                new RawField() { Name = "full_name", Type = "string" },
                new RawField() {Name =  "parent", Type = "string" },
                new RawField() { Name = "sep", Type = "string" },
                new RawField() { Name = "alias", Type = "string" },
                new RawField() { Name = "comment", Type = "string" },
                new RawField() { Name = "tags", Type = "string" },
                new RawField() { Name = "fields", Type = "list,__FieldInfo__" },
            }
        })
        {
            Assembly = ass,
        };
        ass.AddType(defTableRecordType);
        defTableRecordType.PreCompile();
        defTableRecordType.Compile();
        defTableRecordType.PostCompile();
        var tableRecordType = TBean.Create(false, defTableRecordType, null);

        foreach (var file in inputFileInfos)
        {
            var source = new ExcelRowColumnDataSource();
            var bytes = await this.Agent.GetFromCacheOrReadAllBytesAsync(file.ActualFile, file.MD5);
            var records = DataLoaderUtil.LoadCfgRecords(tableRecordType, file.OriginFile, file.SheetName, bytes, true, null);

            foreach (var r in records)
            {
                DBean data = r.Data;
                //s_logger.Info("== read text:{}", r.Data);
                string fullName = (data.GetField("full_name") as DString).Value.Trim();
                string name = TypeUtil.GetName(fullName);
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(name))
                {
                    throw new Exception($"file:'{file.ActualFile}' 定义了一个空bean类名");
                }
                string module = TypeUtil.GetNamespace(fullName);

                string parent = (data.GetField("parent") as DString).Value.Trim();
                string sep = (data.GetField("sep") as DString).Value.Trim();
                string alias = (data.GetField("alias") as DString).Value.Trim();
                string comment = (data.GetField("comment") as DString).Value.Trim();
                string tags = (data.GetField("tags") as DString).Value.Trim();
                DList fields = data.GetField("fields") as DList;
                var curBean = new RawBean()
                {
                    Name = name,
                    Namespace = module,
                    Sep = sep,
                    Alias = alias,
                    Comment = comment,
                    Tags = tags,
                    Parent = parent,
                    Fields = fields.Datas.Select(d => (DBean)d).Select(b => this.CreateField(
                        file.ActualFile,
                        (b.GetField("name") as DString).Value.Trim(),
                        (b.GetField("type") as DString).Value.Trim(),
                        (b.GetField("group") as DString).Value,
                        (b.GetField("comment") as DString).Value.Trim(),
                        (b.GetField("tags") as DString).Value.Trim(),
                        false
                    )).ToList(),
                };
                this._beans.Add(curBean);
            };
        }
    }

    public async Task LoadDefinesFromFileAsync(string dataDir)
    {
        await Task.WhenAll(LoadTableListFromFileAsync(dataDir), LoadEnumListFromFileAsync(dataDir), LoadBeanListFromFileAsync(dataDir));
        await LoadTableValueTypeDefinesFromFileAsync(dataDir);
    }

    private static readonly List<string> _fieldOptionalAttrs = new()
    {
        "ref",
        "path",
        "group",
        "comment",
        "tags",
    };

    private static readonly List<string> _fieldRequireAttrs = new List<string> { "name", "type" };

    protected RawField CreateField(string defineFile, XElement e)
    {
        ValidAttrKeys(defineFile, e, _fieldOptionalAttrs, _fieldRequireAttrs);

        string typeStr = XmlUtil.GetRequiredAttribute(e, "type");

        string refStr = XmlUtil.GetOptionalAttribute(e, "ref");
        if (!string.IsNullOrWhiteSpace(refStr))
        {
            typeStr = typeStr + "#(ref=" + refStr + ")";
        }
        string pathStr = XmlUtil.GetOptionalAttribute(e, "path");
        if (!string.IsNullOrWhiteSpace(pathStr))
        {
            typeStr = typeStr + "#(path=" + pathStr + ")";
        }

        return CreateField(defineFile, XmlUtil.GetRequiredAttribute(e, "name"),
            typeStr,
            XmlUtil.GetOptionalAttribute(e, "group"),
            XmlUtil.GetOptionalAttribute(e, "comment"),
            XmlUtil.GetOptionalAttribute(e, "tags"),
            false
        );
    }

    private RawField CreateField(string defineFile, string name, string type, string group,
        string comment, string tags,
        bool ignoreNameValidation)
    {
        var f = new RawField()
        {
            Name = name,
            Groups = CreateGroups(group),
            Comment = comment,
            Tags = tags,
            IgnoreNameValidation = ignoreNameValidation,
        };

        // 字段与table的默认组不一样。
        // table 默认只属于default=1的组
        // 字段默认属于所有组
        if (!ValidGroup(f.Groups, out var invalidGroup))
        {
            throw new Exception($"定义文件:{defineFile} field:'{name}' group:'{invalidGroup}' 不存在");
        }
        f.Type = type;


        //FillValueValidator(f, refs, "ref");
        //FillValueValidator(f, path, "path"); // (ue4|unity|normal|regex);xxx;xxx
        //FillValueValidator(f, range, "range");

        //FillValidators(defileFile, "key_validator", keyValidator, f.KeyValidators);
        //FillValidators(defileFile, "value_validator", valueValidator, f.ValueValidators);
        //FillValidators(defileFile, "validator", validator, f.Validators);
        return f;
    }

    private static readonly List<string> _beanOptinsAttrs = new List<string> { "parent", "value_type", "alias", "sep", "comment", "tags", "externaltype" };
    private static readonly List<string> _beanRequireAttrs = new List<string> { "name" };

    protected void AddBean(string defineFile, XElement e, string parent)
    {
        ValidAttrKeys(defineFile, e, _beanOptinsAttrs, _beanRequireAttrs);
        TryGetUpdateParent(e, ref parent);
        var b = new RawBean()
        {
            Name = XmlUtil.GetRequiredAttribute(e, "name"),
            Namespace = CurNamespace,
            Parent = parent,
            TypeId = 0,
            IsSerializeCompatible = true,
            IsValueType = XmlUtil.GetOptionBoolAttribute(e, "value_type"),
            Alias = XmlUtil.GetOptionalAttribute(e, "alias"),
            Sep = XmlUtil.GetOptionalAttribute(e, "sep"),
            Comment = XmlUtil.GetOptionalAttribute(e, "comment"),
            Tags = XmlUtil.GetOptionalAttribute(e, "tags"),
        };
        var childBeans = new List<XElement>();

        bool defineAnyChildBean = false;
        foreach (XElement fe in e.Elements())
        {
            switch (fe.Name.LocalName)
            {
                case "var":
                {
                    if (defineAnyChildBean)
                    {
                        throw new LoadDefException($"定义文件:{defineFile} 类型:{b.FullName} 的多态子bean必须在所有成员字段 <var> 之后定义");
                    }
                    b.Fields.Add(CreateField(defineFile, fe)); ;
                    break;
                }
                case "bean":
                {
                    defineAnyChildBean = true;
                    childBeans.Add(fe);
                    break;
                }
                default:
                {
                    throw new LoadDefException($"定义文件:{defineFile} 类型:{b.FullName} 不支持 tag:{fe.Name}");
                }
            }
        }
        s_logger.Trace("add bean:{@bean}", b);
        _beans.Add(b);

        var fullname = b.FullName;
        foreach (var cb in childBeans)
        {
            AddBean(defineFile, cb, fullname);
        }
    }


    private static readonly List<string> _refGroupRequireAttrs = new List<string> { "name", "ref" };

    private void AddRefGroup(string defineFile, XElement e)
    {
        ValidAttrKeys(defineFile, e, null, _refGroupRequireAttrs);

        var refGroup = new RawRefGroup()
        {
            Name = XmlUtil.GetRequiredAttribute(e, "name"),
            Refs = XmlUtil.GetRequiredAttribute(e, "ref").Split(',').Select(s => s.Trim()).ToList(),
        };
        _refGroups.Add(refGroup);
    }
}