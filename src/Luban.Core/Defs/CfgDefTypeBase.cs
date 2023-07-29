using Luban.Job.Common.Defs;

namespace Luban.Defs;

public abstract class CfgDefTypeBase : DefTypeBase
{
    public DefAssembly Assembly => (DefAssembly)AssemblyBase;
}