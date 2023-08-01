namespace Luban.Core;

public class OutputFile
{
    public string File { get; init; }
    
    /// <summary>
    /// Data type: string or byte[]
    /// </summary>
    public object Content { get; init; }
}