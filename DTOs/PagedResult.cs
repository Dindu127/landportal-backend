// DTOs/PagedResult.cs
using System.Collections.Generic;

namespace LandPortal.Api.DTOs
{
    public class PagedResult<T>
    {
        public long Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Items { get; set; } = new List<T>();
    }
}
