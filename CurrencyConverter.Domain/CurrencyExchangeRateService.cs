using CurrencyConverter.Cache.Interfaces;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Configurations;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Domain;

internal class CurrencyExchangeRateService : ICurrencyExchangeRateService
{
    private readonly IExchangeRateProviderFactory _providerFactory;
    private readonly ICurrencyLatestRateCache _latestCache;
    private readonly ICurrencyHistoricalRateCache _historicalCache;
    private readonly Dictionary<string, ExchangeRateProviderOptions> _providerMetadata;
    private readonly ILogger<CurrencyExchangeRateService> _logger;

    public CurrencyExchangeRateService(
        IExchangeRateProviderFactory providerFactory,
        ICurrencyLatestRateCache latestCache,
        ICurrencyHistoricalRateCache historicalCache,
        IOptions<ExchangeRateSettings> settings,
        ILogger<CurrencyExchangeRateService> logger)
    {
        _providerFactory = providerFactory;
        _latestCache = latestCache;
        _historicalCache = historicalCache;
        _providerMetadata = settings.Value.Providers
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<LatestRateDto> GetLatestRateAsync(string providerKey, string baseCurrency)
    {
        var metadata = GetProviderMetadata(providerKey);
        var now = DateTime.UtcNow;

        var cacheEntry = await _latestCache.TryGetAsync(providerKey, baseCurrency);
        if (cacheEntry != null && now - cacheEntry.LastApiCallUtc < TimeSpan.FromSeconds(metadata.LatestRateTtlSeconds))
        {
            _logger.LogInformation("Returning cached latest rate for {Provider}/{BaseCurrency}. Cached at {Timestamp}", providerKey, baseCurrency, cacheEntry.LastApiCallUtc);
            return cacheEntry.Rate;
        }

        _logger.LogInformation("Fetching fresh latest rates for {Provider}/{BaseCurrency} from provider", providerKey, baseCurrency);

        var exchangeProvider = _providerFactory.GetProvider(providerKey);
        var fresh = await exchangeProvider.GetLatestRatesAsync(baseCurrency);
        await _latestCache.SetAsync(providerKey, baseCurrency, fresh, now);

        _logger.LogInformation("Cached fresh latest rates for {Provider}/{BaseCurrency} at {Timestamp}", providerKey, baseCurrency, now);

        return fresh;
    }

    public async Task<CurrencyConversionResult> ConvertAsync(
        string providerKey,
        string baseCurrency,
        string targetCurrency,
        decimal amount)
    {

        if(baseCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException($"Base currency {baseCurrency} and target currency {targetCurrency} cannot be the same.");
        }

        var latest = await GetLatestRateAsync(providerKey, baseCurrency);

        if (!latest.Rates.TryGetValue(targetCurrency, out var rate))
        {
            _logger.LogWarning("Target currency '{TargetCurrency}' not found in latest rates for base '{BaseCurrency}'", targetCurrency, baseCurrency);
            throw new BadRequestException($"Conversion rate for '{targetCurrency}' is not available.");
        }

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
            _logger.LogInformation("Initializing historical cache for {Provider}/{BaseCurrency}", providerKey, baseCurrency);

            cacheEntry = await InitializeCacheFromProvider(providerKey, baseCurrency, now, metadata, provider);
        }
        else
        {
            if (IsCacheStale(cacheEntry, now, metadata))
            {
                _logger.LogInformation("Syncing historical cache for {Provider}/{BaseCurrency}. Last API call was at {LastCall}", providerKey, baseCurrency, cacheEntry.LastApiCallUtc);

                await SyncCacheWithProvider(providerKey, baseCurrency, now, metadata, provider, cacheEntry);
            }

            await EvictExpiredEntriesIfNeeded(providerKey, baseCurrency, now, cacheEntry);
        }

        var (totalCount, items) = GetPaginatedData(from, to, pageNumber, pageSize, cacheEntry);

        _logger.LogInformation("Returning {Count} historical records for {Provider}/{BaseCurrency} between {From} and {To}", items.Count, providerKey, baseCurrency, from, to);


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

        _logger.LogInformation("Initialized historical cache for {Provider}/{BaseCurrency} with {Count} entries", providerKey, baseCurrency, newEntry.Rates.Count);

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

        _logger.LogInformation("Synced {Count} new historical rates into cache for {Provider}/{BaseCurrency}", fetchedRates.Count(), providerKey, baseCurrency);

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
            var toRemove = cacheEntry.Rates.Keys.TakeWhile(d => d < cutoff).ToList();

            foreach (var date in toRemove)
            {
                cacheEntry.Rates.Remove(date);
            }

            await _historicalCache.SetAsync(providerKey, baseCurrency, cacheEntry);

            _logger.LogInformation("Evicted {Count} outdated entries for {Provider}/{BaseCurrency}", toRemove.Count, providerKey, baseCurrency);

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
            throw new BadRequestException("'from' date must be before 'to' date.");

        if ((now - from).TotalDays > 180)
            throw new BadRequestException("Only historical data within the last 6 months is supported.");

        if (to > now)
            throw new BadRequestException("'to' date cannot be in the future.");
    }

    private ExchangeRateProviderOptions GetProviderMetadata(string providerKey)
    {
        if (!_providerMetadata.TryGetValue(providerKey, out var metadata))
            throw new BadRequestException($"Provider metadata not found for '{providerKey}'.");

        return metadata;
    }
}
