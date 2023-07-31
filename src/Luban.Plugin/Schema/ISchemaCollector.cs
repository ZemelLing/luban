using Luban.Core.RawDefs;

namespace Luban.Plugin.Schema;

public interface ISchemaCollector
{
    void Add(RawTable table);

    void Add(RawBean bean);

    void Add(RawEnum @enum);
    
    void Add(RawGroup group);
    
    void Add(RawRefGroup refGroup);
    
    void Add(RawPatch patch);
    
    void Add(RawTarget target);

    void AddSelector(string selector);
    
    void Add(RawExternalType externalType);

    void AddEnv(string key, string value);
}