using Luban.Core.RawDefs;

namespace Luban.Plugin.Loader;

public interface ISchemaCollector
{
    void AddTable(RawTable table);

    void AddBean(RawBean bean);

    void AddEnum(RawEnum @enum);
    
    void AddGroup(RawGroup group);
    
    void AddRefGroup(RawRefGroup refGroup);
    
    void AddPatch(RawPatch patch);
    
    void AddTarget(RawTarget target);

    void AddExternalTypeSelector(string selector);
    
    void AddExternalType(RawExternalType externalType);

    void AddEnv(string key, string value);
}