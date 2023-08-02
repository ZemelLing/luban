using Luban.Datas;
using Luban.Defs;
using Luban.Job.Common.Types;
using System.Xml.Linq;

namespace Luban.DataSources.Xml;

class XmlDataSource : AbstractDataSource
{
    private XElement _doc;

    public override void Load(string rawUrl, string sheetName, Stream stream)
    {
        RawUrl = rawUrl;
        _doc = XElement.Load(stream);
    }

    public override List<Record> ReadMulti(TBean type)
    {
        throw new NotSupportedException();
    }

    public override Record ReadOne(TBean type)
    {
        string tagName = _doc.Element(TAG_KEY)?.Value;
        if (DataUtil.IsIgnoreTag(tagName))
        {
            return null;
        }
        var data = (DBean)type.Apply(XmlDataCreator.Ins, _doc, (DefAssembly)type.Bean.Assembly);
        var tags = DataUtil.ParseTags(tagName);
        return new Record(data, RawUrl, tags);
    }
}