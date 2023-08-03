using Luban.Core.Defs;

namespace Luban.Core.DataTarget;

public abstract class DataExporterBase : IDataExporter
{
    public const string FamilyPrefix = "dataExporter";
    
    public virtual void Handle(GenerationContext ctx, IDataTarget dataTarget, OutputFileManifest manifest)
    {
        if (!dataTarget.AllTablesInOneFile)
        {
            var tasks = ctx.ExportTables.Select(table => Task.Run(() => ExportTable(table, manifest, dataTarget))).ToArray();
            Task.WaitAll(tasks);
        }
        else
        {
            manifest.AddFile(dataTarget.ExportAllInOne(ctx.ExportTables));
        }
    }

    protected abstract void ExportTable(DefTable table, OutputFileManifest manifest, IDataTarget dataTarget);
}