using System.Diagnostics;
using System.Text;

namespace ModularityKit.Mutator.Examples.SmokeTests.Support;

internal static class ExampleSmokeRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(45);
    private const string TargetFramework = "net10.0";
    private static readonly Lock BuildSync = new();
    private static readonly HashSet<string> BuiltProjects = [];

    public static async Task RunAndAssertAsync(ExampleSmokeCase example, CancellationToken cancellationToken = default)
    {
        await EnsureBuiltAsync(example.ProjectPath, cancellationToken).ConfigureAwait(false);
        var result = await RunAsync(example, cancellationToken).ConfigureAwait(false);
        var validationError = example.Validate(result);

        Assert.True(
            validationError is null,
            $"{example.Name} smoke check failed: {validationError}{Environment.NewLine}{FormatResult(result)}");
    }

    private static async Task<ExampleRunResult> RunAsync(ExampleSmokeCase example, CancellationToken cancellationToken)
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var projectDirectory = Path.GetDirectoryName(Path.Combine(repositoryRoot, example.ProjectPath))
            ?? throw new DirectoryNotFoundException($"Could not resolve project directory for '{example.ProjectPath}'.");
        var assemblyName = Path.GetFileNameWithoutExtension(example.ProjectPath);
        var assemblyPath = Path.Combine(projectDirectory, "bin", "Release", TargetFramework, $"{assemblyName}.dll");

        return await RunProcessAsync(
            repositoryRoot,
            arguments =>
            {
                arguments.Add("exec");
                arguments.Add(assemblyPath);
            },
            example.EnvironmentVariables,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureBuiltAsync(string projectPath, CancellationToken cancellationToken)
    {
        lock (BuildSync)
        {
            if (BuiltProjects.Contains(projectPath))
                return;
        }

        var repositoryRoot = ResolveRepositoryRoot();
        var buildResult = await RunProcessAsync(
            repositoryRoot,
            arguments =>
            {
                arguments.Add("build");
                arguments.Add(projectPath);
                arguments.Add("--configuration");
                arguments.Add("Release");
            },
            environmentVariables: null,
            cancellationToken).ConfigureAwait(false);

        if (buildResult.ExitCode != 0 || buildResult.TimedOut)
        {
            Assert.Fail($"{projectPath} build failed before smoke execution.{Environment.NewLine}{FormatResult(buildResult)}");
        }

        lock (BuildSync)
        {
            BuiltProjects.Add(projectPath);
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ModularityKit.Mutator.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for smoke tests.");
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }

    private static string FormatResult(ExampleRunResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Timed out: {result.TimedOut}");
        builder.AppendLine($"Exit code: {result.ExitCode}");
        builder.AppendLine("stdout:");
        builder.AppendLine(string.IsNullOrWhiteSpace(result.StandardOutput) ? "<empty>" : result.StandardOutput.Trim());
        builder.AppendLine("stderr:");
        builder.AppendLine(string.IsNullOrWhiteSpace(result.StandardError) ? "<empty>" : result.StandardError.Trim());
        return builder.ToString();
    }

    private static async Task<ExampleRunResult> RunProcessAsync(
        string workingDirectory,
        Action<List<string>> configureArguments,
        IReadOnlyDictionary<string, string?>? environmentVariables,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var arguments = new List<string>();
        configureArguments(arguments);

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (environmentVariables is not null)
        {
            foreach (var environmentVariable in environmentVariables)
            {
                if (environmentVariable.Value is null)
                    startInfo.Environment.Remove(environmentVariable.Key);
                else
                    startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
            }
        }

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stdoutCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                stdoutCompleted.TrySetResult();
                return;
            }

            stdout.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                stderrCompleted.TrySetResult();
                return;
            }

            stderr.AppendLine(args.Data);
        };

        process.Exited += (_, _) => exited.TrySetResult();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var allCompleted = exited.Task;
        var timeoutTask = Task.Delay(Timeout, timeoutCts.Token);
        var completedTask = await Task.WhenAny(allCompleted, timeoutTask).ConfigureAwait(false);

        if (completedTask != allCompleted)
        {
            TryKill(process);
            await AwaitSafely(exited.Task).ConfigureAwait(false);
            return new ExampleRunResult(-1, stdout.ToString(), stderr.ToString(), TimedOut: true);
        }

        timeoutCts.Cancel();

        var drainTask = Task.WhenAll(stdoutCompleted.Task, stderrCompleted.Task);
        await Task.WhenAny(drainTask, Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)).ConfigureAwait(false);

        return new ExampleRunResult(process.ExitCode, stdout.ToString(), stderr.ToString(), TimedOut: false);
    }

    private static async Task AwaitSafely(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
