using Luban.Core.Utils;

namespace Luban.Core.OutputSaver;

[OutputSaver("local")]
public class LocalFileSaver : OutputSaverBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    private string OutputDir => GenerationContext.Ins.GetOutputCodePath("local");

    protected override void BeforeSave(OutputFileManifest outputFileManifest)
    {
        FileCleaner.Clean(OutputDir, outputFileManifest.DataFiles.Select(f => f.File).ToList());
    }

    public override void SaveFile(OutputFileManifest fileManifest, OutputFile outputFile)
    {
        string fullOutputPath = $"{OutputDir}/{outputFile.File}";
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
        File.WriteAllBytes(fullOutputPath, outputFile.GetContentBytes());
        s_logger.Info("save file:{} ", fullOutputPath);
    }
}