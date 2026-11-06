using System;
using System.Threading.Tasks;
using System.Data;
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
    /// Sequence read and increment run under a serializable database transaction
    /// so separate terminals cannot issue the same document number.
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
            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
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
            await transaction.CommitAsync();

            var number = $"{getPrefix(settings)}-{currentYear}-{sequence:D4}";
            _logger.LogInformation("Generated document number {Number}", number);
            return number;
        }
    }
}
