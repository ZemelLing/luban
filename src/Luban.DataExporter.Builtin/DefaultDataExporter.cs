using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;

namespace Luban.DataExporter.Builtin;

[DataExporter("default")]
public class DefaultDataExporter : DataExporterBase
{
    protected override void ExportTable(DefTable table, OutputFileManifest manifest, ITableExporter tableExporter)
    {
        GenerationContext ctx = GenerationContext.Ins;
        var outputFile = tableExporter.Export(table, ctx.GetTableExportDataList(table));
        manifest.AddFile(outputFile);
    }
}