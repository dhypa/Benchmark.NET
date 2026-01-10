using BenchmarkDotNet.Running;

namespace ConcatSpanCollectionExpr;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<ConcatBench>();
    }
}
