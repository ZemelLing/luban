namespace Luban.Core.OutputSaver;

public interface IOutputSaver
{
    void Save(OutputFileManifest outputFileManifest);

    void SaveFile(OutputFileManifest fileManifest, OutputFile outputFile);
}