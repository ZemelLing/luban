using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_java_bin")]
class JavaCodeBinRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "java_bin";
}