using Luban.Core.RawDefs;
using Luban.Core.TypeVisitors;
using Luban.Core.Utils;

namespace Luban.Core.Defs;

public class DefBean : DefTypeBase
{
    public const string FALLBACK_TYPE_NAME_KEY = "__type__";

    public const string BEAN_NULL_STR = "null";

    public const string BEAN_NOT_NULL_STR = "{}";

    public const string JSON_TYPE_NAME_KEY = "$type";

    public const string XML_TYPE_NAME_KEY = "type";

    public const string LUA_TYPE_NAME_KEY = "_type_";

    public const string EXCEL_TYPE_NAME_KEY = "$type";
    public const string EXCEL_VALUE_NAME_KEY = "$value";

    public bool IsBean => true;

    public string Parent { get; }

    public bool IsValueType { get; }

    public DefBean ParentDefType { get; protected set; }

    public DefBean RootDefType => this.ParentDefType == null ? this : this.ParentDefType.RootDefType;

    public bool IsSerializeCompatible { get; }

    public List<DefBean> Children { get; set; }

    public List<DefBean> HierarchyNotAbstractChildren { get; set; }

    public IEnumerable<DefBean> GetHierarchyChildren()
    {
        yield return this;
        if (Children == null)
        {
            yield break;
        }
        foreach (var child in Children)
        {
            foreach(var c2 in  child.GetHierarchyChildren())
            {
                yield return c2;
            }
        }
    }

    public bool IsNotAbstractType => Children == null;

    public bool IsAbstractType => Children != null;

    public List<DefField> HierarchyFields { get; private set; } = new List<DefField>();

    public List<DefField> Fields { get; } = new List<DefField>();

    public string CsClassModifier => IsAbstractType ? "abstract" : "sealed";

    public string CsMethodModifier => ParentDefType != null ? "override" : (IsAbstractType ? "virtual" : "");


    public string JavaClassModifier => IsAbstractType ? "abstract" : "final";

    public string JavaMethodModifier => ParentDefType != null ? "override" : (IsAbstractType ? "virtual" : "");

    public string TsClassModifier => IsAbstractType ? "abstract" : "";
    
    public string JsonTypeNameKey => JSON_TYPE_NAME_KEY;

    public string LuaTypeNameKey => LUA_TYPE_NAME_KEY;

    public string Alias { get; }

    public bool IsMultiRow { get; set; }

    public string Sep { get; }

    public List<DefField> HierarchyExportFields { get; private set; }

    public List<DefField> ExportFields { get; private set; }

    public int AutoId { get; set; }

    public bool IsDefineEquals(DefBean b)
    {
        return DeepCompareTypeDefine.Ins.Compare(this, b, new Dictionary<DefTypeBase, bool>(), new HashSet<DefTypeBase>());
    }

    public DefBean(RawBean b)
    {
        Name = b.Name;
        Namespace = b.Namespace;
        Parent = b.Parent;
        Id = b.TypeId;
        IsValueType = b.IsValueType;
        Comment = b.Comment;
        Tags = DefUtil.ParseAttrs(b.Tags);
        foreach (var field in b.Fields)
        {
            Fields.Add(CreateField(field, 0));
        }
        Alias = b.Alias;
        Id = b.TypeId;
        Sep = b.Sep;
    }

    protected DefField CreateField(RawField f, int idOffset)
    {
        return new DefField(this, f, idOffset);
    }

    public DefField GetField(string index)
    {
        return HierarchyFields.Where(f => f.Name == index).FirstOrDefault();
    }

    internal bool TryGetField(string index, out DefField field, out int fieldIndexId)
    {
        for (int i = 0; i < HierarchyFields.Count; i++)
        {
            if (HierarchyFields[i].Name == index)
            {
                field = (DefField)HierarchyFields[i];
                fieldIndexId = i;
                return true;
            }
        }
        field = null;
        fieldIndexId = 0;
        return false;
    }

    public DefBean GetNotAbstractChildType(string typeNameOrAliasName)
    {
        if (string.IsNullOrWhiteSpace(typeNameOrAliasName))
        {
            return null;
        }
        foreach (DefBean c in HierarchyNotAbstractChildren)
        {
            if (c.Name == typeNameOrAliasName || c.Alias == typeNameOrAliasName)
            {
                return c;
            }
        }
        return null;
    }

    public override void PreCompile()
    {
        SetUpParentRecursively();
        CollectHierarchyFields(HierarchyFields);
        this.ExportFields = this.Fields.Select(f => (DefField)f).Where(f => f.NeedExport).ToList();
        this.HierarchyExportFields = this.HierarchyFields.Select(f => (DefField)f).Where(f => f.NeedExport).ToList();
    }

    public override void Compile()
    {
        var cs = new List<DefBean>();
        if (Children != null)
        {
            CollectHierarchyNotAbstractChildren(cs);
        }
        HierarchyNotAbstractChildren = cs;
        if (Id != 0)
        {
            throw new Exception($"bean:'{FullName}' beanid:{Id} should be 0!");
        }
        else
        {
            Id = TypeUtil.ComputCfgHashIdByName(FullName);
        }
        // 检查别名是否重复
        HashSet<string> nameOrAliasName = cs.Select(b => b.Name).ToHashSet();
        foreach (DefBean c in cs)
        {
            if (!string.IsNullOrWhiteSpace(c.Alias) && !nameOrAliasName.Add(c.Alias))
            {
                throw new Exception($"bean:'{FullName}' alias:{c.Alias} 重复");
            }
        }
        DefField.CompileFields(this, HierarchyFields, false);

        var allocAutoIds = this.HierarchyFields.Select(f => f.Id).ToHashSet();

        int nextAutoId = 1;
        foreach (var f in this.HierarchyFields)
        {
            while (!allocAutoIds.Add(nextAutoId))
            {
                ++nextAutoId;
            }
            f.AutoId = nextAutoId;
        }
    }

    public void PostCompile()
    {
        foreach (var field in HierarchyFields)
        {
            field.PostCompile();
        }
        if (this.IsAbstractType && this.ParentDefType == null)
        {
            int nextAutoId = 0;
            foreach (DefBean c in this.HierarchyNotAbstractChildren)
            {
                c.AutoId = ++nextAutoId;
            }
        }
    }
    
    public void CollectHierarchyNotAbstractChildren(List<DefBean> children)
    {
        if (IsAbstractType)
        {
            foreach (var c in Children)
            {
                c.CollectHierarchyNotAbstractChildren(children);
            }
        }
        else
        {
            children.Add(this);
        }
    }

    protected void CollectHierarchyFields(List<DefField> fields)
    {
        if (ParentDefType != null)
        {
            ParentDefType.CollectHierarchyFields(fields);
        }
        fields.AddRange(Fields);
    }

    private void SetUpParentRecursively()
    {
        if (ParentDefType == null && !string.IsNullOrEmpty(Parent))
        {
            if ((ParentDefType = (DefBean)Assembly.GetDefType(Namespace, Parent)) == null)
            {
                throw new Exception($"bean:'{FullName}' parent:'{Parent}' not exist");
            }
            if (ParentDefType.Children == null)
            {
                ParentDefType.Children = new List<DefBean>();
            }
            ParentDefType.Children.Add(this);
            ParentDefType.SetUpParentRecursively();
        }
    }
}