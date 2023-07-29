using Luban.Datas;

namespace Luban.DataSources;

public class Record
{
    public DBean Data { get; }

    public string Source { get; }

    public List<string> Tags { get; }

    public bool IsNotFiltered(List<string> excludeTags)
    {
        if (Tags == null)
        {
            return true;
        }
        return Tags.TrueForAll(t => !excludeTags.Contains(t));
    }

    public Record(DBean data, string source, List<string> tags)
    {
        Data = data;
        Source = source;
        Tags = tags;
    }
}