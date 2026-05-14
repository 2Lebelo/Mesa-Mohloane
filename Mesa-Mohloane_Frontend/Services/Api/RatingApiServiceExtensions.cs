using Mesa_Mohloane_Frontend.Dtos;

namespace Mesa_Mohloane_Frontend.Services.Api;

public static class RatingApiServiceExtensions
{
    public static Task<ContractorRatingDto?> GetByAssignmentAsync(
        this IRatingApiService ratings,
        Guid assignmentId)
    {
        if (ratings is RatingApiService concrete)
            return concrete.GetByAssignmentAsync(assignmentId);

        throw new NotSupportedException("Rating service does not support assignment lookup.");
    }
}
