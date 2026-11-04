using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INumberSequenceService _numberSequenceService;
        private readonly ILogger<QuotationService> _logger;

        public QuotationService(
            IUnitOfWork unitOfWork,
            INumberSequenceService numberSequenceService,
            ILogger<QuotationService> logger)
        {
            _unitOfWork = unitOfWork;
            _numberSequenceService = numberSequenceService;
            _logger = logger;
        }

        // Both read queries use AsNoTracking: they only display data, and this
        // app shares one DbContext for the whole session.
        public async Task<IReadOnlyList<Quotation>> GetAllAsync() =>
            await _unitOfWork.Quotations.Query()
                .AsNoTracking()
                .Include(q => q.Customer)
                .Include(q => q.Items)
                .OrderByDescending(q => q.QuotationDate)
                .ThenByDescending(q => q.Id)
                .ToListAsync();

        public async Task<Quotation?> GetWithDetailsAsync(int quotationId) =>
            await _unitOfWork.Quotations.Query()
                .AsNoTracking()
                .Include(q => q.Customer)
                .Include(q => q.Items).ThenInclude(i => i.Material)
                .Include(q => q.ConvertedToJob)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

        public async Task<Quotation> CreateAsync(Quotation quotation, IEnumerable<QuotationItem> items)
        {
            if (quotation.ExpiryDate.HasValue && quotation.ExpiryDate.Value.Date < DateTime.Today)
            {
                throw new InvalidQuotationException("The expiry date cannot be in the past.");
            }

            // Rebuild the items as clean copies carrying only plain values and
            // the MaterialId. The caller's items may have a Material navigation
            // object attached purely for on-screen display; adding THAT to the
            // context would make EF Core try to INSERT a duplicate Material row.
            quotation.Items = items
                .Select(i => new QuotationItem
                {
                    MaterialId = i.MaterialId,
                    Description = i.Description,
                    EstimatedWeightKg = i.EstimatedWeightKg,
                    EstimatedRatePerKg = i.EstimatedRatePerKg,
                    EstimatedAmount = i.EstimatedAmount
                })
                .ToList();

            quotation.QuotationNumber = await _numberSequenceService.GenerateQuotationNumberAsync();
            quotation.Status = QuotationStatus.Draft;

            await _unitOfWork.Quotations.AddAsync(quotation);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Quotation {QuotationNumber} created for Customer {CustomerId} with {ItemCount} items",
                quotation.QuotationNumber, quotation.CustomerId, quotation.Items.Count);

            return quotation;
        }

        public async Task MarkAsSentAsync(int quotationId)
        {
            var quotation = await _unitOfWork.Quotations.GetByIdAsync(quotationId)
                ?? throw new KeyNotFoundException($"Quotation {quotationId} not found.");

            if (quotation.Status == QuotationStatus.Converted)
            {
                throw new InvalidQuotationException(
                    $"Quotation {quotation.QuotationNumber} has already been converted to a job.");
            }

            // The entity is tracked (GetByIdAsync), so change detection picks
            // this up on SaveChanges — no explicit Update() call needed.
            quotation.Status = QuotationStatus.Sent;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Quotation {QuotationNumber} marked as sent", quotation.QuotationNumber);
        }
    }
}
