using Luban.Core.Defs;
using Luban.Core.Tmpl;
using Luban.Core.Utils;
using Scriban;
using Scriban.Runtime;

namespace Luban.Core.CodeGeneration;

public abstract class TemplateCodeTargetBase : CodeTargetBase
{
    protected virtual string CommonTemplateSearchPath => $"common/{FileSuffixName}";
    
    private TemplateContext CreateTemplateContext(Template template)
    {
        var ctx = new TemplateContext()
        {
            LoopLimit = 0,
            NewLine = "\n",
        };
        ctx.PushGlobal(new ContextTemplateExtends());
        ctx.PushGlobal(new TypeTemplateExtends());
        OnCreateTemplateContext(ctx);
        return ctx;
    }

    protected abstract void OnCreateTemplateContext(TemplateContext ctx);
    
    protected virtual Scriban.Template GetTemplate(string name)
    {
        if (TemplateManager.Ins.TryGetTemplate($"{TargetName}/{name}", out var template))
        {
            return template;
        }

        if (!string.IsNullOrWhiteSpace(CommonTemplateSearchPath) && TemplateManager.Ins.TryGetTemplate($"{CommonTemplateSearchPath}/{name}", out template))
        {
            return template;
        }
        throw new Exception($"template:{name} not found");
    }
    
    public override void GenerateTables(GenerationContext ctx, List<DefTable> tables, CodeWriter writer)
    {
        var template = GetTemplate("tables");
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx},
            { "__name", ctx.Target.Manager },
            { "__namespace", ctx.Target.TopModule },
            { "__tables", tables },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }

    public override void GenerateTable(GenerationContext ctx, DefTable table, CodeWriter writer)
    {
        var template = GetTemplate("table");
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx},
            { "__name", table.Name },
            { "__table", table },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }

    public override void GenerateBean(GenerationContext ctx, DefBean bean, CodeWriter writer)
    {
        var template = GetTemplate("bean");
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx},
            { "__name", bean.Name },
            { "__bean", bean },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }

    public override void GenerateEnum(GenerationContext ctx, DefEnum @enum, CodeWriter writer)
    {
        var template = GetTemplate("enum");
        var tplCtx = CreateTemplateContext(template);
        var extraEnvs = new ScriptObject
        {
            { "__ctx", ctx},
            { "__name", @enum.Name },
            { "__enum", @enum },
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }
}