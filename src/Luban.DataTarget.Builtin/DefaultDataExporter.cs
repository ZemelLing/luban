using Luban.Core;
using Luban.Core.DataTarget;
using Luban.Core.Defs;

namespace Luban.DataExporter.Builtin;

[DataExporter("default")]
public class DefaultDataExporter : DataExporterBase
{
    protected override void ExportTable(DefTable table, OutputFileManifest manifest, IDataTarget dataTarget)
    {
        GenerationContext ctx = GenerationContext.Ins;
        var outputFile = dataTarget.Export(table, ctx.GetTableExportDataList(table));
        manifest.AddFile(outputFile);
    }
}