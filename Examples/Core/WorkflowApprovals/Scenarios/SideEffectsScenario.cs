using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using System.Text.Json;
using WorkflowApprovals.Contracts;
using WorkflowApprovals.Mutations;
using WorkflowApprovals.State;

namespace WorkflowApprovals.Scenarios;

internal static class SideEffectsScenario
{
    internal static async Task Run(IMutationEngine engine)
    {
        Console.WriteLine("\n=== Side Effects Scenario ===");

        SideEffectDataContractRegistry.Register<WorkflowStartedSideEffectData>();
        SideEffectDataContractRegistry.Register<WorkflowRejectedSideEffectData>();

        var state = new ApprovalWorkflowState();

        var startContext = MutationContext.System("Start side effect demo", correlationId: "workflow-side-effects");
        var start = new StartApprovalMutation("initiator", ["SecurityReview", "FinanceReview"], startContext);
        var startResult = await engine.ExecuteAsync(start, state);

        if (!startResult.IsSuccess || startResult.NewState == null)
        {
            Console.WriteLine("✗ Failed to start workflow.");
            return;
        }

        PrintSideEffects("Start workflow", startResult.SideEffects);

        state = startResult.NewState;

        var rejectContext = MutationContext.User("security.lead", reason: "Reject risky request");
        var reject = new RejectWorkflowMutation("security.lead", rejectContext);
        var rejectResult = await engine.ExecuteAsync(reject, state);

        if (!rejectResult.IsSuccess || rejectResult.NewState == null)
        {
            Console.WriteLine("✗ Failed to reject workflow.");
            return;
        }

        PrintSideEffects("Reject workflow", rejectResult.SideEffects);
    }

    private static void PrintSideEffects(string operation, IReadOnlyList<SideEffect> sideEffects)
    {
        Console.WriteLine($"{operation} side effects:");

        foreach (var effect in sideEffects)
        {
            Console.WriteLine(
                $"  {effect.Type} | severity={effect.Severity} | requiresAction={effect.RequiresAction}");
            Console.WriteLine($"    {effect.Description}");

            var roundtrip = JsonSerializer.Deserialize<SideEffect>(JsonSerializer.Serialize(effect));

            if (roundtrip?.TryGetData<WorkflowStartedSideEffectData>(out var started) == true)
            {
                Console.WriteLine(
                    $"    contract={roundtrip.DataContractType}@v{roundtrip.DataContractVersion} | initiator={started!.Initiator} | workflowId={started.WorkflowId}");
                continue;
            }

            if (roundtrip?.TryGetData<WorkflowRejectedSideEffectData>(out var rejected) == true)
            {
                Console.WriteLine(
                    $"    contract={roundtrip.DataContractType}@v{roundtrip.DataContractVersion} | rejector={rejected!.Rejector} | state={rejected.State}");
                continue;
            }

            if (roundtrip?.Data is not null)
            {
                Console.WriteLine($"    data={roundtrip.Data}");
            }
        }
    }
}
