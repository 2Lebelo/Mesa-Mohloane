using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IInvoiceService
{
    // Contractor operations
    Task<ServiceResult<InvoiceDto>> SubmitAsync(Guid contractorId, InvoiceCreateDto dto);

    // Admin / Auditor operations
    Task<ServiceResult<InvoiceDto>> ValidateAsync(Guid invoiceId, Guid adminId, string? remarks);
    Task<ServiceResult<InvoiceDto>> ApproveAsync(Guid invoiceId, Guid adminId);
    Task<ServiceResult<InvoiceDto>> RejectAsync(Guid invoiceId, Guid adminId, string remarks);

    // Citizen operations
    Task<ServiceResult<InvoiceDto>> AcknowledgeAsync(Guid invoiceId, Guid citizenId);

    // Queries
    Task<ServiceResult<InvoiceDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<InvoiceDto>> GetByAssignmentAsync(Guid assignmentId);
    Task<PagedResultDto<InvoiceListDto>> GetByContractorAsync(Guid contractorId, int page, int pageSize);
    Task<PagedResultDto<InvoiceListDto>> GetFlaggedAsync(int page, int pageSize);
}