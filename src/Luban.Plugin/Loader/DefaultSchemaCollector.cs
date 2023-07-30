using System.Xml.Linq;
using Luban.Core.Datas;
using Luban.Core.Defs;
using Luban.Core.RawDefs;
using Luban.Core.Types;
using Luban.Core.Utils;

namespace Luban.Plugin.Loader;

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