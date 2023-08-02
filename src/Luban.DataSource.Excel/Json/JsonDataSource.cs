using Luban.Datas;
using Luban.Defs;
using Luban.Job.Common.Types;
using System.Text.Json;

namespace Luban.DataSources.Json;

class JsonDataSource : AbstractDataSource
{
    private JsonElement _data;

    public override void Load(string rawUrl, string sheetOrFieldName, Stream stream)
    {
        RawUrl = rawUrl;
        this._data = JsonDocument.Parse(stream).RootElement;

        if (!string.IsNullOrEmpty(sheetOrFieldName))
        {
            if (sheetOrFieldName.StartsWith("*"))
            {
                sheetOrFieldName = sheetOrFieldName.Substring(1);
            }
            if (!string.IsNullOrEmpty(sheetOrFieldName))
            {
                foreach (var subField in sheetOrFieldName.Split('.'))
                {
                    _data = _data.GetProperty(subField);
                }
            }
        }
    }

    public override List<Record> ReadMulti(TBean type)
    {
        var records = new List<Record>();
        foreach (var ele in _data.EnumerateArray())
        {
            Record rec = ReadRecord(ele, type);
            if (rec != null)
            {
                records.Add(rec);
            }
        }
        return records;
    }

    private Record ReadRecord(JsonElement ele, TBean type)
    {
        List<string> tags;
        if (ele.TryGetProperty(TAG_KEY, out var tagEle))
        {
            var tagName = tagEle.GetString();
            if (DataUtil.IsIgnoreTag(tagName))
            {
                return null;
            }
            tags = DataUtil.ParseTags(tagName);
        }
        else
        {
            tags = null;
        }

        var data = (DBean)type.Apply(JsonDataCreator.Ins, ele, (DefAssembly)type.Bean.Assembly);
        return new Record(data, RawUrl, tags);
    }

    public override Record ReadOne(TBean type)
    {
        return ReadRecord(_data, type);
    }
}