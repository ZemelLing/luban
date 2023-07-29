using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_java_json")]
class JavaCodeJsonRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "java_json";
}