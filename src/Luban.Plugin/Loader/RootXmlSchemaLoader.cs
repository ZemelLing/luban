namespace Luban.Plugin.Loader;

public class RootXmlSchemaLoader : ISchemaLoader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    private readonly List<SchemaFileInfo> _importFiles = new();
    
    public IReadOnlyList<SchemaFileInfo> ImportFiles => _importFiles;

    public RootXmlSchemaLoader()
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
    }

    public void LoadAsync(string rootXml)
    {
        _rootXml = rootXml;

        _rootDir = FileUtil.GetParent(rootXml);

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
    
    public void Load(string fileName, ISchemaCollector collector)
    {
        throw new NotImplementedException();
    }
    
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
            default: throw new Exception($"import excel name:'{importName}' type:'{type}' 不合法. 有效值为 table|enum|bean");
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
    
    

    protected void RegisterRootDefineHandler(string name, Action<XElement> handler)
    {
        _rootDefineHandlers.Add(name, handler);
    }

    protected void RegisterModuleDefineHandler(string name, Action<string, XElement> handler)
    {
        _moduleDefineHandlers.Add(name, handler);
    }
}