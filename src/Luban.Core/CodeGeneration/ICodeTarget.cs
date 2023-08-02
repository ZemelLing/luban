using Luban.Core.Defs;
using Luban.Core.Mission;

namespace Luban.Core.CodeGeneration;

public interface ICodeTarget : IMission
{
    string FileHeader { get; }

    string GetPathFromFullName(string fullName);
    
    void GenerateTables(GenerationContext ctx, List<DefTable> tables, CodeWriter writer);

    void GenerateTable(GenerationContext ctx, DefTable table, CodeWriter writer);

    void GenerateBean(GenerationContext ctx, DefBean bean, CodeWriter writer);
    
    void GenerateEnum(GenerationContext ctx, DefEnum @enum, CodeWriter writer);
}