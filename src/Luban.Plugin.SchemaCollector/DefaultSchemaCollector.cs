using Luban.Plugin.Loader;
using Luban.Plugin.Schema;

namespace Luban.Plugin.SchemaCollector;

public class DefaultSchemaCollector : SchemaCollectorBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public DefaultSchemaCollector()
    {
        
    }

    public void Load(string rootXml)
    {
        var rootLoader = new RootXmlSchemaLoader();
        rootLoader.Load(rootXml, this);

        foreach (var importFile in rootLoader.ImportFiles)
        {
            s_logger.Trace("import schema file:{} type:{}", importFile.FileName, importFile.Type);
            var schemaLoader = SchemaLoaderFactory.Ins.Create(Path.GetExtension(importFile.FileName), importFile.Type);
            schemaLoader.Load(importFile.FileName, this);
        }
    }
}