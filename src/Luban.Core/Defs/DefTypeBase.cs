using Luban.Core.Utils;

namespace Luban.Core.Defs;

public abstract class DefTypeBase
{
    public DefAssembly Assembly { get; set; }

    public string TopModule => GenerationContext.Ins.TopModule;

    public string Name { get; set; }

    public string Namespace { get; set; }

    public string FullName => TypeUtil.MakeFullName(Namespace, Name);
    
    public List<string> Groups { get; set; }

    public string Comment { get; protected set; }

    public Dictionary<string, string> Tags { get; protected set; }

    public bool HasTag(string attrName)
    {
        return Tags != null && Tags.ContainsKey(attrName);
    }

    public string GetTag(string attrName)
    {
        return Tags != null && Tags.TryGetValue(attrName, out var value) ? value : null;
    }

    public virtual void PreCompile() { }

    public abstract void Compile();

    public virtual void PostCompile() { }
}