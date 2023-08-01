namespace Luban.Core.PostProcess;

public interface IPostProcessCode
{
    void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest);
    void PostProcess(OutputFileManifest oldOutputFileManifest, OutputFileManifest newOutputFileManifest, OutputFile outputFile);
}