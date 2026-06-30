namespace ModularityKit.Mutator.Abstractions.Effects;

/// <summary>
/// Declares stable contract identifier for typed side effect payloads.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SideEffectDataContractAttribute(string contractType, int contractVersion = 1) : Attribute
{
    /// <summary>
    /// Stable contract identifier for the payload.
    /// </summary>
    public string ContractType { get; } = string.IsNullOrWhiteSpace(contractType)
        ? throw new ArgumentException("Contract type is required.", nameof(contractType))
        : contractType;

    /// <summary>
    /// Version number for the payload contract.
    /// </summary>
    public int ContractVersion { get; } = contractVersion > 0
        ? contractVersion
        : throw new ArgumentOutOfRangeException(nameof(contractVersion), "Contract version must be greater than zero.");
}
