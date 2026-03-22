using System;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankInsight.API.Services;

public interface ISequenceGeneratorService
{
    Task<int> GetNextSequenceAsync(string prefix);
    int CalculateLuhnCheckDigit(string number);
}

public class SequenceGeneratorService : ISequenceGeneratorService
{
    private readonly ApplicationDbContext _context;

    public SequenceGeneratorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetNextSequenceAsync(string prefix)
    {
        var currentTransaction = _context.Database.CurrentTransaction;
        var ownsTransaction = currentTransaction == null;
        IDbContextTransaction? transaction = currentTransaction;

        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        try
        {
            var sequence = await _context.NumberSequences
                .FirstOrDefaultAsync(s => s.Id == prefix);

            if (sequence == null)
            {
                sequence = new NumberSequence
                {
                    Id = prefix,
                    NextValue = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.NumberSequences.Add(sequence);
            }

            var currentValue = sequence.NextValue;
            sequence.NextValue++;
            sequence.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (ownsTransaction && transaction != null)
            {
                await transaction.CommitAsync();
                await transaction.DisposeAsync();
            }

            return currentValue;
        }
        catch
        {
            if (ownsTransaction && transaction != null)
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }

            throw;
        }
    }

    public int CalculateLuhnCheckDigit(string number)
    {
        int sum = 0;
        bool alternate = true;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(number.Substring(i, 1));
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n = (n % 10) + 1;
                }
            }
            sum += n;
            alternate = !alternate;
        }
        return (10 - (sum % 10)) % 10;
    }
}
