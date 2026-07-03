using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Results.Support;

namespace ModularityKit.Mutator.Benchmarks.Results;

/// <summary>
/// Benchmarks the cost of creating mutation results with and without side effects.
/// </summary>
[BenchmarkCategory("Results")]
[MemoryDiagnoser]
[InProcess]
public class MutationResultCreationBenchmarks
{
    private ResultsBenchmarkSupport.ResultBenchmarkState _state = null!;
    private ChangeSet _changes = null!;

    /// <summary>
    /// Prepares the shared state and change set used by the result creation cases.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _state = new ResultsBenchmarkSupport.ResultBenchmarkState(0, 42);
        _changes = ResultsBenchmarkSupport.CreateChangeSet(_state.Revision, 2);
    }

    /// <summary>
    /// Measures creation of a successful mutation result with no side effects.
    /// </summary>
    [Benchmark(Baseline = true)]
    public MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState> Success_NoSideEffects()
        => MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState>.Success(
            _state with
            {
                Revision = _state.Revision + 1,
                Value = _state.Value + 1
            },
            _changes);

    /// <summary>
    /// Measures creation of a successful mutation result with one side effect.
    /// </summary>
    [Benchmark]
    public MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState> Success_SingleSideEffect()
    {
        var sideEffect = SideEffect.Create(
            "ResultMaterialization",
            "Single side effect",
            new ResultsBenchmarkSupport.SideEffectPayload(1, "single"),
            SideEffectSeverity.Info);

        return MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState>.Success(
            _state with
            {
                Revision = _state.Revision + 1,
                Value = _state.Value + 1
            },
            _changes,
            [sideEffect]);
    }

    /// <summary>
    /// Measures creation of a successful mutation result with several side effects.
    /// </summary>
    [Benchmark]
    public MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState> Success_MultipleSideEffects()
        => MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState>.Success(
            _state with
            {
                Revision = _state.Revision + 1,
                Value = _state.Value + 1
            },
            _changes,
            ResultsBenchmarkSupport.CreateSideEffects(4));
}
