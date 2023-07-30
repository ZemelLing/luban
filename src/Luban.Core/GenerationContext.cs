using System.Collections.Concurrent;
using Luban.Core.Defs;
using Luban.Core.RawDefs;

namespace Luban.Core;

public class GenerationContext
{
    public static GenerationContext Ctx { get; set; }

    public DefAssembly Assembly { get; init; }
        
    public string Language { get; set; }

    public string TopModule => Assembly.TopModule;
    public RawTarget TargetRawTarget => Assembly.Target;

    public Func<Task> DataLoader { get; set; }

    public List<DefTypeBase> ExportTypes { get; init; }
    public List<DefTable> ExportTables { get; init; }
    public ConcurrentBag<FileInfo> GenCodeFilesInOutputCodeDir { get; init; }
    public ConcurrentBag<FileInfo> GenDataFilesInOutputDataDir { get; init; }
    public ConcurrentBag<FileInfo> GenScatteredFiles { get; init; }
    public List<Task> Tasks { get; init; }
}