using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_go_bin")]
class GoCodeBinRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "go_bin";
}