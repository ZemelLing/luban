namespace Luban.Core.PostProcess;

public abstract class PostProcessCodeBase : IPostProcessCode
{
    public virtual void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest)
    {
        foreach (var outputFile in oldOutputFileManifest.DataFiles)
        {
            PostProcess(oldOutputFileManifest, newOutputFileManifest, outputFile);
        }
    }

    public virtual void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest,
        OutputFile outputFile)
    {
        newOutputFileManifest.AddFile(outputFile);
    }
}