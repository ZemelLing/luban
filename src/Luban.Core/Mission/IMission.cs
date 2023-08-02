namespace Luban.Core.Mission;

public interface IMission
{
    void Handle(GenerationContext ctx, OutputFileManifest manifest);
}