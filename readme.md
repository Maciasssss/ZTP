# .NET Garbage Collector Performance Testing Suite
![.NET](https://img.shields.io/badge/.NET-6.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-5391FE?style=for-the-badge&logo=powershell&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)

> A comprehensive testing framework for evaluating the impact of different .NET Garbage Collector configurations on memory-intensive operations

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Usage](#-usage)
- [GC Configurations](#-gc-configurations)
- [How It Works](#-how-it-works)
- [Analyzing Results](#-analyzing-results)
- [Expected Findings](#-expected-findings)
- [Customization](#-customization)
- [Troubleshooting](#-troubleshooting)
- [References](#-references)
- [License](#-license)
- [Acknowledgments](#-acknowledgments)

## 🔍 Overview

This project provides tools to analyze how different .NET Garbage Collector configurations affect application performance, memory usage patterns, and garbage collection behavior. The suite uses large matrix operations and bitmap processing to create measurable memory pressure that demonstrates the real-world impact of various GC settings.

## ✨ Features

- **Comprehensive Testing**: Compare multiple GC configurations in a controlled environment
- **Real-World Workloads**: Uses matrix multiplication (15000×15000) and bitmap processing
- **Visual Profiling**: Integration with JetBrains dotMemory for in-depth analysis
- **Automation**: PowerShell script for running tests with minimal intervention
- **Detailed Metrics**: Collection counts, memory usage, operation timing, and more

## 📋 Prerequisites

- **.NET SDK** (6.0 or later)
- **PowerShell** 5.1 or later
- **JetBrains dotMemory** (for profiling)
- **RAM**: 8GB minimum, 16GB recommended

## 💻 Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/dotnet-gc-testing.git
   cd dotnet-gc-testing
   ```

2. Build the C# application:
   ```
   dotnet build -c Release
   ```

## 🚀 Usage

1. Start JetBrains dotMemory

2. Run the PowerShell script:
   ```powershell
   .\dotmemory-test-scriptg.ps1
   ```

3. For each test configuration:
   - Attach dotMemory to the process (ID will be displayed)
   - Take a baseline snapshot
   - Press ESC to start the memory-intensive operations
   - Take a final snapshot when operations complete
   - Compare snapshots to analyze the impact of GC settings

## ⚙️ GC Configurations

| Test Name | Description | Environment Variables |
|:----------|:------------|:----------------------|
| **Baseline** | Default settings for comparison | None |
| **Server GC** | Multiple GC heaps (parallel collection) | `DOTNET_GCServer=1` |
| **Workstation GC** | Single GC heap | `DOTNET_GCServer=0` |
| **Low Memory Limit** | Forces more frequent collections | `DOTNET_GCHeapHardLimitPercent=25` |
| **Low LOH Threshold** | More objects on Large Object Heap | `DOTNET_GCLOHThreshold=32768` |
| **High LOH Threshold** | Fewer objects on Large Object Heap | `DOTNET_GCLOHThreshold=512000` |
| **Aggressive Memory Conservation** | Most aggressive memory conservation | `DOTNET_GCConserveMemory=9` |
| **Memory Conservation + LOH Compaction** | Reduces fragmentation | `DOTNET_GCConserveMemory=7`<br>`DOTNET_GCLargeObjectHeapCompactionMode=CompactOnce` |
| **Non-Concurrent GC** | Disables background collections | `DOTNET_GCConcurrent=0` |
| **Server + Background GC** | Parallel with background collections | `DOTNET_GCServer=1`<br>`DOTNET_GCConcurrent=1` |
| **Server GC + No CPU Groups** | Explicit heap count | `DOTNET_GCServer=1`<br>`DOTNET_GCCpuGroup=0`<br>`DOTNET_GCHeapCount=8` |

## 🔄 How It Works

The testing framework operates in the following sequence:

1. **Configuration**: PowerShell script sets environment variables for the chosen GC settings
2. **Initialization**: C# app starts and waits for dotMemory attachment
3. **Baseline**: Initial memory snapshot captures the starting state
4. **Memory Pressure**: Application performs:
   - Matrix creation and filling (15000×15000 ≈ 1.8GB)
   - Matrix multiplication operations
   - Bitmap processing with unmanaged memory
5. **Analysis**: Final snapshot captures the end state for comparison
6. **Repeat**: Process repeats for each GC configuration

## 📊 Analyzing Results

When comparing dotMemory snapshots, focus on:

### Memory Usage Patterns
- **Memory Growth**: Rate at which memory increases
- **Peak Usage**: Maximum memory consumption
- **Retained Memory**: What remains after operations complete

### GC Collection Metrics
- **Collection Counts**: Number of collections per generation
- **Collection Duration**: Time spent in garbage collection
- **Heap Fragmentation**: Gaps in memory caused by collections

### Performance Impact
- **Operation Timing**: Execution time of key operations
- **GC Pauses**: Correlation between operations and collection pauses
- **CPU Usage**: Processor time spent in GC vs. application code

## 🔬 Expected Findings

Different GC configurations exhibit distinct behaviors:

- **Server GC**: Better throughput on multi-core systems, higher memory usage
- **Workstation GC**: More frequent but shorter pauses, better for interactive apps
- **Low Memory Limit**: More aggressive collections, smaller memory footprint
- **LOH Threshold Settings**: Different fragmentation patterns and pause durations
- **Memory Conservation**: Trade memory usage for CPU time
- **Concurrent vs. Non-Concurrent**: Different pause patterns (shorter more frequent vs. longer less frequent)

## 🛠️ Customization

Adapt the test suite to your needs by:

```csharp
// In Program.cs - Adjust matrix size for different memory pressure
const int LARGE_ROWS = 15000;  // Change based on your system's memory
const int LARGE_COLS = 15000;  // Change based on your system's memory
```

```powershell
# In PowerShell script - Add custom GC configurations
Test-GCConfiguration -TestName "Custom Config" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
    "DOTNET_GCLatencyLevel" = "1"
} -Description "Your custom description"
```

## ❓ Troubleshooting

| Issue | Solution |
|:------|:---------|
| **OutOfMemoryException** | Reduce matrix size or increase available RAM |
| **Slow Performance** | Expected with certain configurations; compare relative differences |
| **dotMemory Connection Issues** | Run PowerShell as administrator |
| **High CPU Usage** | Normal for Server GC with multiple processors |
| **Excessive Memory Usage** | Try configurations with memory limits |

## 📚 References

- [.NET GC Configuration Documentation](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector)
- [Understanding GC Pauses in .NET Core](https://devblogs.microsoft.com/dotnet/gc-perf-series-understanding-gc-pauses/)
- [Large Object Heap Improvements](https://devblogs.microsoft.com/dotnet/large-object-heap-improvements-in-net-core-2-0/)
- [JetBrains dotMemory Documentation](https://www.jetbrains.com/help/dotmemory/Introduction.html)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👏 Acknowledgments

- Microsoft .NET team for the configurable GC implementation
- JetBrains for the excellent dotMemory profiler