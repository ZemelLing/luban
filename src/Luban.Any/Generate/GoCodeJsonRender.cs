using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_go_json")]
class GoCodeJsonRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "go_json";
}