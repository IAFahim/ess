using System.Threading.Channels;

namespace Ess;

internal sealed class WatchCommand(CodeGenerator generator, DiagramFiles diagrams, GitSaver git, EssConsole console)
{
    private readonly TimeSpan quietTimeBeforeMaking = TimeSpan.FromMilliseconds(500);

    public async Task<int> Run(string folder, CancellationToken cancel)
    {
        if (diagrams.In(folder).Count == 0)
        {
            console.Warn("There is no diagram in this folder. Run:  ess n");
            return 1;
        }

        var saveToGit = git.IsOwnRepository(folder);

        console.Step($"Watching {folder}");
        console.Step(saveToGit
            ? "Git: every good make is saved and pushed."
            : "Git: off. Run  ess g  once to turn it on.");
        console.Step("Save a .plantuml file to make the code. Press Ctrl+C to stop.");
        console.Line();

        await MakeAndSave(folder, ["first make"], saveToGit, cancel);

        var changedNames = Channel.CreateUnbounded<string>();
        using var watcher = new FileSystemWatcher(folder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        void NoticeChange(object? sender, FileSystemEventArgs change)
        {
            if (diagrams.IsDiagram(change.FullPath))
            {
                changedNames.Writer.TryWrite(Path.GetFileName(change.FullPath));
            }
        }

        watcher.Changed += NoticeChange;
        watcher.Created += NoticeChange;
        watcher.Renamed += NoticeChange;

        try
        {
            await foreach (var firstName in changedNames.Reader.ReadAllAsync(cancel))
            {
                var names = new SortedSet<string> { firstName };
                await Task.Delay(quietTimeBeforeMaking, cancel);

                while (changedNames.Reader.TryRead(out var anotherName))
                {
                    names.Add(anotherName);
                }

                await MakeAndSave(folder, names, saveToGit, cancel);
            }
        }
        catch (OperationCanceledException)
        {
        }

        console.Line();
        console.Good("Stopped watching.");
        return 0;
    }

    private async Task MakeAndSave(string folder, IReadOnlyCollection<string> changedNames, bool saveToGit, CancellationToken cancel)
    {
        var changedText = string.Join(", ", changedNames);
        console.Step($"{DateTime.Now:HH:mm:ss}  {changedText}");

        if (!await generator.Make(folder, cancel))
        {
            console.Warn("Fix the diagram and save again. Nothing was saved to git.");
            console.Line();
            return;
        }

        if (saveToGit)
        {
            await git.Save(folder, $"ess: {changedText}", createGitHubRepoWhenMissing: false, cancel);
        }

        console.Line();
    }
}
