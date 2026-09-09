using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityPaymentService(
    RentalDbContext dbContext,
    IUtilityBillService utilityBillService) : IUtilityPaymentService
{
    public async Task<IReadOnlyList<UtilityBillPaymentDto>> GetAllAsync(int? utilityBillId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UtilityBillPayments
            .AsNoTracking()
            .Include(x => x.UtilityBill)
            .ThenInclude(x => x!.Payments)
            .AsQueryable();

        if (utilityBillId.HasValue)
        {
            query = query.Where(x => x.UtilityBillId == utilityBillId.Value);
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityBillPaymentDto> CreateAsync(CreateUtilityBillPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            if (request.Amount <= 0)
            {
                throw new AppValidationException("Payment amount must be greater than zero.");
            }

            var bill = await dbContext.UtilityBills
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == request.UtilityBillId, cancellationToken)
                ?? throw new AppValidationException("Utility bill does not exist.");

            if (bill.Status == UtilityBillStatus.Cancelled)
            {
                throw new AppValidationException("Cannot record payment for a cancelled bill.");
            }

            var payment = new UtilityBillPayment
            {
                UtilityBillId = request.UtilityBillId,
                PaymentMethodId = request.PaymentMethodId,
                PaymentDate = request.PaymentDate == default ? DateTime.UtcNow.Date : request.PaymentDate.Date,
                Amount = request.Amount,
                ReferenceNumber = request.ReferenceNumber?.Trim(),
                Notes = request.Notes?.Trim(),
                IsVoided = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.UtilityBillPayments.Add(payment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var (totalPaid, balance) = await RecomputeBillAndGetTotalsAsync(bill.Id, cancellationToken);
            await HandleOverpaymentCreditAsync(bill.UtilityCustomerId, bill.UtilityTypeId, payment.Id, bill.Amount, totalPaid, cancellationToken);
            (totalPaid, balance) = await RecomputeBillAndGetTotalsAsync(bill.Id, cancellationToken);

            return new UtilityBillPaymentDto
            {
                Id = payment.Id,
                UtilityBillId = payment.UtilityBillId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                IsVoided = payment.IsVoided,
                BillTotalPaidAfterPayment = totalPaid,
                BillBalanceAfterPayment = balance,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes,
                IsCreditApplication = false
            };
        }, cancellationToken);
    }

    public async Task<UtilityBillPaymentDto> VoidAsync(int paymentId, string? reason, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var payment = await dbContext.UtilityBillPayments
                .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
                ?? throw new AppNotFoundException("Utility payment not found.");

            if (payment.IsVoided)
            {
                throw new AppValidationException("Payment is already voided.");
            }

            payment.IsVoided = true;
            payment.Notes = string.IsNullOrWhiteSpace(reason)
                ? payment.Notes
                : string.IsNullOrWhiteSpace(payment.Notes)
                    ? $"Voided: {reason.Trim()}"
                    : $"{payment.Notes} | Voided: {reason.Trim()}";
            payment.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await ReversePaymentLinkedCreditsAsync(payment, cancellationToken);

            var (totalPaid, balance) = await RecomputeBillAndGetTotalsAsync(payment.UtilityBillId, cancellationToken);

            return new UtilityBillPaymentDto
            {
                Id = payment.Id,
                UtilityBillId = payment.UtilityBillId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                IsVoided = payment.IsVoided,
                BillTotalPaidAfterPayment = totalPaid,
                BillBalanceAfterPayment = balance,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes,
                IsCreditApplication = false
            };
        }, cancellationToken);
    }

    public async Task<UtilityBillPaymentDto> ApplyCreditAsync(int utilityBillId, decimal amount, string? notes, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            if (amount <= 0)
            {
                throw new AppValidationException("Credit amount must be greater than zero.");
            }

            var bill = await dbContext.UtilityBills
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == utilityBillId, cancellationToken)
                ?? throw new AppValidationException("Utility bill does not exist.");

            if (bill.Status == UtilityBillStatus.Cancelled)
            {
                throw new AppValidationException("Cannot apply credit to a cancelled bill.");
            }

            var latestBalance = await dbContext.UtilityCustomerCredits
                .Where(x => x.UtilityCustomerId == bill.UtilityCustomerId && x.UtilityTypeId == bill.UtilityTypeId)
                .OrderByDescending(x => x.OccurredAt)
                .Select(x => (decimal?)x.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;

            if (latestBalance <= 0)
            {
                throw new AppValidationException("No available credit for this utility customer.");
            }

            var currentPaid = bill.Payments.Where(x => !x.IsVoided).Sum(x => x.Amount);
            var currentBalance = bill.Amount - currentPaid;
            if (currentBalance <= 0)
            {
                throw new AppValidationException("This bill has no outstanding balance for credit application.");
            }

            var applyAmount = new[] { amount, latestBalance, currentBalance }.Min();

            var payment = new UtilityBillPayment
            {
                UtilityBillId = bill.Id,
                PaymentMethodId = null,
                PaymentDate = DateTime.UtcNow.Date,
                Amount = applyAmount,
                ReferenceNumber = "CREDIT-APPLIED",
                Notes = string.IsNullOrWhiteSpace(notes) ? "Applied from utility customer credit." : notes.Trim(),
                IsVoided = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.UtilityBillPayments.Add(payment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var newCreditBalance = latestBalance - applyAmount;
            dbContext.UtilityCustomerCredits.Add(new UtilityCustomerCredit
            {
                UtilityCustomerId = bill.UtilityCustomerId,
                UtilityTypeId = bill.UtilityTypeId,
                SourcePaymentId = payment.Id,
                Amount = -applyAmount,
                BalanceAfter = newCreditBalance,
                TransactionType = UtilityCreditTransactionType.AppliedToBill,
                OccurredAt = DateTime.UtcNow,
                Notes = "Credit applied to utility bill."
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            var (totalPaid, balance) = await RecomputeBillAndGetTotalsAsync(bill.Id, cancellationToken);

            return new UtilityBillPaymentDto
            {
                Id = payment.Id,
                UtilityBillId = payment.UtilityBillId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                IsVoided = payment.IsVoided,
                BillTotalPaidAfterPayment = totalPaid,
                BillBalanceAfterPayment = balance,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes,
                IsCreditApplication = true
            };
        }, cancellationToken);
    }

    private async Task<(decimal totalPaid, decimal balance)> RecomputeBillAndGetTotalsAsync(int utilityBillId, CancellationToken cancellationToken)
    {
        await utilityBillService.RecomputeStatusAsync(utilityBillId, cancellationToken);

        var refreshedBill = await dbContext.UtilityBills
            .Include(x => x.Payments)
            .FirstAsync(x => x.Id == utilityBillId, cancellationToken);

        var totalPaid = refreshedBill.Payments.Where(x => !x.IsVoided).Sum(x => x.Amount);
        var balance = refreshedBill.Amount - totalPaid;
        return (totalPaid, balance);
    }

    private async Task HandleOverpaymentCreditAsync(int utilityCustomerId, int utilityTypeId, int paymentId, decimal billAmount, decimal totalPaid, CancellationToken cancellationToken)
    {
        if (totalPaid <= billAmount)
        {
            return;
        }

        var latestBalance = await dbContext.UtilityCustomerCredits
            .Where(x => x.UtilityCustomerId == utilityCustomerId && x.UtilityTypeId == utilityTypeId)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => (decimal?)x.BalanceAfter)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var overpayment = totalPaid - billAmount;
        var newBalance = latestBalance + overpayment;

        dbContext.UtilityCustomerCredits.Add(new UtilityCustomerCredit
        {
            UtilityCustomerId = utilityCustomerId,
            UtilityTypeId = utilityTypeId,
            SourcePaymentId = paymentId,
            Amount = overpayment,
            BalanceAfter = newBalance,
            TransactionType = UtilityCreditTransactionType.OverpaymentCreated,
            OccurredAt = DateTime.UtcNow,
            Notes = "Auto-created from utility overpayment."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReversePaymentLinkedCreditsAsync(UtilityBillPayment payment, CancellationToken cancellationToken)
    {
        var bill = await dbContext.UtilityBills
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == payment.UtilityBillId, cancellationToken);

        if (bill is null)
        {
            return;
        }

        var linkedCredits = await dbContext.UtilityCustomerCredits
            .Where(x => x.SourcePaymentId == payment.Id)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(cancellationToken);

        if (linkedCredits.Count == 0)
        {
            return;
        }

        var latestBalance = await dbContext.UtilityCustomerCredits
            .Where(x => x.UtilityCustomerId == bill.UtilityCustomerId && x.UtilityTypeId == bill.UtilityTypeId)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => (decimal?)x.BalanceAfter)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var reversalAmount = -linkedCredits.Sum(x => x.Amount);
        var newBalance = latestBalance + reversalAmount;

        dbContext.UtilityCustomerCredits.Add(new UtilityCustomerCredit
        {
            UtilityCustomerId = bill.UtilityCustomerId,
            UtilityTypeId = bill.UtilityTypeId,
            SourcePaymentId = payment.Id,
            Amount = reversalAmount,
            BalanceAfter = newBalance,
            TransactionType = UtilityCreditTransactionType.ManualAdjustment,
            OccurredAt = DateTime.UtcNow,
            Notes = "Auto reversal for voided utility payment."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null ||
            string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await action();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static UtilityBillPaymentDto ToDto(UtilityBillPayment entity)
    {
        var bill = entity.UtilityBill;
        var paid = bill?.Payments.Where(x => !x.IsVoided).Sum(x => x.Amount) ?? 0m;
        var balance = bill is null ? 0m : bill.Amount - paid;
        var isCredit = string.Equals(entity.ReferenceNumber, "CREDIT-APPLIED", StringComparison.OrdinalIgnoreCase);

        return new UtilityBillPaymentDto
        {
            Id = entity.Id,
            UtilityBillId = entity.UtilityBillId,
            PaymentDate = entity.PaymentDate,
            Amount = entity.Amount,
            IsVoided = entity.IsVoided,
            BillTotalPaidAfterPayment = paid,
            BillBalanceAfterPayment = balance,
            ReferenceNumber = entity.ReferenceNumber,
            Notes = entity.Notes,
            IsCreditApplication = isCredit
        };
    }
}
