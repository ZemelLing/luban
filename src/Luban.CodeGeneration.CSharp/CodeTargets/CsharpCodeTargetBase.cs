using Luban.CodeGeneration.CSharp.TemplateExtensions;
using Luban.Core;
using Luban.Core.CodeGeneration;
using Luban.Core.Defs;
using Scriban;

namespace Luban.CodeGeneration.CSharp.CodeTargets;

public abstract class CsharpCodeTargetBase : TemplateCodeTargetBase
{
    public override string FileHeader => CommonFileHeaders.AUTO_GENERATE_C_LIKE;

    protected override string FileSuffixName => "cs";
    
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        ctx.PushGlobal(new CsharpTemplateExtension());
    }
}