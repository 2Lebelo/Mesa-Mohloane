using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces
{
    public class PagedResultDto<T>
    {
        public List<UserDto> Items { get; internal set; }
        public int TotalCount { get; internal set; }
        public int Page { get; internal set; }
        public int PageSize { get; internal set; }
    }
}