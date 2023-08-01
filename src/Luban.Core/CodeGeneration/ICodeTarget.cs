using Luban.Core.Defs;

namespace Luban.Core.CodeGeneration;

public interface ICodeTarget
{
    string FileHeader { get; }

    string GetPathFromFullName(string fullName);
    
    void GenerateCode(GenerationContext ctx, OutputFileManifest manifest);
    
    void GenerateTables(GenerationContext ctx, List<DefTable> tables, CodeWriter writer);

    void GenerateTable(GenerationContext ctx, DefTable table, CodeWriter writer);

    void GenerateBean(GenerationContext ctx, DefBean bean, CodeWriter writer);
    
    void GenerateEnum(GenerationContext ctx, DefEnum @enum, CodeWriter writer);
}