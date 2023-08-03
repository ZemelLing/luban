namespace Luban.Core.DataExport;

[AttributeUsage(AttributeTargets.Class)]
public class TableExporterAttribute : System.Attribute
{
    public string Name { get; }
    
    public bool AllTablesInOneFile { get; }
    
    public TableExporterAttribute(string name, bool allTablesInOneFile = false)
    {
        Name = name;
        AllTablesInOneFile = allTablesInOneFile;
    }
}