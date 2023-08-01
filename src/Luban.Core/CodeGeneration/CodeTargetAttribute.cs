namespace Luban.Core.CodeGeneration;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CodeTargetAttribute : Attribute
{
    public string Name { get; }
    
    public CodeTargetAttribute(string name)
    {
        Name = name;
    }
}