using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_cs_bin")]
[Render("code_cs_unity_bin")]
class CsCodeBinRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "cs_bin";
}