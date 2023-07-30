using Luban.Core;

namespace Luban.Generate;

abstract class DataRenderBase : IRender
{
    public abstract void Render(GenerationContext ctx);
}