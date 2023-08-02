using Luban.CodeGeneration.CSharp.TemplateExtensions;
using Luban.Core;
using Luban.Core.CodeGeneration;
using Luban.Core.Defs;
using Scriban;

namespace Luban.CodeGeneration.CSharp.CodeTargets;

[CodeTarget("cs-bin")]
public class CsharpBin : CsharpCodeTargetBase
{
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        base.OnCreateTemplateContext(ctx);
        ctx.PushGlobal(new CsharpBinTemplateExtension());
    }
}