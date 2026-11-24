using Ess;

var useColor = !Console.IsOutputRedirected
    && !OperatingSystem.IsWindows()
    && Environment.GetEnvironmentVariable("NO_COLOR") is null;

var console = new EssConsole(Console.Out, useColor);
var catalog = new CommandCatalog();
var demoRequest = new DemoRequest();
var resolver = new CommandResolver(catalog, new EditDistance(), demoRequest);
var diagrams = new DiagramFiles();
var subprocess = new Subprocess();

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("ess");

var tool = new StateSmithTool(new EssHome(), new StateSmithAssets(), http, console);
var generator = new CodeGenerator(tool, subprocess, new GenerationOutputJudge(), diagrams, new StructConverter(), console);
var gitSaver = new GitSaver(subprocess, console, new RepoNames());
var keys = new EssKeys();

var app = new EssApp(
    resolver,
    catalog,
    keys,
    console,
    new NewCommand(generator, tool, new StarterDiagram(), new MachineNames(), console),
    new RunCommand(generator),
    new WatchCommand(generator, diagrams, gitSaver, keys, console),
    new GitCommand(gitSaver),
    new DemoCommand(new DemoCatalog(), demoRequest, new DemoFiles(), generator, subprocess, console),
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
