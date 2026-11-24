using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Ess;

internal sealed record ProcessResult(int ExitCode, string Output)
{
    public bool Succeeded => ExitCode == 0;

    public bool ProgramMissing => ExitCode == Subprocess.ProgramMissingExitCode;
}

internal sealed class Subprocess
{
    public const int ProgramMissingExitCode = 127;

    public async Task<ProcessResult> Run(string program, IReadOnlyList<string> arguments, string workingDirectory, CancellationToken cancel, IReadOnlyDictionary<string, string>? extraEnvironment = null)
    {
        var startInfo = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GH_PROMPT_DISABLED"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        foreach (var (name, value) in extraEnvironment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[name] = value;
        }

        var collected = new StringBuilder();
        var gate = new object();

        void Collect(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (gate)
            {
                collected.AppendLine(line);
            }
        }

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, received) => Collect(received.Data);
        process.ErrorDataReceived += (_, received) => Collect(received.Data);

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new ProcessResult(ProgramMissingExitCode, $"{program} was not found");
        }

        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancel);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        lock (gate)
        {
            return new ProcessResult(process.ExitCode, collected.ToString());
        }
    }
}
