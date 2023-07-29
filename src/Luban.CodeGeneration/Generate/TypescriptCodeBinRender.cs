using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_typescript_bin")]
class TypescriptCodeBinRender : TypescriptCodeRenderBase
{
    protected override string RenderTemplateDir => "typescript_bin";
}