namespace Luban.Plugin.Schema;

public class SchemaRootCollectorAttribute
{
    public string Name { get; }
    
    public SchemaRootCollectorAttribute(string name)
    {
        Name = name;
    }
}