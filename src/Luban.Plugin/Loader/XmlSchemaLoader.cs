using System.Xml.Linq;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Utils;

namespace Luban.Plugin.Loader;

public class XmlSchemaLoader : ISchemaLoader
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    public void Load(string fileName, ISchemaCollector collector)
    {
        XElement doc = XmlUtil.Open(fileName);

        foreach (XElement e in doc.Elements())
        {
            var tagName = e.Name.LocalName;

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
            throw new LoadDefException($"定义文件:{_rootXml} external selector name:{name} 重复");
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
                string uniqKey = $"{mapper.Language}##{mapper.Selector}";
                if (mappers.ContainsKey(uniqKey))
                {
                    throw new LoadDefException($"定义文件:{_rootXml} externaltype name:{name} mapper(lan='{mapper.Language}',selector='{mapper.Selector}') 重复");
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
            Language = XmlUtil.GetRequiredAttribute(e, "lan"),
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
            throw new LoadDefException($"定义文件:{defineFile} externaltype:{externalType} lan:{m.Language} selector:{m.Selector} 没有定义 'target_type_name'");
        }
        return m;
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
}