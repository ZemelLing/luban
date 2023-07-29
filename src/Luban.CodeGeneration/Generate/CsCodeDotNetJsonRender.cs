using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_cs_dotnet_json")]
class CsCodeDotNetJsonRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => "cs_dotnet_json";
}