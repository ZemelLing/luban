namespace Luban.Core.Mission;

public class MissionManager
{
    public static MissionManager Ins { get; } = new MissionManager();

    private readonly Dictionary<string, IMission> _missions = new Dictionary<string, IMission>();

    public void Init()
    {
        
    }

    public IMission GetMission(string name)
    {
        return _missions.TryGetValue(name, out var mission)
            ? mission
            : throw new Exception($"mission:{name} not exists");
    }

    public void RegisterMission(string name, IMission mission)
    {
        if (!_missions.TryAdd(name, mission))
        {
            throw new Exception($"mission:{name} already exists");
        }
    }
}