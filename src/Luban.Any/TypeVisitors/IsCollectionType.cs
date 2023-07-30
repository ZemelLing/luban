using Luban.Core.Types;

namespace Luban.Core.TypeVisitors;

public class IsCollectionType : AllFalseVisitor
{
    public static IsCollectionType Ins { get; } = new IsCollectionType();


    public override bool Accept(TArray type)
    {
        return true;
    }

    public override bool Accept(TList type)
    {
        return true;
    }

    public override bool Accept(TSet type)
    {
        return true;
    }

    public override bool Accept(TMap type)
    {
        return true;
    }
}