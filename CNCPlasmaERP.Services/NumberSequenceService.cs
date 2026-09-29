using System;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// Generates document numbers in the form "{Prefix}-{Year}-{Sequence:D4}",
    /// e.g. "INV-2026-0001". The sequence counters live on the single AppSettings
    /// row and reset to 1 whenever the calendar year changes.
    ///
    /// NOTE — concurrency: this reads-then-writes AppSettings without a database
    /// row lock. That's safe for a single-PC deployment (today's setup). Once this
    /// system is shared across multiple terminals against one SQL Server, two
    /// terminals generating a number in the same instant could theoretically get
    /// the same sequence value. If that happens in practice, harden this with a
    /// raw SQL atomic "UPDATE ... OUTPUT" statement or an application lock
    /// (sp_getapplock) instead of the read-then-save pattern used here.
    /// </summary>
    public class NumberSequenceService : INumberSequenceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NumberSequenceService> _logger;

        public NumberSequenceService(IUnitOfWork unitOfWork, ILogger<NumberSequenceService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<string> GenerateJobNumberAsync() =>
            GenerateAsync(
                s => s.JobPrefix,
                s => s.CurrentJobSequence,
                (s, v) => s.CurrentJobSequence = v,
                s => s.JobSequenceYear,
                (s, v) => s.JobSequenceYear = v);

        public Task<string> GenerateInvoiceNumberAsync() =>
            GenerateAsync(
                s => s.InvoicePrefix,
                s => s.CurrentInvoiceSequence,
                (s, v) => s.CurrentInvoiceSequence = v,
                s => s.InvoiceSequenceYear,
                (s, v) => s.InvoiceSequenceYear = v);

        public Task<string> GenerateQuotationNumberAsync() =>
            GenerateAsync(
                s => s.QuotationPrefix,
                s => s.CurrentQuotationSequence,
                (s, v) => s.CurrentQuotationSequence = v,
                s => s.QuotationSequenceYear,
                (s, v) => s.QuotationSequenceYear = v);

        private async Task<string> GenerateAsync(
            Func<AppSettings, string> getPrefix,
            Func<AppSettings, int> getSequence,
            Action<AppSettings, int> setSequence,
            Func<AppSettings, int> getYear,
            Action<AppSettings, int> setYear)
        {
            var settings = await _unitOfWork.Settings.GetSettingsAsync();
            var currentYear = DateTime.Now.Year;

            var sequence = getSequence(settings);
            var storedYear = getYear(settings);

            if (storedYear != currentYear)
            {
                sequence = 0;
                setYear(settings, currentYear);
            }

            sequence += 1;
            setSequence(settings, sequence);

            await _unitOfWork.Settings.SaveAsync(settings);

            var number = $"{getPrefix(settings)}-{currentYear}-{sequence:D4}";
            _logger.LogInformation("Generated document number {Number}", number);
            return number;
        }
    }
}
