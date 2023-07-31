using System.Xml.Linq;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Utils;

namespace Luban.Plugin.SchemaCollector;

public static class XmlSchemaUtil
{
    public static void ValidAttrKeys(string defineFile, XElement e, List<string> optionKeys, List<string> requireKeys)
    {
        foreach (var k in e.Attributes())
        {
            var name = k.Name.LocalName;
            if (!requireKeys.Contains(name) && (optionKeys != null && !optionKeys.Contains(name)))
            {
                throw new LoadDefException($"定义文件:{defineFile} 定义:{e} 包含未知属性 attr:{name}");
            }
        }
        foreach (var k in requireKeys)
        {
            if (e.Attribute(k) == null)
            {
                throw new LoadDefException($"定义文件:{defineFile} 定义:{e} 缺失属性 attr:{k}");
            }
        }
    }

    private static readonly List<string> _refGroupRequireAttrs = new List<string> { "name", "ref" };

    public static RawRefGroup CreateRefGroup(string fileName, XElement e)
    {
        ValidAttrKeys(fileName, e, null, _refGroupRequireAttrs);

        return new RawRefGroup()
        {
            Name = XmlUtil.GetRequiredAttribute(e, "name"),
            Refs = XmlUtil.GetRequiredAttribute(e, "ref").Split(',').Select(s => s.Trim()).ToList(),
        };
    }
    
    
    public static List<string> CreateGroups(string s)
    {
        return s.Split(',', ';').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}