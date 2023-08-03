using Luban.Core.Schema;
using Luban.Core.Utils;

namespace Luban.Schema.Default;

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
            s_logger.Debug("import schema file:{} type:{}", importFile.FileName, importFile.Type);
            var schemaLoader = SchemaLoaderFactory.Ins.Create(FileUtil.GetExtensionWithDot(importFile.FileName), importFile.Type);
            schemaLoader.Load(importFile.FileName, this);
        }
    }
}