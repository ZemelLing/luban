using Luban.Core.Mission;

namespace Luban.Core.DataExport;

public interface IDataExporter
{
    void Handle(GenerationContext ctx, OutputFileManifest manifest);
}