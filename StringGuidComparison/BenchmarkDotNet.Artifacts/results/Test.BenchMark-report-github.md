```

BenchmarkDotNet v0.15.4, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i5-10400F CPU 2.90GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.300
  [Host]     : .NET 8.0.16 (8.0.16, 8.0.1625.21506), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.16 (8.0.16, 8.0.1625.21506), X64 RyuJIT x86-64-v3


```
| Method         | Index | Mean     | Error   | StdDev  | Ratio |
|--------------- |------ |---------:|--------:|--------:|------:|
| **EqualsParse**    | **0**     | **537.5 ns** | **3.15 ns** | **2.63 ns** |  **1.00** |
| EqualsToString | 0     | 539.3 ns | 1.70 ns | 1.42 ns |  1.00 |
|                |       |          |         |         |       |
| **EqualsParse**    | **1**     | **539.3 ns** | **2.08 ns** | **1.85 ns** |  **1.00** |
| EqualsToString | 1     | 546.4 ns | 5.66 ns | 4.72 ns |  1.01 |
|                |       |          |         |         |       |
| **EqualsParse**    | **2**     | **533.5 ns** | **2.93 ns** | **2.60 ns** |  **1.00** |
| EqualsToString | 2     | 542.2 ns | 3.43 ns | 3.21 ns |  1.02 |
|                |       |          |         |         |       |
| **EqualsParse**    | **3**     | **538.8 ns** | **2.57 ns** | **2.40 ns** |  **1.00** |
| EqualsToString | 3     | 545.7 ns | 3.94 ns | 3.69 ns |  1.01 |
|                |       |          |         |         |       |
| **EqualsParse**    | **4**     | **542.9 ns** | **3.41 ns** | **3.19 ns** |  **1.00** |
| EqualsToString | 4     | 545.7 ns | 7.90 ns | 7.39 ns |  1.01 |
|                |       |          |         |         |       |
| **EqualsParse**    | **5**     | **543.9 ns** | **2.60 ns** | **2.03 ns** |  **1.00** |
| EqualsToString | 5     | 539.6 ns | 1.41 ns | 1.18 ns |  0.99 |
|                |       |          |         |         |       |
| **EqualsParse**    | **6**     | **537.1 ns** | **2.83 ns** | **2.51 ns** |  **1.00** |
| EqualsToString | 6     | 548.5 ns | 3.45 ns | 3.06 ns |  1.02 |
|                |       |          |         |         |       |
| **EqualsParse**    | **7**     | **548.5 ns** | **5.24 ns** | **4.65 ns** |  **1.00** |
| EqualsToString | 7     | 549.1 ns | 4.11 ns | 3.65 ns |  1.00 |
