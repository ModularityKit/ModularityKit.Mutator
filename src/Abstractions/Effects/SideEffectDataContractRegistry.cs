using System.Collections.Concurrent;

namespace ModularityKit.Mutator.Abstractions.Effects;

/// <summary>
/// Stores registrations that map side effect payload contracts to CLR types.
/// Registered types allow serializers and integration layers to rehydrate typed payloads
/// from stable contract identifiers instead of inferring payload shape at runtime.
/// </summary>
public static class SideEffectDataContractRegistry
{
    private static readonly ConcurrentDictionary<(string ContractType, int ContractVersion), Type> TypesByContract = new();

    /// <summary>
    /// Registers typed side effect payload contract.
    /// </summary>
    /// <typeparam name="TData">The payload type to register.</typeparam>
    public static void Register<TData>()
        => Register(typeof(TData));

    /// <summary>
    /// Registers typed side effect payload contract.
    /// </summary>
    /// <param name="dataType">The payload type to register.</param>
    public static void Register(Type dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        var contract = GetRequiredContract(dataType);
        TypesByContract[(contract.ContractType, contract.ContractVersion)] = dataType;
    }

    /// <summary>
    /// Attempts to resolve payload CLR type from side effect data contract.
    /// </summary>
    /// <param name="contractType">The stable contract identifier.</param>
    /// <param name="contractVersion">The contract version.</param>
    /// <param name="dataType">The resolved CLR type when present.</param>
    /// <returns><see langword="true"/> when the contract is registered; otherwise <see langword="false"/>.</returns>
    public static bool TryResolve(string contractType, int contractVersion, out Type? dataType)
    {
        if (string.IsNullOrWhiteSpace(contractType) || contractVersion <= 0)
        {
            dataType = null;
            return false;
        }

        return TypesByContract.TryGetValue((contractType, contractVersion), out dataType);
    }

    /// <summary>
    /// Reads the declared side effect data contract for CLR type.
    /// </summary>
    /// <typeparam name="TData">The payload type.</typeparam>
    /// <returns>The declared side effect data contract.</returns>
    public static SideEffectDataContractAttribute GetRequiredContract<TData>()
        => GetRequiredContract(typeof(TData));

    /// <summary>
    /// Reads the declared side effect data contract for CLR type.
    /// </summary>
    /// <param name="dataType">The payload type.</param>
    /// <returns>The declared side effect data contract.</returns>
    public static SideEffectDataContractAttribute GetRequiredContract(Type dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        return dataType.GetCustomAttributes(typeof(SideEffectDataContractAttribute), inherit: false)
            .OfType<SideEffectDataContractAttribute>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"Typed side effect payload '{dataType.FullName}' must declare {nameof(SideEffectDataContractAttribute)}.");
    }
}
