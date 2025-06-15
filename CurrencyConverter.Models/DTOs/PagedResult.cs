﻿namespace CurrencyConverter.Models.DTOs
{
    public record PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = [];
    }
}
