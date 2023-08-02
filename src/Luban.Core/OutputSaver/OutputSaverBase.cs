namespace Luban.Core.OutputSaver;

public abstract class OutputSaverBase : IOutputSaver
{

    protected virtual void BeforeSave(OutputFileManifest outputFileManifest)
    {

    }

    protected virtual void PostSave(OutputFileManifest outputFileManifest)
    {

    }
    
    public virtual void Save(OutputFileManifest outputFileManifest)
    {
        BeforeSave(outputFileManifest);
        var tasks = new List<Task>();
        foreach (var outputFile in outputFileManifest.DataFiles)
        {
            tasks.Add(Task.Run(() =>
            {
                SaveFile(outputFileManifest, outputFile);
            }));
        }
        Task.WaitAll(tasks.ToArray());
        PostSave(outputFileManifest);
    }

    public abstract void SaveFile(OutputFileManifest fileManifest, OutputFile outputFile);
}