namespace ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;

/// <summary>
/// Raised when approval requirements are configured in a way the governance runtime cannot execute safely.
/// </summary>
public sealed class InvalidMutationApprovalConfigurationException(string message) : InvalidOperationException(message);
