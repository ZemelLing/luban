namespace Luban.Core.CodeFormat.CodeStyles;

public class ConfigurableCodeStyle : CodeStyleBase
{
    private readonly INamingConventionFormatter _namespaceFormatter;
    private readonly INamingConventionFormatter _typeFormatter;
    private readonly INamingConventionFormatter _methodFormatter;
    private readonly INamingConventionFormatter _propertyFormatter;
    private readonly INamingConventionFormatter _fieldFormatter;
    
    public ConfigurableCodeStyle(string namespaceFormatterName, string typeFormatterName, string methodFormatterName, string propertyFormatterName, string fieldFormatterName)
    {
        _namespaceFormatter = CodeFormatManager.Ins.GetFormatter(namespaceFormatterName);
        _typeFormatter = CodeFormatManager.Ins.GetFormatter(typeFormatterName);
        _methodFormatter = CodeFormatManager.Ins.GetFormatter(methodFormatterName);
        _propertyFormatter = CodeFormatManager.Ins.GetFormatter(propertyFormatterName);
        _fieldFormatter = CodeFormatManager.Ins.GetFormatter(fieldFormatterName);
    }

    public override string FormatNamespace(string ns)
    {
        return _namespaceFormatter.FormatName(ns);
    }

    public override string FormatType(string typeName)
    {
        return _typeFormatter.FormatName(typeName);
    }

    public override string FormatMethod(string methodName)
    {
        return _methodFormatter.FormatName(methodName);
    }
    
    public override string FormatProperty(string propertyName)
    {
        return _propertyFormatter.FormatName(propertyName);
    }

    public override string FormatField(string fieldName)
    {
        return _fieldFormatter.FormatName(fieldName);
    }
}