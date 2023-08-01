using Luban.Plugin;
using NLog;

namespace Luban.Plugin.SchemaCollector;

public class PluginEntry : PluginBase
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
    
    public override string Name => "SchemaCollector";
    
    public override void Init(string jsonStr)
    {
        s_logger.Info($"plugin [{Name}] inits success");
    }

    public override void Start()
    {

    }
}