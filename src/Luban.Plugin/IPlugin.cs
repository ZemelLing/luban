
namespace Luban.Plugin;

public interface IPlugin
{
    string Name { get; }

    void Init(string jsonStr);

    void Start();
}