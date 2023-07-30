namespace Luban.Core.RawDefs;

public class RawDefines
{
    public string TopModule { get; set; } = "";

    public Dictionary<string, string> Options { get; set; } = new();

    public HashSet<string> ExternalSelectors { get; set; } = new();

    public Dictionary<string, RawExternalType> ExternalTypes { get; set; } = new();

    public List<RawBean> Beans { get; set; } = new();

    public List<RawEnum> Enums { get; set; } = new();
    
    public List<RawPatch> Patches { get; set; } = new();

    public List<RawTable> Tables { get; set; } = new();

    public List<RawGroup> Groups { get; set; } = new();

    public List<RawTarget> Services { get; set; } = new();

    public List<RawRefGroup> RefGroups { get; set; } = new();
}