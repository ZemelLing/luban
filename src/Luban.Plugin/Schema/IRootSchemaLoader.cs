namespace Luban.Plugin.Schema;

public interface IRootSchemaLoader : ISchemaLoader
{
    public IReadOnlyList<SchemaFileInfo> ImportFiles { get; }
}