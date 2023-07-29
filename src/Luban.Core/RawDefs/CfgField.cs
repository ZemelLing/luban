using Luban.Job.Common.RawDefs;

namespace Luban.RawDefs;

public class CfgField : Field
{
    public List<string> Groups { get; set; } = new List<string>();
}