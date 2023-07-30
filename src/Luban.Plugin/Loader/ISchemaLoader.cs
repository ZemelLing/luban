namespace Luban.Plugin.Loader;

public interface ISchemaLoader
{
    void Load(string fileName, ISchemaCollector collector);
}