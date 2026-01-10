using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test;

[ShortRunJob]
public class BenchMark
{
    public static IEnumerable<string> strings = ["18397d0b-4a68-4252-ba43-1bbde7265037",
"f3fc33d2-9b13-45a0-b103-077af924c3da",
"c5c632fb-ddd2-4e31-b826-5d26e3b4dee2",
"7007e509-c0ac-4752-8e7f-7ff4d61e17fb",
"176545f4-fc08-44bd-9c7a-f1803d59fc39",
"81f88871-d11d-4594-9cc9-3264ec22e2d1",
"f93e7ac6-413e-4ee0-8146-839055f4b49d",
"8fd77896-bceb-4b57-8c5f-5c516ed43070"];

    public static IEnumerable<object[]> Cases => strings.Select(s => new object[] {Guid.Parse(s), s });

    [Benchmark(Baseline =true)]
    [ArgumentsSource(nameof(Cases))]
    public bool EqualsParse(Guid guid, string str)
    {
        return Guid.Parse(str).Equals(guid);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Cases))]
    public bool EqualsToString(Guid guid, string str)
    {
        return guid.ToString().Equals(str);
    }

}
public static class ParamsValues
{
    
}
