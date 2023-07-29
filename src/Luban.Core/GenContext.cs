using Luban.Defs;
using Luban.RawDefs;
using Luban.Job.Common.Defs;
using System.Collections.Concurrent;

namespace Luban;

public class GenContext
{
    private readonly static AsyncLocal<GenContext> s_asyncLocal = new();

    public static GenContext Ctx { get => s_asyncLocal.Value; set => s_asyncLocal.Value = value; }

    public GenArgs GenArgs { get; init; }
    public DefAssembly Assembly { get; init; }
    public string GenType { get; set; }
        
    // public ICfgCodeRender Render { get; set; }
        
    public ELanguage Lan { get; set; }

    public string TopModule => Assembly.TopModule;
    public Service TargetService => Assembly.CfgTargetService;

    public Func<Task> DataLoader { get; set; }

    public List<DefTypeBase> ExportTypes { get; init; }
    public List<DefTable> ExportTables { get; init; }
    public ConcurrentBag<FileInfo> GenCodeFilesInOutputCodeDir { get; init; }
    public ConcurrentBag<FileInfo> GenDataFilesInOutputDataDir { get; init; }
    public ConcurrentBag<FileInfo> GenScatteredFiles { get; init; }
    public List<Task> Tasks { get; init; }
}