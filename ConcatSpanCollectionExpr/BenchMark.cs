using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;

namespace ConcatSpanCollectionExpr;

[MediumRunJob]           
[MemoryDiagnoser]          
public class ConcatBench
{
    private byte[] _first = default!;
    private byte[] _second = default!;

    [Params(4, 10, 100, 1024)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var r = new Random(12345);          // fixed seed for repeatability
        _first = new byte[Size];
        _second = new byte[Size];
        r.NextBytes(_first);
        r.NextBytes(_second);
    }

    // Baseline: collection expression
    [Benchmark(Baseline = true)]
    public int CollectionExpr_Array()
    {
        ReadOnlySpan<byte> combined = [.. _first, .. _second];

        return combined[0] | combined[^1];
    }

    [Benchmark]
    public int CustomConcat_Array()
    {
        ReadOnlySpan<byte> combined = ConcatToArray(_first, _second);
        return combined[0] | combined[^1];
    }

    private static byte[] ConcatToArray(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var dest = new byte[a.Length + b.Length];

        a.CopyTo(dest);                 
        b.CopyTo(dest.AsSpan(a.Length));  

        return dest;
    }
}
