using Luban.Core.RawDefs;

namespace Luban.Plugin.Loader;

public abstract class SchemaCollectorBase : ISchemaCollector
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly List<RawEnum> _enums = new List<RawEnum>();
    private readonly List<RawBean> _beans = new List<RawBean>();
    private readonly HashSet<string> _externalSelectors = new();
    private readonly List<RawExternalType> _externalTypes = new();

    private readonly Dictionary<string, string> _envs = new();
    
    private readonly List<RawPatch> _patches = new();

    private readonly List<RawTable> _tables = new List<RawTable>();

    private readonly List<RawTarget> _targets = new List<RawTarget>();

    private readonly List<RawGroup> _groups = new List<RawGroup>();

    private readonly List<RawRefGroup> _refGroups = new();
    
    
    public RawAssembly CreateRawAssembly()
    {
        return new RawAssembly()
        {
            Patches = _patches,
            Tables = _tables,
            Targets = _targets,
            Groups = _groups,
            RefGroups = _refGroups,
            Enums = _enums,
            Beans = _beans,
            ExternalTypes = _externalTypes,
            ExternalSelectors = _externalSelectors,
            Envs = _envs,
        };
    }
    
    public void AddTable(RawTable table)
    {
        _tables.Add(table);
    }

    public void AddBean(RawBean bean)
    {
        _beans.Add(bean);
    }

    public void AddEnum(RawEnum @enum)
    {
        _enums.Add(@enum);
    }

    public void AddGroup(RawGroup group)
    {
        _groups.Add(group);
    }

    public void AddRefGroup(RawRefGroup refGroup)
    {
        _refGroups.Add(refGroup);
    }

    public void AddPatch(RawPatch patch)
    {
        _patches.Add(patch);
    }

    public void AddTarget(RawTarget target)
    {
        _targets.Add(target);
    }

    public void AddExternalTypeSelector(string selector)
    {
        _externalSelectors.Add(selector);
    }

    public void AddExternalType(RawExternalType externalType)
    {
        _externalTypes.Add(externalType);
    }

    public void AddEnv(string key, string value)
    {
        if (!_envs.TryAdd(key, value))
        {
            s_logger.Warn("env key:{} already exist, value:{} will be ignored", key, value);
        }
    }
}