using System.Xml.Linq;
using System.Xml.Schema;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Utils;
using Luban.Plugin.Schema;
using Luban.Plugin.SchemaCollector;

namespace Luban.Plugin.Loader;

public class RootXmlSchemaLoader : ISchemaLoader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, Action<XElement>> _tagHandlers = new();
    
    private readonly List<SchemaFileInfo> _importFiles = new();
    
    public IReadOnlyList<SchemaFileInfo> ImportFiles => _importFiles;

    private string _xmlFileName;
    private ISchemaCollector _schemaCollector;

    public RootXmlSchemaLoader()
    {
        _tagHandlers.Add("env", AddEnv);
        _tagHandlers.Add("externalselector", AddExternalSelector);
        _tagHandlers.Add("import", AddImport);
        _tagHandlers.Add("patch", AddPatch);
        _tagHandlers.Add("service", AddTarget);
        _tagHandlers.Add("group", AddGroup);
        _tagHandlers.Add("refgroup", AddRefGroup);
    }

    public void Load(string fileName, ISchemaCollector collector)
    {
        s_logger.Info("load root xml schema file:{}", fileName);
        _xmlFileName = fileName;
        XElement doc = XmlUtil.Open(fileName);

        foreach (XElement e in doc.Elements())
        {
            var tagName = e.Name.LocalName;
            if (_tagHandlers.TryGetValue(tagName, out var handler))
            {
                handler(e);
            }
            else
            {
                throw new LoadDefException($"定义文件:{fileName} 非法 tag:{tagName}");
            }
        }
    }

    private static readonly List<string> _envRequireAttrs = new List<string> { "name", "value", };

    private void AddEnv(XElement e)
    {
        XmlSchemaUtil.ValidAttrKeys(_xmlFileName, e, null, _envRequireAttrs);
        string name = XmlUtil.GetRequiredAttribute(e, "name");
        _schemaCollector.AddEnv(name, XmlUtil.GetRequiredAttribute(e, "value"));
    }
    
     private static readonly List<string> _excelImportRequireAttrs = new List<string> { "name", "type" };
     
    private void AddImport(XElement e)
    {
        XmlSchemaUtil.ValidAttrKeys(_xmlFileName, e, null, _excelImportRequireAttrs);
        var importName = XmlUtil.GetRequiredAttribute(e, "name");
        if (string.IsNullOrWhiteSpace(importName))
        {
            throw new Exception("import 属性name不能为空");
        }
        var type = XmlUtil.GetRequiredAttribute(e, "type");
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new Exception($"import  name:'{importName}' type属性不能为空");
        }
        _importFiles.Add(new SchemaFileInfo(){ FileName = importName, Type = type});
    }

    private static readonly List<string> _patchRequireAttrs = new List<string> { "name" };
    private void AddPatch(XElement e)
    {
        XmlSchemaUtil.ValidAttrKeys(_xmlFileName, e, null, _patchRequireAttrs);
        var patchName = XmlUtil.GetRequiredAttribute(e, "name");
        if (string.IsNullOrWhiteSpace(patchName))
        {
            throw new Exception("patch 属性name不能为空");
        }
        _schemaCollector.Add(new RawPatch(patchName));
    }

    private static readonly List<string> _groupOptionalAttrs = new List<string> { "default" };
    private static readonly List<string> _groupRequireAttrs = new List<string> { "name" };

    private void AddGroup(XElement e)
    {
        XmlSchemaUtil.ValidAttrKeys(_xmlFileName, e, _groupOptionalAttrs, _groupRequireAttrs);
        List<string> groupNames = XmlSchemaUtil.CreateGroups(XmlUtil.GetRequiredAttribute(e, "name"));
        bool isDefault = XmlUtil.GetOptionBoolAttribute(e, "default");
        _schemaCollector.Add(new RawGroup(){ Names = groupNames, IsDefault = isDefault});
    }

    private readonly List<string> _serviceAttrs = new List<string> { "name", "manager", "group" };

    private void AddTarget(XElement e)
    {
        var name = XmlUtil.GetRequiredAttribute(e, "name");
        var manager = XmlUtil.GetRequiredAttribute(e, "manager");
        List<string> groups = XmlSchemaUtil.CreateGroups(XmlUtil.GetOptionalAttribute(e, "group"));
        XmlSchemaUtil.ValidAttrKeys(_xmlFileName, e, _serviceAttrs, _serviceAttrs);
        _schemaCollector.Add(new RawTarget() { Name = name, Manager = manager, Groups = groups });
    }

    private void AddRefGroup(XElement e)
    {
        _schemaCollector.Add(XmlSchemaUtil.CreateRefGroup(_xmlFileName, e));
    }
    
    private static readonly List<string> _selectorRequiredAttrs = new List<string> { "name" };
    private void AddExternalSelector(XElement e)
    {
        // ValidAttrKeys(_rootXml, e, null, _selectorRequiredAttrs);
        // string name = XmlUtil.GetRequiredAttribute(e, "name");
        // if (!_externalSelectors.Add(name))
        // {
        //     throw new LoadDefException($"定义文件:{_rootXml} external selector name:{name} 重复");
        // }
        // s_logger.Trace("add selector:{}", name);
    }
}