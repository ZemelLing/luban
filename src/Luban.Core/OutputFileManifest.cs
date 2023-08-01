namespace Luban.Core;

public class OutputFileManifest
{
    private readonly List<OutputFile> _dataFiles = new();

    public IReadOnlyList<OutputFile> DataFiles => _dataFiles;
    

    public void AddFile(string file, object content)
    {
        _dataFiles.Add(new OutputFile { File = file, Content = content });
    }
    
    public void AddFile(OutputFile file)
    {
        _dataFiles.Add(file);
    }
}