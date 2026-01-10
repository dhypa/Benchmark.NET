```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6691/22H2/2022Update)
Intel Core i5-10400F CPU 2.90GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.101
  [Host]    : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Size | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| **CollectionExpr_Array** | **4**    |  **12.92 ns** | **0.285 ns** | **0.426 ns** |  **1.00** |    **0.05** | **0.0051** |      **32 B** |        **1.00** |
| CustomConcat_Array   | 4    |  13.04 ns | 0.315 ns | 0.461 ns |  1.01 |    0.05 | 0.0051 |      32 B |        1.00 |
|                      |      |           |          |          |       |         |        |           |             |
| **CollectionExpr_Array** | **10**   |  **13.24 ns** | **0.191 ns** | **0.261 ns** |  **1.00** |    **0.03** | **0.0076** |      **48 B** |        **1.00** |
| CustomConcat_Array   | 10   |  13.80 ns | 0.315 ns | 0.472 ns |  1.04 |    0.04 | 0.0076 |      48 B |        1.00 |
|                      |      |           |          |          |       |         |        |           |             |
| **CollectionExpr_Array** | **100**  |  **25.74 ns** | **0.638 ns** | **0.914 ns** |  **1.00** |    **0.05** | **0.0357** |     **224 B** |        **1.00** |
| CustomConcat_Array   | 100  |  26.54 ns | 0.735 ns | 1.030 ns |  1.03 |    0.05 | 0.0357 |     224 B |        1.00 |
|                      |      |           |          |          |       |         |        |           |             |
| **CollectionExpr_Array** | **1024** | **137.49 ns** | **3.034 ns** | **4.351 ns** |  **1.00** |    **0.04** | **0.3302** |    **2072 B** |        **1.00** |
| CustomConcat_Array   | 1024 | 138.60 ns | 3.320 ns | 4.866 ns |  1.01 |    0.05 | 0.3302 |    2072 B |        1.00 |
