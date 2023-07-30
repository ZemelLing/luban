using Luban.Core.Defs;
using Luban.Job.Common.Generate;

namespace Luban.Generate;

interface ICfgCodeRender : ICodeRender<DefTable>, IRender
{
    string Render(DefBean b);

    string Render(DefTable c);
}