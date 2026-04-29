using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly MesaMohloaneDbContext _context;

    public AssignmentRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<Assignment?> GetByIdAsync(Guid id)
        => await _context.Assignments
            .Include(a => a.Incident)
            .Include(a => a.TenderApplication).ThenInclude(t => t!.LineItems)
            .Include(a => a.Contractor)
            .Include(a => a.WorkCompletion)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

    public async Task<Assignment?> GetByIncidentAsync(Guid incidentId)
        => await _context.Assignments
            .Include(a => a.Contractor)
            .Include(a => a.TenderApplication)
            .Include(a => a.WorkCompletion)
            .FirstOrDefaultAsync(a => a.IncidentId == incidentId && !a.IsDeleted);

    public async Task<Assignment?> GetByContractorAsync(Guid contractorId)
        => await _context.Assignments
            .Include(a => a.Incident)
            .FirstOrDefaultAsync(a => a.ContractorId == contractorId && !a.IsDeleted);

    public async Task<Guid> CreateAsync(Assignment assignment)
    {
        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment.Id;
    }

    public async Task UpdateAsync(Assignment assignment)
    {
        _context.Assignments.Update(assignment);
        await _context.SaveChangesAsync();
    }

    public async Task<WorkCompletion?> GetWorkCompletionAsync(Guid assignmentId)
        => await _context.WorkCompletions
            .FirstOrDefaultAsync(w => w.AssignmentId == assignmentId && !w.IsDeleted);

    public async Task<Guid> CreateWorkCompletionAsync(WorkCompletion workCompletion)
    {
        _context.WorkCompletions.Add(workCompletion);
        await _context.SaveChangesAsync();
        return workCompletion.Id;
    }
}