using Luban.Job.Common;
using Luban.Job.Common.Generate;
using Luban.Job.Common.Utils;

namespace Luban.Generate;

[Render("code_template")]
class TemplateCodeScatterRender : TemplateCodeRenderBase
{
    protected override string RenderTemplateDir => GenContext.Ctx.GenArgs.TemplateCodeDir;

    protected override ELanguage GetLanguage(GenContext ctx)
    {
        return RenderFileUtil.GetLanguage(ctx.GenArgs.TemplateCodeDir);
    }
}