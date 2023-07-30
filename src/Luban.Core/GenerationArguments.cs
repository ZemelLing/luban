namespace Luban.Core;

public class GenerationArguments
{
    public string Target { get; set; }
    
    public string OutputTables { get; set; }

    public string OutputIncludeTables { get; set; }
    
    public string OutputExcludeTables { get; set; }
    

    public TimeZoneInfo TimeZone { get; set; }

    public bool OutputCompactJson { get; set; }

    public List<string> ExcludeTags { get; set; }
}