using Luban.Core.Utils;

namespace Luban.Core.Defs;

public abstract class DefTypeBase
{
    public DefAssembly Assembly { get; set; }

    public string TopModule => GenerationContext.Ins.TopModule;

    public string Name { get; set; }

    public string Namespace { get; set; }

    public string FullName => TypeUtil.MakeFullName(Namespace, Name);

    public string NamespaceWithTopModule => TypeUtil.MakeNamespace(TopModule, Namespace);

    public string FullNameWithTopModule => TypeUtil.MakeFullName(TopModule, FullName);

    public string CsFullName => TypeUtil.MakeFullName(Namespace, Name);

    public string JavaFullName => TypeUtil.MakeFullName(Namespace, Name);

    public string GoFullName => TypeUtil.MakeGoFullName(Namespace, Name);

    public string GoPkgName => TypeUtil.MakeGoPkgName(Namespace);

    public string CppNamespaceBegin => TypeUtil.MakeCppNamespaceBegin(Namespace);

    public string CppNamespaceEnd => TypeUtil.MakeCppNamespaceEnd(Namespace);

    public string CppFullNameWithTopModule => TypeUtil.MakeCppFullName(Assembly.TopModule, FullName);

    public string TypescriptNamespaceBegin => TypeUtil.MakeTypescriptNamespaceBegin(Namespace);

    public string TypescriptNamespaceEnd => TypeUtil.MakeTypescriptNamespaceEnd(Namespace);

    public string CppFullName => TypeUtil.MakeCppFullName(Namespace, Name);

    public string PyFullName => TypeUtil.MakePyFullName(Namespace, Name);

    public string GDScriptFullName => TypeUtil.MakeGDScriptFullName(Namespace, Name);

    public string RustFullName => TypeUtil.MakeRustFullName(Namespace, Name);

    public string PbFullName => TypeUtil.MakePbFullName(Namespace, Name);

    public string FlatBuffersFullName => TypeUtil.MakeFlatBuffersFullName(Namespace, Name);

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