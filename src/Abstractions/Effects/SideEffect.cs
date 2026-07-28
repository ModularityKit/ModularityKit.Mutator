using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ModularityKit.Mutator.Abstractions.Effects;

/// <summary>
/// Represents a side effect produced by a mutation.
/// Side effects capture additional consequences that are not part of the primary state change.
/// </summary>
[JsonConverter(typeof(SideEffectJsonConverter))]
public sealed class SideEffect
{
    /// <summary>
    /// The type of the side effect (e.g., "Notification", "AuditLog", "ExternalCall").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the side effect.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Severity of the side effect.
    /// Determines the criticality or importance for monitoring and alerting.
    /// </summary>
    public SideEffectSeverity Severity { get; init; } = SideEffectSeverity.Info;

    /// <summary>
    /// Optional data associated with the side effect.
    /// Can hold structured information for logging, auditing, or downstream processing.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Stable contract identifier for typed side effect payloads.
    /// </summary>
    public string? DataContractType { get; init; }

    /// <summary>
    /// Version number for typed side effect payloads.
    /// </summary>
    public int? DataContractVersion { get; init; }

    /// <summary>
    /// Timestamp when the side effect occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates whether this side effect requires an explicit action or intervention.
    /// </summary>
    public bool RequiresAction { get; init; }

    /// <summary>
    /// Creates a new <see cref="SideEffect"/> with the specified properties.
    /// </summary>
    /// <param name="type">The type of the side effect.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="data">
    /// Optional associated data. When the payload type declares <see cref="SideEffectDataContractAttribute"/>,
    /// the side effect contract metadata is populated automatically.
    /// </param>
    /// <param name="severity">Severity level.</param>
    /// <param name="requiresAction">
    /// Indicates whether the side effect requires explicit follow-up. Critical severity always implies action.
    /// </param>
    /// <param name="timestamp">Optional timestamp override. Defaults to current UTC time.</param>
    public static SideEffect Create(
        string type,
        string description,
        object? data = null,
        SideEffectSeverity severity = SideEffectSeverity.Info,
        bool requiresAction = false,
        DateTimeOffset? timestamp = null)
        => CreateCore(
            type,
            description,
            data,
            severity,
            requiresAction,
            timestamp);

    /// <summary>
    /// Creates a new <see cref="SideEffect"/> with a typed payload contract.
    /// </summary>
    /// <typeparam name="TData">The payload type.</typeparam>
    /// <param name="type">The type of the side effect.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="data">Typed associated payload.</param>
    /// <param name="severity">Severity level.</param>
    /// <param name="requiresAction">
    /// Indicates whether the side effect requires explicit follow-up. Critical severity always implies action.
    /// </param>
    /// <param name="timestamp">Optional timestamp override. Defaults to current UTC time.</param>
    public static SideEffect Create<TData>(
        string type,
        string description,
        TData data,
        SideEffectSeverity severity = SideEffectSeverity.Info,
        bool requiresAction = false,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        return CreateCore(type, description, data, severity, requiresAction, timestamp);
    }

    /// <summary>
    /// Creates a new critical <see cref="SideEffect"/> instance.
    /// </summary>
    /// <param name="type">The type of the side effect.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="data">
    /// Optional associated data. When the payload type declares <see cref="SideEffectDataContractAttribute"/>,
    /// the side effect contract metadata is populated automatically.
    /// </param>
    /// <param name="timestamp">Optional timestamp override. Defaults to current UTC time.</param>
    public static SideEffect Critical(
        string type,
        string description,
        object? data = null,
        DateTimeOffset? timestamp = null)
        => Create(
            type,
            description,
            data,
            SideEffectSeverity.Critical,
            requiresAction: true,
            timestamp: timestamp);

    /// <summary>
    /// Creates a new critical <see cref="SideEffect"/> instance with a typed payload contract.
    /// </summary>
    /// <typeparam name="TData">The payload type.</typeparam>
    /// <param name="type">The type of the side effect.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="data">Typed associated payload.</param>
    /// <param name="timestamp">Optional timestamp override. Defaults to current UTC time.</param>
    public static SideEffect Critical<TData>(
        string type,
        string description,
        TData data,
        DateTimeOffset? timestamp = null)
        => Create(
            type,
            description,
            data,
            SideEffectSeverity.Critical,
            requiresAction: true,
            timestamp: timestamp);

    /// <summary>
    /// Attempts to read the side effect payload as a typed contract.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="data">The typed payload when available.</param>
    /// <returns><see langword="true"/> when the payload is available as <typeparamref name="TData"/>.</returns>
    public bool TryGetData<TData>(out TData? data)
    {
        if (Data is TData typed)
        {
            data = typed;
            return true;
        }

        data = default;
        return false;
    }

    private static SideEffect CreateCore(
        string type,
        string description,
        object? data,
        SideEffectSeverity severity,
        bool requiresAction,
        DateTimeOffset? timestamp)
    {
        var (contractType, contractVersion) = ResolveContract(data);

        return new SideEffect
        {
            Type = type,
            Description = description,
            Data = data,
            Severity = severity,
            RequiresAction = requiresAction || severity == SideEffectSeverity.Critical,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            DataContractType = contractType,
            DataContractVersion = contractVersion
        };
    }

    private static readonly ConcurrentDictionary<Type, (string? ContractType, int? ContractVersion)> _contractCache = new();

    private static (string? ContractType, int? ContractVersion) ResolveContract(object? data)
    {
        if (data is null)
            return (null, null);

        var dataType = data.GetType();
        if (_contractCache.TryGetValue(dataType, out var cached))
            return cached;

        var contract = dataType.GetCustomAttributes(typeof(SideEffectDataContractAttribute), inherit: false)
            .OfType<SideEffectDataContractAttribute>()
            .SingleOrDefault();

        (string?, int?) result;
        if (contract is null)
        {
            result = (null, null);
        }
        else
        {
            SideEffectDataContractRegistry.Register(dataType);
            result = (contract.ContractType, contract.ContractVersion);
        }

        _contractCache.TryAdd(dataType, result);
        return result;
    }
}
