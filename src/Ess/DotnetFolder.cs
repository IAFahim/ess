namespace Ess;

internal sealed class DotnetFolder(MachineNames machineNames)
{
    public const string FolderName = "Dotnet~";
    public const string ProgramFileName = "Program.cs";
    public const string HowToRun = "cd " + FolderName + " && dotnet run";

    private const string ProjectTemplate = """
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <LangVersion>9.0</LangVersion>
            <ImplicitUsings>disable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
          </PropertyGroup>

          <ItemGroup>
            <Compile Include="../*.cs" />
          </ItemGroup>

        </Project>
        """;

    private const string ProgramTemplate = """
        using System;
        using Machines;

        var machine = new {name}();
        machine.Start();
        Show("Start");

        for (var press = 0; press < 3; press++)
        {
            machine.DispatchEvent({name}.EventId.TOGGLE);
            Show("TOGGLE");
        }

        void Show(string eventName)
        {
            Console.WriteLine($"{eventName,-8} -> {machine.stateId,-4} count={machine.vars.count}");
        }
        """;

    public string ProjectFileName(string folder) =>
        machineNames.ToIdentifier(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder))) + ".csproj";

    public string Project() => ProjectTemplate + "\n";

    public string Program(string machineName) => ProgramTemplate.Replace("{name}", machineName, StringComparison.Ordinal) + "\n";

    public bool HasProject(string folder)
    {
        var dotnetFolder = Path.Combine(folder, FolderName);
        return Directory.Exists(dotnetFolder) && Directory.EnumerateFiles(dotnetFolder, "*.csproj").Any();
    }
}
