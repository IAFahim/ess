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

var home = new EssHome();
var userFolders = new UserFolders();
var tool = new StateSmithTool(home, new StateSmithAssets(), http, console);
var omaspec = new OmaspecTool(home, userFolders, new OmaspecRelease(), new DesktopEntry(), new ScreenCards(), http, subprocess, console);
var generator = new CodeGenerator(tool, subprocess, new GenerationOutputJudge(), diagrams, new StructConverter(), console);
var dotnet = new DotnetLocator(subprocess, new SdkList());
var svCommand = new SvCommand(new SvRepo(subprocess, console, "https://github.com/IAFahim/sv.git"), new DemoFiles(), generator, subprocess, dotnet, new MachineNames(), console);
var gitSaver = new GitSaver(subprocess, console, new RepoNames());
var demos = new DemoCatalog();
var unityPackage = new UnityPackage(new MachineNames());

var app = new EssApp(
    parser,
    table,
    new HelpWriter(table),
    keys,
    console,
    new NewCommand(generator, tool, new StarterDiagram(), new MachineNames(), unityPackage, svCommand, console),
    new RunCommand(generator),
    new WatchCommand(generator, diagrams, gitSaver, keys, console),
    new GitCommand(gitSaver),
    new DemoCommand(demos, new DemoSelector(demos), new DemoFiles(), generator, subprocess, dotnet, svCommand, keys, console),
    new UpdateCommand(svCommand, console),
    new OpenCommand(omaspec, diagrams, new DiagramLookup(diagrams, new EditDistance()), userFolders, subprocess, console),
    new UnityCommand(unityPackage, new StarterDiagram(), new MachineNames(), generator, userFolders, console),
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
