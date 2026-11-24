using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ess.Tests;

internal sealed record CompiledSource(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    bool? MachineIsUnmanaged,
    byte[]? Assembly);

internal sealed class CSharp9Compilation
{
    private const string UnityEntitiesStub = "namespace Unity.Entities { public interface IComponentData { } }";

    private readonly MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => Path.GetFileName(path) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll")
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToArray();

    public CompiledSource Compile(string machineSource, string machineTypeName, string assemblyName)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [
                CSharpSyntaxTree.ParseText(machineSource, parseOptions),
                CSharpSyntaxTree.ParseText(UnityEntitiesStub, parseOptions),
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);

        var errors = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToList();
        var warnings = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.ToString()).ToList();
        var machineType = compilation.GetTypeByMetadataName(machineTypeName);

        return new CompiledSource(errors, warnings, machineType?.IsUnmanagedType, emitted.Success ? stream.ToArray() : null);
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
