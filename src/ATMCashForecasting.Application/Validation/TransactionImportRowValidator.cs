using ATMCashForecasting.Application.DTOs;
using FluentValidation;

namespace ATMCashForecasting.Application.Validation;

/// <summary>
/// Data validation engine (Module 3) run against every row of a CSV/Excel/API transaction import
/// before it is persisted. Rows failing validation are rejected and reported back with row-level errors.
/// </summary>
public class TransactionImportRowValidator : AbstractValidator<TransactionImportRowDto>
{
    public TransactionImportRowValidator()
    {
        RuleFor(x => x.AtmCode)
            .NotEmpty().WithMessage("ATM Code is required.")
            .MaximumLength(20);

        RuleFor(x => x.TransactionDate)
            .NotEqual(default(DateOnly)).WithMessage("Transaction date is required.")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Transaction date cannot be in the future.");

        RuleFor(x => x.WithdrawalCount)
            .GreaterThanOrEqualTo(0).WithMessage("Withdrawal count cannot be negative.");

        RuleFor(x => x.WithdrawalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Withdrawal amount cannot be negative.");

        RuleFor(x => x.DepositAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Deposit amount cannot be negative.");

        RuleFor(x => x.RemainingCash)
            .GreaterThanOrEqualTo(0).WithMessage("Remaining cash cannot be negative.");

        RuleFor(x => x.CashLoaded)
            .GreaterThanOrEqualTo(0).WithMessage("Cash loaded cannot be negative.");

        RuleFor(x => x)
            .Must(x => x.WithdrawalCount == 0 || x.WithdrawalAmount > 0)
            .WithMessage("Withdrawal amount must be greater than zero when withdrawal count is greater than zero.");
    }
}
