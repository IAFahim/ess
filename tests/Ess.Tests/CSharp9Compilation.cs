using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ess.Tests;

internal sealed record CompiledSource(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    bool? MachineIsUnmanaged,
    bool MachineIsComponent,
    byte[]? Assembly);

internal sealed class CSharp9Compilation
{
    private const string UnityEntitiesStub = "namespace Unity.Entities { public interface IComponentData { } }";

    private readonly MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => Path.GetFileName(path) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll" or "System.Console.dll")
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToArray();

    public CompiledSource Compile(string machineSource, string machineTypeName, string assemblyName) =>
        Compile([machineSource, UnityEntitiesStub], machineTypeName, assemblyName);

    public CompiledSource CompileWithoutUnity(string machineSource, string machineTypeName, string assemblyName) =>
        Compile([machineSource], machineTypeName, assemblyName);

    public CompiledSource CompileWithComponentFile(string machineSource, string componentSource, string machineTypeName, string assemblyName) =>
        Compile([machineSource, componentSource, UnityEntitiesStub], machineTypeName, assemblyName);

    public CompiledSource CompileProgram(string machineSource, string programSource, string machineTypeName, string assemblyName) =>
        Compile([machineSource, programSource], machineTypeName, assemblyName, OutputKind.ConsoleApplication);

    private CompiledSource Compile(
        IEnumerable<string> sources,
        string machineTypeName,
        string assemblyName,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)),
            references,
            new CSharpCompilationOptions(outputKind, nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);

        var errors = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToList();
        var warnings = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.ToString()).ToList();
        var machineType = compilation.GetTypeByMetadataName(machineTypeName);

        var isComponent = machineType is not null
            && machineType.AllInterfaces.Any(type => type.ToDisplayString() == "Unity.Entities.IComponentData");

        return new CompiledSource(errors, warnings, machineType?.IsUnmanagedType, isComponent, emitted.Success ? stream.ToArray() : null);
    }
}

internal sealed class MachineDriver
{
    private readonly object machine;
    private readonly Type machineType;
    private readonly FieldInfo varsField;

    public MachineDriver(byte[] assemblyBytes, string machineTypeName)
    {
        var context = new AssemblyLoadContext(machineTypeName, isCollectible: true);
        var assembly = context.LoadFromStream(new MemoryStream(assemblyBytes));
        machineType = assembly.GetType(machineTypeName)!;
        machine = Activator.CreateInstance(machineType)!;
        varsField = machineType.GetField("vars")!;
    }

    public void Start() => machineType.GetMethod("Start")!.Invoke(machine, null);

    public void Dispatch(string eventName)
    {
        var eventType = machineType.GetNestedType("EventId")!;
        machineType.GetMethod("DispatchEvent")!.Invoke(machine, [Enum.Parse(eventType, eventName)]);
    }

    public void SetVar(string name, object value)
    {
        var vars = varsField.GetValue(machine)!;
        vars.GetType().GetField(name)!.SetValue(vars, value);
        varsField.SetValue(machine, vars);
    }

    public string Snapshot()
    {
        var vars = varsField.GetValue(machine)!;
        var values = vars.GetType().GetFields().Select(field => $"{field.Name}={field.GetValue(vars)}");
        return $"{machineType.GetField("stateId")!.GetValue(machine)}|{string.Join(",", values)}";
    }
}
