namespace Luban.Plugin.Schema;

public interface ISchemaLoader
{
    void Load(string fileName, ISchemaCollector collector);
}