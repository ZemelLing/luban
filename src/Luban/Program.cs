using CommandLine;
using Luban.Plugin;
using Luban.Utils;
using NLog;
using System.Text;

namespace Luban;

internal class Program
{
    
    private static ILogger s_logger;

    static void Main(string[] args)
    {
        ConsoleWindow.EnableQuickEditMode(false);
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        LogUtil.InitSimpleNLogConfigure(LogLevel.Info);
        s_logger = LogManager.GetCurrentClassLogger();
        s_logger.Info("init logger success");
        PluginManager.Ins.Init(new DefaultPluginCollector(@"D:\workspace2\luban\src\Luban\bin\Debug\net7.0\Plugins"));

        int processorCount = Environment.ProcessorCount;
        ThreadPool.SetMinThreads(Math.Max(4, processorCount), 5);
        ThreadPool.SetMaxThreads(Math.Max(16, processorCount * 4), 10);
        s_logger.Info("ThreadPool.SetThreads");
        
        s_logger.Info("start");
        
        s_logger.Info("bye~");
    }

    
}