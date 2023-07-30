namespace Luban.Plugin.Loader;

public class SchemaLoaderFactory
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    public static SchemaLoaderFactory Ins { get; } = new SchemaLoaderFactory();
    
    private readonly Dictionary<(string, string), Func<string, ISchemaLoader>> _schemaLoaders = new();

    public ISchemaLoader Create(string name, string type)
    {
        if (_schemaLoaders.TryGetValue((name, type), out var creator))
        {
            return creator(type);
        }
        else
        {
            throw new Exception($"unknown schema loader name:{name} type:{type}");
        }
    }
    
    public void RegisterSchemaLoaderCreator(string name, string type, Func<string, ISchemaLoader> creator)
    {
        if (_schemaLoaders.ContainsKey((name, type)))
        {
            _schemaLoaders.Add((name, type), creator);
            s_logger.Info("override schema loader creator. name:{} type:{}", name, type);
        }
        else
        {
            _schemaLoaders[(name, type)] = creator;
            s_logger.Info("add schema loader creator. name:{} type:{}", name, type);
        }
    }
}