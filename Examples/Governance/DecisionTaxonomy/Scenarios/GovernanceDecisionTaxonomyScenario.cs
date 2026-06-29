using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace DecisionTaxonomy.Scenarios;

internal static class GovernanceDecisionTaxonomyScenario
{
    public static void Run()
    {
        PrintSection("Lifecycle Decisions");
      
        PrintDecision(MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Submitted,
            MutationContext.User("requester", "Requester", "Submit request"),
            reason: "Request was submitted into governance."));
     
        PrintDecision(MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Approved,
            MutationContext.User("system", "System", "Request reached executable state"),
            reason: "Request is now approved for execution."));


        PrintSection("Approval Decisions");
     
        PrintDecision(MutationRequestDecision.Approval(
            MutationRequestApprovalDecisionType.Requested,
            MutationContext.User("requester", "Requester", "Approval needed for sensitive change"),
            reason: "Sensitive change requires explicit sign-off."));

        PrintDecision(MutationRequestDecision.Approval(
            MutationRequestApprovalDecisionType.Granted,
            MutationContext.User("alice", "Alice", "Manager approved"),
            reason: "Manager granted the required approval."));
     
        PrintDecision(MutationRequestDecision.Approval(
            MutationRequestApprovalDecisionType.Rejected,
            MutationContext.User("bob", "Bob", "Security rejected"),
            reason: "Security review rejected the request."));

        PrintSection("Version Resolution Decisions");
       
        PrintDecision(MutationRequestDecision.VersionResolution(
            MutationRequestVersionResolutionDecisionType.Validated,
            MutationContext.User("approver", "Approver", "Version still matches"),
            reason: "Current state version still matches the approved request."));
      
        PrintDecision(MutationRequestDecision.VersionResolution(
            MutationRequestVersionResolutionDecisionType.RejectedAsStale,
            MutationContext.User("approver", "Approver", "State drift invalidated the request"),
            reason: "Request was rejected because the approved version is stale."));
  
        PrintDecision(MutationRequestDecision.VersionResolution(
            MutationRequestVersionResolutionDecisionType.RenewedApprovalRequired,
            MutationContext.User("approver", "Approver", "Re-approval required on latest state"),
            reason: "Request must be approved again on the latest state version."));
    }

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    private static void PrintDecision(MutationRequestDecision decision)
    {
        Console.WriteLine($"Category: {decision.Type.Category}");
        Console.WriteLine($"Code: {decision.Type.Code}");
        Console.WriteLine($"Display: {decision.Type}");
        Console.WriteLine($"Actor: {decision.Context.ActorId ?? "system"}");
        Console.WriteLine($"Reason: {decision.Reason ?? "-"}");
        Console.WriteLine();
    }
}