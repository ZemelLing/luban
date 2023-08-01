using Luban.Job.Common.Generate;

namespace Luban.Generate;

[Render("code_cs_unity_editor_json")]
class CsUnityEditorRender : TemplateEditorJsonCodeRenderBase
{
    override protected string RenderTemplateDir => "cs_unity_editor_json";
}