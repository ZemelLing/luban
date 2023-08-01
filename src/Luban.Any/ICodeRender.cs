using Luban.Core.Defs;

namespace Luban.Job.Common.Generate;

public interface ICodeRender<T> where T : DefTypeBase
{
    string RenderAny(DefTypeBase o);

    string Render(DefEnum c);

    string RenderService(string name, string module, List<T> tables);
}