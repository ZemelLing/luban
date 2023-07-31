using Luban.Core.Utils;
using Luban.Plugin.Schema;

namespace Luban.Plugin.SchemaCollector;

[SchemaCollector("default")]
public class DefaultSchemaCollector : SchemaCollectorBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public DefaultSchemaCollector()
    {
        
    }

    public override void Load(string rootXml)
    {
        var rootLoader = (IRootSchemaLoader)SchemaLoaderFactory.Ins.Create(FileUtil.GetExtensionWithDot(rootXml), "root");
        rootLoader.Load(rootXml, this);

        foreach (var importFile in rootLoader.ImportFiles)
        {
            s_logger.Info("import schema file:{} type:{}", importFile.FileName, importFile.Type);
            var schemaLoader = SchemaLoaderFactory.Ins.Create(FileUtil.GetExtensionWithDot(importFile.FileName), importFile.Type);
            schemaLoader.Load(importFile.FileName, this);
        }
    }
}