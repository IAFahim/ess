using Ess;

var useColor = !Console.IsOutputRedirected
    && !OperatingSystem.IsWindows()
    && Environment.GetEnvironmentVariable("NO_COLOR") is null;

var console = new EssConsole(Console.Out, Console.Error, useColor);
var table = new CommandTable();
var parser = new ArgumentParser(table, new EditDistance());
var diagrams = new DiagramFiles();
var subprocess = new Subprocess();
var keys = new EssKeys();

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("ess");

var tool = new StateSmithTool(new EssHome(), new StateSmithAssets(), http, console);
var generator = new CodeGenerator(tool, subprocess, new GenerationOutputJudge(), diagrams, new StructConverter(), console);
var svCommand = new SvCommand(new SvRepo(subprocess, console, "https://github.com/IAFahim/sv.git"), new DemoFiles(), generator, subprocess, new MachineNames(), console);
var gitSaver = new GitSaver(subprocess, console, new RepoNames());
var demos = new DemoCatalog();

var app = new EssApp(
    parser,
    table,
    new HelpWriter(table),
    keys,
    console,
    new NewCommand(generator, tool, new StarterDiagram(), new MachineNames(), svCommand, console),
    new RunCommand(generator),
    new WatchCommand(generator, diagrams, gitSaver, keys, console),
    new GitCommand(gitSaver),
    new DemoCommand(demos, new DemoSelector(demos), new DemoFiles(), generator, subprocess, svCommand, keys, console),
    new UpdateCommand(svCommand, console),
    new AppVersion().Current,
    interactive: !Console.IsInputRedirected);

using var cancelSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, pressed) =>
{
    pressed.Cancel = true;
    cancelSource.Cancel();
};

try
{
    return await app.Run(args, Directory.GetCurrentDirectory(), cancelSource.Token);
}
catch (OperationCanceledException)
{
    return 130;
}
