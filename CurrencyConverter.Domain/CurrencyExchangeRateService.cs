using CurrencyConverter.Cache.Interfaces;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Configurations;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Domain;

internal class CurrencyExchangeRateService : ICurrencyExchangeRateService
{
    private readonly IExchangeRateProviderFactory _providerFactory;
    private readonly ICurrencyLatestRateCache _latestCache;
    private readonly ICurrencyHistoricalRateCache _historicalCache;
    private readonly Dictionary<string, ExchangeRateProviderOptions> _providerMetadata;

    public CurrencyExchangeRateService(
        IExchangeRateProviderFactory providerFactory,
        ICurrencyLatestRateCache latestCache,
        ICurrencyHistoricalRateCache historicalCache,
        IOptions<ExchangeRateSettings> settings)
    {
        _providerFactory = providerFactory;
        _latestCache = latestCache;
        _historicalCache = historicalCache;
        _providerMetadata = settings.Value.Providers
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<LatestRateDto> GetLatestRateAsync(string providerKey, string baseCurrency)
    {
        var metadata = GetProviderMetadata(providerKey);
        var now = DateTime.UtcNow;

        var cacheEntry = await _latestCache.TryGetAsync(providerKey, baseCurrency);
        if (cacheEntry != null && now - cacheEntry.LastApiCallUtc < TimeSpan.FromSeconds(metadata.LatestRateTtlSeconds))
        {
            return cacheEntry.Rate;
        }

        var exchangeProvider = _providerFactory.GetProvider(providerKey);
        var fresh = await exchangeProvider.GetLatestRatesAsync(baseCurrency);
        await _latestCache.SetAsync(providerKey, baseCurrency, fresh, now);

        return fresh;
    }

    public async Task<CurrencyConversionResult> ConvertAsync(
        string providerKey,
        string baseCurrency,
        string targetCurrency,
        decimal amount)
    {

        var latest = await GetLatestRateAsync(providerKey, baseCurrency);

        if (!latest.Rates.TryGetValue(targetCurrency, out var rate))
            throw new InvalidOperationException($"Rate for '{targetCurrency}' not available.");

        return new CurrencyConversionResult
        {
            BaseCurrency = baseCurrency,
            TargetCurrency = targetCurrency,
            Amount = amount,
            ConvertedAmount = amount * rate,
            RateTimestamp = latest.Timestamp
        };
    }
    public async Task<PagedResult<HistoricalRateDto>> GetHistoricalRatesAsync(
     string providerKey,
     string baseCurrency,
     DateTime from,
     DateTime to,
     int pageNumber,
     int pageSize)
    {
        var now = DateTime.UtcNow;

        ValidateParams(from, to, now);

        var metadata = GetProviderMetadata(providerKey);

        var provider = _providerFactory.GetProvider(providerKey);

        var cacheEntry = await _historicalCache.GetAllAsync(providerKey, baseCurrency);

        if (cacheEntry == null || cacheEntry.Rates.Count == 0)
        {
            cacheEntry = await InitializeCacheFromProvider(providerKey, baseCurrency, now, metadata, provider);
        }
        else
        {
            if (IsCacheStale(cacheEntry, now, metadata))
            {
                await SyncCacheWithProvider(providerKey, baseCurrency, now, metadata, provider, cacheEntry);
            }

            await EvictExpiredEntriesIfNeeded(providerKey, baseCurrency, now, cacheEntry);
        }

        var (totalCount, items) = GetPaginatedData(from, to, pageNumber, pageSize, cacheEntry);

        return new PagedResult<HistoricalRateDto>
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }


    private async Task<HistoricalRateCacheEntry> InitializeCacheFromProvider(
    string providerKey,
    string baseCurrency,
    DateTime now,
    ExchangeRateProviderOptions metadata,
    IExchangeRateProvider provider)
    {
        var fromDate = now.AddDays(-180);
        var fetchedRates = await provider.GetRatesInRangeAsync(baseCurrency, fromDate, now);

        var newEntry = new HistoricalRateCacheEntry
        {
            LastApiCallUtc = now
        };

        foreach (var rate in fetchedRates)
        {
            newEntry.Rates[rate.Date] = rate;
        }

        await _historicalCache.SetAsync(providerKey, baseCurrency, newEntry);
        return newEntry;
    }

    private static bool IsCacheStale(HistoricalRateCacheEntry cacheEntry, DateTime now, ExchangeRateProviderOptions metadata)
    {
        return (now - cacheEntry.LastApiCallUtc) >= metadata.UpdateInterval;
    }

    private async Task SyncCacheWithProvider(
        string providerKey,
        string baseCurrency,
        DateTime now,
        ExchangeRateProviderOptions metadata,
        IExchangeRateProvider provider,
        HistoricalRateCacheEntry cacheEntry)
    {
        var lastCached = cacheEntry.Rates.Keys.Any()
            ? cacheEntry.Rates.Keys.Max().Add(metadata.UpdateInterval)
            : now.AddDays(-180);

        var fetchedRates = await provider.GetRatesInRangeAsync(baseCurrency, lastCached, now);
        foreach (var rate in fetchedRates)
        {
            cacheEntry.Rates[rate.Date] = rate;
        }

        cacheEntry.LastApiCallUtc = now;
        await _historicalCache.SetAsync(providerKey, baseCurrency, cacheEntry);
    }

    private async Task EvictExpiredEntriesIfNeeded(
        string providerKey,
        string baseCurrency,
        DateTime now,
        HistoricalRateCacheEntry cacheEntry)
    {
        var cutoff = now.Date.AddDays(-180);

        var oldestDate = cacheEntry.Rates.Keys.FirstOrDefault();

        if (oldestDate < cutoff)
        {
            foreach (var date in cacheEntry.Rates.Keys.TakeWhile(d => d < cutoff).ToList())
            {
                cacheEntry.Rates.Remove(date);
            }

            await _historicalCache.SetAsync(providerKey, baseCurrency, cacheEntry);
        }
    }

    private static (int TotalCount, List<HistoricalRateDto> Items) GetPaginatedData(
        DateTime from,
        DateTime to,
        int pageNumber,
        int pageSize,
        HistoricalRateCacheEntry cacheEntry)
    {
        var filtered = cacheEntry.Rates
            .Where(entry => entry.Key >= from && entry.Key <= to)
            .OrderBy(entry => entry.Key)
            .ToList();

        var pagedItems = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => entry.Value)
            .ToList();

        return (filtered.Count, pagedItems);
    }

    private static void ValidateParams(DateTime from, DateTime to, DateTime now)
    {
        if (from > to)
            throw new ArgumentException("'from' date must be before 'to' date.");

        if ((now - from).TotalDays > 180)
            throw new ArgumentException("Only historical data within the last 6 months is supported.");

        if (to > now)
            throw new ArgumentException("'to' date cannot be in the future.");
    }

    private ExchangeRateProviderOptions GetProviderMetadata(string providerKey)
    {
        if (!_providerMetadata.TryGetValue(providerKey, out var metadata))
            throw new NotSupportedException($"Provider metadata not found for '{providerKey}'.");

        return metadata;
    }
}
