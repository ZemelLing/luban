namespace Luban.Core.DataTarget;

[AttributeUsage(AttributeTargets.Class)]
public class DataTargetAttribute : System.Attribute
{
    public string Name { get; }
    
    public bool AllTablesInOneFile { get; }
    
    public DataTargetAttribute(string name, bool allTablesInOneFile = false)
    {
        Name = name;
        AllTablesInOneFile = allTablesInOneFile;
    }
}