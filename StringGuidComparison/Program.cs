using BenchmarkDotNet.Running;
using Test;

namespace StringGuidComparison;

internal class Program
{
    static void Main(string[] args)
    {

        BenchmarkRunner.Run<BenchMark>();
    }
}
