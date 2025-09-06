using System;
using System.Collections.Generic;

namespace InteliRoute.DAL.Entities
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
