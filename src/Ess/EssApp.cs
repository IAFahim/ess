namespace Ess;

internal sealed class EssApp(
    ArgumentParser parser,
    CommandTable table,
    HelpWriter help,
    EssKeys keys,
    EssConsole console,
    NewCommand newCommand,
    RunCommand runCommand,
    WatchCommand watchCommand,
    GitCommand gitCommand,
    DemoCommand demoCommand,
    UpdateCommand updateCommand,
    OpenCommand openCommand,
    string version,
    bool interactive)
{
    private const char YesKey = 'y';
    private const char QuitKey = 'q';

    public async Task<int> Run(string[] args, string folder, CancellationToken cancel)
    {
        var invocation = parser.Parse(args);

        switch (invocation.Kind)
        {
            case InvocationKind.NoArguments:
                return interactive ? await ChooseFromMenu(folder, cancel) : Print(help.Global());

            case InvocationKind.GlobalHelp:
                return Print(help.Global());

            case InvocationKind.CommandHelp:
                return Print(help.For(invocation.Command!));

            case InvocationKind.Version:
                console.Line($"ess {version}");
                return 0;

            case InvocationKind.Command:
                return await Execute(invocation.Command!.Command, invocation.Arguments, folder, chooseAfterList: false, cancel);

            default:
                return await Fail(invocation, args, folder, cancel);
        }
    }

    private int Print(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            console.Line(line);
        }

        return 0;
    }

    private async Task<int> Fail(Invocation invocation, string[] args, string folder, CancellationToken cancel)
    {
        if (interactive && invocation.SuggestedCommand is not null)
        {
            console.Warn($"{invocation.Error}");
            console.Line($"    Did you mean  {invocation.SuggestedLine} ?  Press y for yes, any other key to stop.");

            if (await keys.ReadAsync(cancel) != YesKey)
            {
                return 1;
            }

            return await Run(ReplaceCommandWord(args, invocation.SuggestedCommand.Name), folder, cancel);
        }

        console.UsageError(invocation.Error);
        if (invocation.SuggestedLine.Length > 0)
        {
            console.UsageHint($"Did you mean:  {invocation.SuggestedLine}");
        }

        return 1;
    }

    private static string[] ReplaceCommandWord(string[] args, string commandName)
    {
        var replaced = args.ToArray();
        var commandWordIndex = Array.FindIndex(replaced, arg => !arg.StartsWith('-'));
        replaced[commandWordIndex] = commandName;
        return replaced;
    }

    private async Task<int> ChooseFromMenu(string folder, CancellationToken cancel)
    {
        var lastExitCode = 0;

        while (!cancel.IsCancellationRequested)
        {
            console.Line("ess  -  easy StateSmith");
            console.Line($"folder: {folder}");
            console.Line();

            foreach (var line in help.Menu())
            {
                console.Line(line);
            }

            console.Line();
            console.Line($"Press one key  ({QuitKey} to quit, ess --help for everything):");

            var pressed = await keys.ReadAsync(cancel);
            if (pressed == QuitKey)
            {
                return lastExitCode;
            }

            var command = table.Find(pressed.ToString());
            if (command is null || command.MinArguments > 0)
            {
                console.Bad($"\"{pressed}\" is not one of the keys.");
                console.Line();
                continue;
            }

            console.Line();
            lastExitCode = await Execute(command.Command, [], folder, chooseAfterList: true, cancel);
            console.Line();
        }

        return lastExitCode;
    }

    private Task<int> Execute(EssCommand command, IReadOnlyList<string> arguments, string folder, bool chooseAfterList, CancellationToken cancel)
    {
        return command switch
        {
            EssCommand.New => newCommand.Run(arguments.ToArray(), folder, cancel),
            EssCommand.Run => runCommand.Run(folder, cancel),
            EssCommand.Watch => watchCommand.Run(folder, cancel),
            EssCommand.Git => gitCommand.Run(folder, cancel),
            EssCommand.Demo => demoCommand.Run(arguments, folder, chooseAfterList, cancel),
            EssCommand.Update => updateCommand.Run(arguments, folder, cancel),
            EssCommand.Open => openCommand.Run(arguments, folder, cancel),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown ess command"),
        };
    }
}
