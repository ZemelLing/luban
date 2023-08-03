﻿using Luban.Core.Types;
using Luban.Core.TypeVisitors;

namespace Luban.CodeTarget.CSharp.TypeVisitors;

public class IsRawNullableTypeVisitor : AllFalseVisitor
{
    public static IsRawNullableTypeVisitor Ins { get; } = new();

    public override bool Accept(TString type)
    {
        return true;
    }

    public override bool Accept(TText type)
    {
        return true;
    }

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

    public override bool Accept(TBean type)
    {
        return true;
    }
}