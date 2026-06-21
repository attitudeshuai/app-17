using FluentValidation;
using MedCabinet.Application.DTOs.Auth;

namespace MedCabinet.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3-50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确")
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .Length(6, 50).WithMessage("密码长度必须在6-50个字符之间");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Username)
            .Length(3, 50).WithMessage("用户名长度必须在3-50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线")
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
