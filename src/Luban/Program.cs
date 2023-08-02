using CommandLine;
using Luban.CodeGeneration.CSharp.CodeTargets;
using Luban.Core;
using Luban.Core.CodeFormat;
using Luban.Core.CodeGeneration;
using Luban.Core.Defs;
using Luban.Core.OutputSaver;
using Luban.Core.PostProcess;
using Luban.Core.RawDefs;
using Luban.Core.Schema;
using Luban.Core.Tmpl;
using Luban.Plugin;
using Luban.Schema.Default;
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

        string curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        TemplateManager templateManager = TemplateManager.Ins;
        templateManager.Init();
        templateManager.AddTemplateSearchPath($"{curDir}/Templates", true);
        
        CodeFormatManager.Ins.Init();
        CodeTargetManager.Ins.Init();
        PostProcessManager.Ins.Init();
        OutputSaverManager.Ins.Init();
        
        PluginManager.Ins.Init(new DefaultPluginCollector($"{curDir}/Plugins"));
        
        var scanAssemblies = PluginManager.Ins.Plugins.Select(p => p.GetType().Assembly).ToList();
        scanAssemblies.Add(typeof(CsharpBin).Assembly);
        scanAssemblies.Add(typeof(DefaultSchemaCollector).Assembly);
        scanAssemblies.Add(typeof(GenerationContext).Assembly);

        foreach (var assembly in scanAssemblies)
        {
            SchemaCollectorFactory.Ins.ScanRegisterCollectorCreator(assembly);
            SchemaLoaderFactory.Ins.ScanRegisterSchemaLoaderCreator(assembly);
            CodeFormatManager.Ins.ScanRegisterFormatters(assembly);
            CodeFormatManager.Ins.ScanRegisterCodeStyle(assembly);
            CodeTargetManager.Ins.ScanResisterCodeTarget(assembly);
            PostProcessManager.Ins.ScanRegisterPostProcess(assembly);
            OutputSaverManager.Ins.ScanRegisterOutputSaver(assembly);
        }

        foreach (var plugin in PluginManager.Ins.Plugins)
        {
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
        var defAss = new DefAssembly(ass);
        var genArgs = new GenerationArguments()
        {
            Target = "all",
            GeneralArgs = new()
            {
                {"global.outputCodeDir", @"Output/Code"},
                {"global.outputDataDir", @"Output/Data"},
            },
        };

        var genCtx = new GenerationContext(defAss, genArgs);
        
        ICodeTarget csBinTarget = CodeTargetManager.Ins.GetCodeTarget("cs-bin");
        var outputManifest = new OutputFileManifest();
        csBinTarget.GenerateCode(genCtx, outputManifest);

        var output2 = new OutputFileManifest();
        PostProcessManager.Ins.GetPostProcess("nop").PostProcess(outputManifest, output2);

        var saver = OutputSaverManager.Ins.GetOutputSaver("local");
        saver.Save(output2);
        
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