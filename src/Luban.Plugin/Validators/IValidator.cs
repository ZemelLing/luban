using Luban.Datas;
using Luban.Job.Common.Defs;
using Luban.Job.Common.Types;

namespace Luban.Validators;

public interface IValidator : IProcessor
{
    void Validate(ValidatorContext ctx, TType type, DType data);
}