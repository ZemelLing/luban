using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_typescript_json")]
class TypescriptCodeJsonRender : TypescriptCodeRenderBase
{
    protected override string RenderTemplateDir => "typescript_json";
}