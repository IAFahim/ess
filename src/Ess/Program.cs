using Ess;

var useColor = !Console.IsOutputRedirected
    && !OperatingSystem.IsWindows()
    && Environment.GetEnvironmentVariable("NO_COLOR") is null;

var console = new EssConsole(Console.Out, useColor);
var catalog = new CommandCatalog();
var resolver = new CommandResolver(catalog, new EditDistance());
var diagrams = new DiagramFiles();
var subprocess = new Subprocess();

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("ess");

var tool = new StateSmithTool(new EssHome(), new StateSmithAssets(), http, console);
var generator = new CodeGenerator(tool, subprocess, new GenerationOutputJudge(), diagrams, console);
var gitSaver = new GitSaver(subprocess, console, new RepoNames());

var app = new EssApp(
    resolver,
    catalog,
    new EssKeys(),
    console,
    new NewCommand(generator, tool, new StarterDiagram(), new MachineNames(), console),
    new RunCommand(generator),
    new WatchCommand(generator, diagrams, gitSaver, console),
    new GitCommand(gitSaver),
    new AppVersion().Current);

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
