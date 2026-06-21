using FluentValidation;
using MedCabinet.Application.DTOs.Household;
using MedCabinet.Application.DTOs.HouseholdMember;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.DTOs.MedUsage;
using MedCabinet.Application.DTOs.MedAlert;

namespace MedCabinet.Application.Validators;

public class CreateHouseholdRequestValidator : AbstractValidator<CreateHouseholdRequestDto>
{
    public CreateHouseholdRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("家庭名称不能为空")
            .Length(2, 100).WithMessage("家庭名称长度必须在2-100个字符之间");
    }
}

public class CreateMedicineRequestValidator : AbstractValidator<CreateMedicineRequestDto>
{
    public CreateMedicineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("药品名称不能为空")
            .Length(1, 200).WithMessage("药品名称长度必须在1-200个字符之间");

        RuleFor(x => x.HouseholdId)
            .GreaterThan(0).WithMessage("家庭ID必须大于0");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("药品分类不能为空")
            .Length(1, 50).WithMessage("药品分类长度必须在1-50个字符之间");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("库存数量不能为负数");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("有效期不能为空");
    }
}

public class UpdateMedicineRequestValidator : AbstractValidator<UpdateMedicineRequestDto>
{
    public UpdateMedicineRequestValidator()
    {
        RuleFor(x => x.Name)
            .Length(1, 200).WithMessage("药品名称长度必须在1-200个字符之间")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("库存数量不能为负数")
            .When(x => x.StockQuantity.HasValue);
    }
}

public class CreateMedUsageRequestValidator : AbstractValidator<CreateMedUsageRequestDto>
{
    public CreateMedUsageRequestValidator()
    {
        RuleFor(x => x.MedicineId)
            .GreaterThan(0).WithMessage("药品ID必须大于0");

        RuleFor(x => x.UsedBy)
            .NotEmpty().WithMessage("用药人不能为空")
            .Length(1, 50).WithMessage("用药人长度必须在1-50个字符之间");

        RuleFor(x => x.UsedQuantity)
            .GreaterThan(0).WithMessage("使用数量必须大于0");
    }
}

public class CreateMedAlertRequestValidator : AbstractValidator<CreateMedAlertRequestDto>
{
    public CreateMedAlertRequestValidator()
    {
        RuleFor(x => x.MedicineId)
            .GreaterThan(0).WithMessage("药品ID必须大于0");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("提醒消息不能为空")
            .Length(1, 500).WithMessage("提醒消息长度必须在1-500个字符之间");
    }
}
