using System.Threading.Channels;

namespace Ess;

internal sealed class WatchCommand(CodeGenerator generator, DiagramFiles diagrams, GitSaver git, EssKeys keys, EssConsole console)
{
    private const char StopKey = 'q';
    private const char SaveKey = 'g';

    private readonly TimeSpan quietTimeBeforeMaking = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan keyPollInterval = TimeSpan.FromMilliseconds(100);

    public async Task<int> Run(string folder, CancellationToken cancel)
    {
        if (diagrams.In(folder).Count == 0)
        {
            console.Warn("There is no diagram in this folder. Run:  ess n");
            return 1;
        }

        using var stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        using var oneAtATime = new SemaphoreSlim(1, 1);

        console.Step($"Watching {folder}");
        console.Step(git.IsOwnRepository(folder)
            ? "Git: every good make is saved and pushed."
            : "Git: off until you press g.");
        console.Step($"Save a .plantuml file to make the code.  {SaveKey} = save to GitHub now,  {StopKey} = stop.");
        console.Line();

        await MakeAndSave(folder, ["first make"], oneAtATime, stopSource.Token);

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

        var keyListener = ListenForKeys(folder, oneAtATime, stopSource);

        try
        {
            await foreach (var firstName in changedNames.Reader.ReadAllAsync(stopSource.Token))
            {
                var names = new SortedSet<string> { firstName };
                await Task.Delay(quietTimeBeforeMaking, stopSource.Token);

                while (changedNames.Reader.TryRead(out var anotherName))
                {
                    names.Add(anotherName);
                }

                await MakeAndSave(folder, names, oneAtATime, stopSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }

        await stopSource.CancelAsync();
        await keyListener;

        console.Line();
        console.Good("Stopped watching.");
        return 0;
    }

    private async Task ListenForKeys(string folder, SemaphoreSlim oneAtATime, CancellationTokenSource stopSource)
    {
        try
        {
            while (!stopSource.IsCancellationRequested)
            {
                var pressed = keys.ReadIfPressed();

                if (pressed == StopKey)
                {
                    await stopSource.CancelAsync();
                    return;
                }

                if (pressed == SaveKey)
                {
                    await SaveNow(folder, oneAtATime, stopSource.Token);
                }

                await Task.Delay(keyPollInterval, stopSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveNow(string folder, SemaphoreSlim oneAtATime, CancellationToken cancel)
    {
        await oneAtATime.WaitAsync(cancel);
        try
        {
            await git.Save(folder, "ess: save", createGitHubRepoWhenMissing: true, cancel);
            console.Line();
        }
        finally
        {
            oneAtATime.Release();
        }
    }

    private async Task MakeAndSave(string folder, IReadOnlyCollection<string> changedNames, SemaphoreSlim oneAtATime, CancellationToken cancel)
    {
        await oneAtATime.WaitAsync(cancel);
        try
        {
            var changedText = string.Join(", ", changedNames);
            console.Step($"{DateTime.Now:HH:mm:ss}  {changedText}");

            if (!await generator.Make(folder, cancel))
            {
                console.Warn("Fix the diagram and save again. Nothing was saved to git.");
                console.Line();
                return;
            }

            if (git.IsOwnRepository(folder))
            {
                await git.Save(folder, $"ess: {changedText}", createGitHubRepoWhenMissing: false, cancel);
            }

            console.Line();
        }
        finally
        {
            oneAtATime.Release();
        }
    }
}
