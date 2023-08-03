using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public abstract class DataExporterBase : IDataExporter
{
    public virtual void Handle(GenerationContext ctx, OutputFileManifest manifest)
    {
        var tasks = ctx.ExportTables.Select(table => Task.Run(() => ExportTable(table, manifest))).ToArray();
        Task.WaitAll(tasks);
    }

    protected abstract void ExportTable(DefTable table, OutputFileManifest manifest);
}