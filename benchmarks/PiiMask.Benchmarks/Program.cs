using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PiiMask;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class MaskingBenchmarks
{
    [Benchmark] public string Email() => Pii.Mask("rahul@example.com", SensitiveKind.Email);
    [Benchmark] public string PlainLog() => Pii.MaskText("Request completed successfully in 42 milliseconds.");
    [Benchmark] public string MixedLog() => Pii.MaskText("User rahul@example.com called +1 415-555-2671; card=4111 1111 1111 1111; Bearer abcdefghijklmnop");
}
