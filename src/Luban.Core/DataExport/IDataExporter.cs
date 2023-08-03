
namespace Luban.Core.DataExport;

public interface IDataExporter
{
    void Handle(GenerationContext ctx, ITableExporter tableExporter, OutputFileManifest manifest);
}