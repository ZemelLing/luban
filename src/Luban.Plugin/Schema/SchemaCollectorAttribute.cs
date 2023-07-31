namespace Luban.Plugin.Schema;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SchemaCollectorAttribute : System.Attribute
{
    public string Name { get; }
    
    public SchemaCollectorAttribute(string name)
    {
        Name = name;
    }
}