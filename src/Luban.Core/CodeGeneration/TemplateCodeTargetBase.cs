using Luban.Core.CodeFormat;
using Luban.Core.Defs;
using Luban.Core.TemplateExtensions;
using Luban.Core.Tmpl;
using Luban.Core.Utils;
using Scriban;
using Scriban.Runtime;

namespace Luban.Core.CodeGeneration;

public abstract class TemplateCodeTargetBase : CodeTargetBase
{
    protected virtual string CommonTemplateSearchPath => $"common/{FileSuffixName}";

    protected virtual ICodeStyle DefaultCodeStyle => CodeFormatManager.Ins.NoneCodeStyle;

    protected virtual ICodeStyle CodeStyle => GenerationContext.Ins.GetCodeStyle(Name) ?? DefaultCodeStyle;
    
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
        if (TemplateManager.Ins.TryGetTemplate($"{Name}/{name}", out var template))
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
            { "__code_style", CodeStyle},
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
            { "__namespace", table.Namespace },
            { "__namespace_with_top_module", table.NamespaceWithTopModule },
            { "__full_name_with_top_module", table.FullNameWithTopModule },
            { "__table", table },
            { "__code_style", CodeStyle},
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
            { "__namespace", bean.Namespace },
            { "__namespace_with_top_module", bean.NamespaceWithTopModule },
            { "__full_name_with_top_module", bean.FullNameWithTopModule },
            { "__bean", bean },
            { "__code_style", CodeStyle},
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
            { "__namespace", @enum.Namespace },
            { "__namespace_with_top_module", @enum.NamespaceWithTopModule },
            { "__full_name_with_top_module", @enum.FullNameWithTopModule },
            { "__enum", @enum },
            { "__code_style", CodeStyle},
        };
        tplCtx.PushGlobal(extraEnvs);
        writer.Write(template.Render(tplCtx));
    }
}