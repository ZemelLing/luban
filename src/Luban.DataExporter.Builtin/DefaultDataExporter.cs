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
        tableExporter.Export(table, ctx.GetTableExportDataList(table));
    }

    protected override ITableExporter TableExporter
    {
        get
        {
            GenerationContext ctx = GenerationContext.Ins;
            string tableExporterName = ctx.GetOption($"{FamilyPrefix}.default", "tableExporter", true);
            return DataExporterManager.Ins.GetTableExporter(tableExporterName);
        }
        
    }
}