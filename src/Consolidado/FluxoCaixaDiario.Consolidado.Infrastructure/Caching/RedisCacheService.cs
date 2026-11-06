using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using StackExchange.Redis;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Application.Metrics;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    ConsolidadoMetrics metrics,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddConcurrencyLimiter(permitLimit: 20, queueLimit: 10)
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<RedisTimeoutException>()
        })
        .AddTimeout(TimeSpan.FromSeconds(2))
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<TimeoutRejectedException>()
        })
        .Build();

    public async Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default) where T : class
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                var json = await _db.StringGetAsync(chave);
                if (json.IsNullOrEmpty)
                {
                    metrics.CacheMisses.Add(1);
                    return null;
                }
                metrics.CacheHits.Add(1);
                return JsonSerializer.Deserialize<T>(json.ToString()!);
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit breaker aberto para Redis. Continuando sem cache (chave: {Chave}).", chave);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao obter cache para chave {Chave}. Continuando sem cache.", chave);
            return null;
        }
    }

    public async Task DefinirAsync<T>(string chave, T valor, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        try
        {
            await _pipeline.ExecuteAsync(async token =>
            {
                var json = JsonSerializer.Serialize(valor);
                await _db.StringSetAsync(chave, json, ttl);
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit breaker aberto para Redis. Cache não atualizado (chave: {Chave}).", chave);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao definir cache para chave {Chave}. Continuando sem cache.", chave);
        }
    }

    public async Task RemoverAsync(string chave, CancellationToken ct = default)
    {
        try
        {
            await _pipeline.ExecuteAsync(async token =>
            {
                await _db.KeyDeleteAsync(chave);
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit breaker aberto para Redis. Remoção de cache ignorada (chave: {Chave}).", chave);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover cache para chave {Chave}.", chave);
        }
    }
}