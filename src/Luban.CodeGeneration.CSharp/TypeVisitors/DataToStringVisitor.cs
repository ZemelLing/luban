﻿using Luban.Core.Types;
using Luban.Core.TypeVisitors;

namespace Luban.CodeGeneration.CSharp.TypeVisitors;

public class DataToStringVisitor : DecoratorFuncVisitor<string, string>
{
    public static DataToStringVisitor Ins { get; } = new();

    public override string DoAccept(TType type, string fieldName)
    {
        return fieldName;
    }

    public override string Accept(TArray type, string fieldName)
    {
        return $"Luban.StringUtil.CollectionToString({fieldName})";
    }

    public override string Accept(TList type, string fieldName)
    {
        return $"Luban.StringUtil.CollectionToString({fieldName})";
    }

    public override string Accept(TSet type, string fieldName)
    {
        return $"Luban.StringUtil.CollectionToString({fieldName})";
    }

    public override string Accept(TMap type, string fieldName)
    {
        return $"Luban.StringUtil.CollectionToString({fieldName})";
    }
}