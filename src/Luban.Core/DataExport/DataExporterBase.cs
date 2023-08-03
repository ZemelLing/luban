using Luban.Core.Defs;

namespace Luban.Core.DataExport;

public abstract class DataExporterBase : IDataExporter
{
    public const string FamilyPrefix = "dataExporter";
    
    public virtual void Handle(GenerationContext ctx, ITableExporter tableExporter, OutputFileManifest manifest)
    {
        if (!tableExporter.AllTablesInOneFile)
        {
            var tasks = ctx.ExportTables.Select(table => Task.Run(() => ExportTable(table, manifest, tableExporter))).ToArray();
            Task.WaitAll(tasks);
        }
        else
        {
            manifest.AddFile(tableExporter.ExportAllInOne(ctx.ExportTables));
        }
    }

    protected abstract void ExportTable(DefTable table, OutputFileManifest manifest, ITableExporter tableExporter);
}