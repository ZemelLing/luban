using Luban.Core;
using Luban.Core.DataExport;
using Luban.Core.Defs;

namespace Luban.DataExporter.Builtin;

[DataExporter("default")]
public class DefaultDataExporter : DataExporterBase
{
    protected override void ExportTable(DefTable table, OutputFileManifest manifest)
    {
        throw new NotImplementedException();
    }
}