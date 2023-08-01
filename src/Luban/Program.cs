using CommandLine;
using Luban.Core.CodeFormat;
using Luban.Core.CodeGeneration;
using Luban.Core.RawDefs;
using Luban.Core.Schema;
using Luban.Core.Tmpl;
using Luban.Plugin;
using Luban.Utils;
using NLog;
using System.Reflection;
using System.Text;

namespace Luban;

internal class Program
{
    
    private static ILogger s_logger;

    static void Main(string[] args)
    {
        ConsoleUtil.EnableQuickEditMode(false);
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        InitSimpleNLogConfigure(LogLevel.Info);
        s_logger = LogManager.GetCurrentClassLogger();
        s_logger.Info("init logger success");

        string appLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        TemplateManager templateManager = TemplateManager.Ins;
        templateManager.Init();
        templateManager.AddTemplateSearchPath($"{appLocation}/Templates", true);
        
        CodeFormatManager.Ins.Init();
        
        PluginManager.Ins.Init(new DefaultPluginCollector($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Plugins"));

        foreach (var plugin in PluginManager.Ins.Plugins)
        {
            Assembly pluginAssembly = plugin.GetType().Assembly;
            SchemaCollectorFactory.Ins.ScanRegisterCollectorCreator(pluginAssembly);
            SchemaLoaderFactory.Ins.ScanRegisterSchemaLoaderCreator(pluginAssembly);
            CodeFormatManager.Ins.ScanRegisterFormatters(pluginAssembly);
            CodeFormatManager.Ins.ScanRegisterCodeStyle(pluginAssembly);
            templateManager.AddTemplateSearchPath($"{plugin.Location}/Templates", false);
        }

        int processorCount = Environment.ProcessorCount;
        ThreadPool.SetMinThreads(Math.Max(4, processorCount), 5);
        ThreadPool.SetMaxThreads(Math.Max(16, processorCount * 4), 10);
        
        s_logger.Info("start");

        string schemaRootFile = @"D:\workspace2\luban_examples\DesignerConfigs\Defines\__root__.xml";
        string schemaCollectorName = "default";
        var schemaCollector = SchemaCollectorFactory.Ins.CreateSchemaCollector(schemaCollectorName);
        schemaCollector.Load(schemaRootFile);
        RawAssembly ass = schemaCollector.CreateRawAssembly();
        s_logger.Info("table count:{}", ass.Tables.Count);
        
        s_logger.Info("bye~");
    }

    
    private static void InitSimpleNLogConfigure(NLog.LogLevel minConsoleLogLevel)
    {
        var logConfig = new NLog.Config.LoggingConfiguration();
        NLog.Layouts.Layout layout;
        if (minConsoleLogLevel <= NLog.LogLevel.Debug)
        {
            layout = NLog.Layouts.Layout.FromString("${longdate}|${level:uppercase=true}|${callsite}:${callsite-linenumber}|${message}${onexception:${newline}${exception:format=tostring}${exception:format=StackTrace}}");
        }
        else
        {
            layout = NLog.Layouts.Layout.FromString("${longdate}|${message}${onexception:${newline}${exception:format=tostring}${exception:format=StackTrace}}");
        }
        logConfig.AddTarget("console", new NLog.Targets.ColoredConsoleTarget() { Layout = layout });
        logConfig.AddRule(minConsoleLogLevel, NLog.LogLevel.Fatal, "console");
        NLog.LogManager.Configuration = logConfig;
    }
}