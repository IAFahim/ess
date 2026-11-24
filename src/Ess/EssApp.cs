namespace Ess;

internal sealed class EssApp(
    CommandResolver resolver,
    CommandCatalog catalog,
    EssKeys keys,
    EssConsole console,
    NewCommand newCommand,
    RunCommand runCommand,
    WatchCommand watchCommand,
    GitCommand gitCommand,
    string version)
{
    private const char YesKey = 'y';
    private const char QuitKey = 'q';

    public async Task<int> Run(string[] args, string folder, CancellationToken cancel)
    {
        var typed = args.Length > 0 ? args[0] : "";
        var extraArguments = args.Skip(1).ToArray();
        var resolution = resolver.Resolve(typed);

        switch (resolution.Kind)
        {
            case ResolutionKind.RunCommand:
                return await Execute(resolution.Command!.Command, extraArguments, folder, cancel);

            case ResolutionKind.ShowVersion:
                console.Line($"ess {version}");
                return 0;

            case ResolutionKind.DidYouMean:
                return await ConfirmThenExecute(typed, resolution.Command!, extraArguments, folder, cancel);

            case ResolutionKind.Unknown:
                console.Bad($"I do not know \"{typed}\".");
                console.Line();
                return await ChooseFromMenu(folder, cancel);

            default:
                return await ChooseFromMenu(folder, cancel);
        }
    }

    private async Task<int> ConfirmThenExecute(string typed, CommandInfo guess, string[] extraArguments, string folder, CancellationToken cancel)
    {
        console.Warn($"I do not know \"{typed}\". Did you mean  {guess.Key}  ({guess.Name})?");
        console.Line("    Press y for yes, any other key to stop.");

        if (keys.Read() != YesKey)
        {
            return 1;
        }

        return await Execute(guess.Command, extraArguments, folder, cancel);
    }

    private async Task<int> ChooseFromMenu(string folder, CancellationToken cancel)
    {
        console.Line("ess  -  easy state smith");
        console.Line($"folder: {folder}");
        console.Line();

        foreach (var command in catalog.All)
        {
            console.Line($"  {command.Key}   {command.Name,-6}  {command.Meaning}");
        }

        console.Line();
        console.Line($"Press one key  ({QuitKey} to quit):");

        var pressed = keys.Read();
        if (pressed == QuitKey)
        {
            return 0;
        }

        var resolution = resolver.Resolve(pressed.ToString());
        if (resolution.Kind != ResolutionKind.RunCommand)
        {
            console.Bad($"\"{pressed}\" is not one of the keys.");
            return 1;
        }

        console.Line();
        return await Execute(resolution.Command!.Command, [], folder, cancel);
    }

    private Task<int> Execute(EssCommand command, string[] extraArguments, string folder, CancellationToken cancel)
    {
        return command switch
        {
            EssCommand.New => newCommand.Run(extraArguments, folder, cancel),
            EssCommand.Run => runCommand.Run(folder, cancel),
            EssCommand.Watch => watchCommand.Run(folder, cancel),
            EssCommand.Git => gitCommand.Run(folder, cancel),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown ess command"),
        };
    }
}
