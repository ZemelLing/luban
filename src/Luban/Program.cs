using CommandLine;
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
        PluginManager.Ins.Init(new DefaultPluginCollector($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Plugins"));

        int processorCount = Environment.ProcessorCount;
        ThreadPool.SetMinThreads(Math.Max(4, processorCount), 5);
        ThreadPool.SetMaxThreads(Math.Max(16, processorCount * 4), 10);
        s_logger.Info("ThreadPool.SetThreads");
        
        s_logger.Info("start");
        
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