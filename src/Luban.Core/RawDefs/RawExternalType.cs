namespace Luban.Core.RawDefs;

public class ExternalTypeMapper
{
    public string Selector { get; set; }

    public string Lan { get; set; }

    public string TargetTypeName { get; set; }

    public string CreateExternalObjectFunction { get; set; }
}

public class RawExternalType
{
    public string Name { get; set; }

    public string OriginTypeName { get; set; }

    public List<ExternalTypeMapper> Mappers { get; set; } = new List<ExternalTypeMapper>();
}