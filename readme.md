# .NET Bitmap Processing: Memory Management & Performance Analysis

![.NET](https://img.shields.io/badge/.NET-6.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-10.0+-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)

> A C# application to benchmark and analyze the performance characteristics of various memory management techniques in .NET for large bitmap/2D array processing.

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Memory Management Techniques Compared](#-memory-management-techniques-compared)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Usage](#-usage)
- [How It Works](#-how-it-works)
- [Analyzing Results](#-analyzing-results)
- [Expected Findings](#-expected-findings)
- [Customization](#-customization)
- [Troubleshooting](#-troubleshooting)
- [References](#-references)
- [License](#-license)

## 🔍 Overview

This project provides a C# console application designed to evaluate and compare the performance implications of different memory management strategies when handling large, fixed-size 2D byte arrays, simulating bitmap or image data. It demonstrates techniques ranging from standard managed arrays to raw unmanaged memory, allowing for analysis of CPU time, memory allocation patterns, and garbage collection impact.

The application performs a simple image processing task (applying a convolution kernel) repeatedly for each technique and measures the execution time. It also includes prompts for attaching a memory profiler (like JetBrains dotMemory or Visual Studio Profiler) to observe memory behavior in detail.

## ✨ Features

- **Comparative Benchmarking**: Directly compares five distinct memory management approaches for large array processing.
- **Fixed-Size Workloads**: Uses constant image dimensions (`10000x7000` pixels) and kernel size (`5x5`) for consistent comparisons.
- **Parallel Processing**: Leverages `Parallel.For` for applying the kernel, simulating multi-threaded workloads.
- **Profiler Integration**: Includes `Console.ReadKey()` pauses to facilitate attaching a profiler and taking memory snapshots at key stages.
- **GC Settings Display**: Prints current .NET GC settings and relevant environment variables at startup.
- **Explicit GC Calls**: Demonstrates `GC.Collect()` for baseline memory state between test suites (primarily for profiling observation).

## 🧠 Memory Management Techniques Compared

The application benchmarks the following five approaches:

1.  **Standard Managed Arrays (`byte[,]`)**:
    - Creation: `new byte[HEIGHT, WIDTH]`
    - Processing: Direct access.
    - Cleanup: Handled by the Garbage Collector.
2.  **Pooled Managed Arrays (`FixedSizeBitmapPool`)**:
    - Creation: Arrays are rented from a `ConcurrentBag` pool. New arrays are created if the pool is empty.
    - Processing: Direct access.
    - Cleanup: Arrays are returned to the pool if space is available; otherwise, left for GC. Aims to reduce GC pressure from frequent large allocations.
3.  **Pinned Managed Arrays (`fixed` keyword)**:
    - Creation: `new byte[HEIGHT, WIDTH]`
    - Processing: Arrays are pinned in memory using the `fixed` statement, allowing safe direct pointer access. This prevents the GC from moving the array during pointer operations.
    - Cleanup: Handled by the Garbage Collector after unpinning.
4.  **IDisposable-Wrapped Unmanaged Memory (`UnmanagedBitmap` class)**:
    - Creation: `Marshal.AllocHGlobal()` wrapped in an `IDisposable` class (`UnmanagedBitmap`).
    - Processing: Direct pointer access (`byte*`).
    - Cleanup: `Marshal.FreeHGlobal()` called via the `Dispose()` method (typically in a `using` block). Finalizer provides a safety net.
5.  **Raw Unmanaged Memory (manual `Marshal.AllocHGlobal`)**:
    - Creation: `Marshal.AllocHGlobal()`.
    - Processing: Direct pointer access (`byte*`).
    - Cleanup: Requires explicit `Marshal.FreeHGlobal()` calls.

## 📋 Prerequisites

- **.NET SDK** (6.0 or later recommended, due to features like `GCSettings` and modern C# syntax).
- **Optional**: A memory profiler such as JetBrains dotMemory or Visual Studio Diagnostic Tools for in-depth memory analysis.
- **RAM**: At least 8GB recommended due to the large array sizes (`10000x7000` pixels is approx. 66.7MB per array, and multiple arrays are used).

## 💻 Installation

1.  Clone the repository:

    ```bash
    git clone <repository-url>
    cd <repository-directory>
    ```

2.  Build the C# application:
    ```bash
    dotnet build -c Release
    ```
    The executable will typically be in `bin/Release/netX.0/YourProjectName.exe`.

## 🚀 Usage

1.  Open your preferred memory profiler (e.g., JetBrains dotMemory, Visual Studio Profiler) if you intend to perform detailed memory analysis.
2.  Run the application from the command line:
    ```bash
    dotnet run --project YourProjectName.csproj -c Release
    ```
    Or directly execute the compiled `.exe` file.
3.  The application will first print current GC settings and its Process ID.
4.  It will then pause, prompting you to attach your profiler to the displayed Process ID.
5.  Once attached, press any key in the console to continue execution.
6.  The application will run through `NUM_ITERATIONS` (default 5) for each of the five memory management techniques.
7.  Between each set of tests (e.g., after all "Original Managed Memory" tests), it will pause again, prompting for a memory snapshot. This allows you to compare memory states.
8.  Observe the console output for timing results for each technique.
9.  After all tests complete, the application will wait for a key press to exit.

## 🔄 How It Works

The `Main` method in `BitmapProcessor.cs` orchestrates the benchmark:

1.  **Initial Setup**:
    - `GCSettingsPrinter.PrintCurrentGCSettings()` displays GC information.
    - The program prints its process ID and pauses for profiler attachment.
    - Constants like `IMAGE_HEIGHT`, `IMAGE_WIDTH`, `KERNEL_SIZE`, and `NUM_ITERATIONS` define the workload.
2.  **Benchmarking Loop**: For each of the five memory management techniques:
    - A loop runs for `NUM_ITERATIONS`.
    - In each iteration:
      - An input "image" (2D byte array or unmanaged equivalent) is created and filled with a pattern.
      - A `Stopwatch` measures the time taken to apply a convolution kernel to the input image, producing an output image.
      - The input and output images are then made eligible for garbage collection (or explicitly freed/returned to pool).
    - Total and average processing times for the technique are printed.
3.  **Memory Profiling Points**:
    - `ForceGC()` is called after each set of tests for a technique to attempt to establish a cleaner baseline for subsequent profiler snapshots (its effectiveness can vary).
    - `PauseForSnapshot()` prompts the user to take a memory snapshot in their profiler.
4.  **Bitmap Operations**:
    - `CreateAndFill...` methods: Allocate and initialize the 2D byte arrays.
    - `ApplyKernelTo...` methods: Perform a convolution using a pre-computed `5x5` kernel. This operation is parallelized using `Parallel.For`.

## 📊 Analyzing Results

### Console Output:

- **Timing**: Pay attention to the "Total ... processing time" and "Avg: ... ms/iter" for each technique. This gives a direct performance comparison for the CPU-bound kernel application task.

### Memory Profiler (e.g., dotMemory):

When comparing snapshots taken at `PauseForSnapshot` points:

1.  **Memory Allocation Patterns**:
    - Observe the number and size of allocated `byte[,]` objects for managed techniques.
    - For pooled techniques, see if the pool (`ConcurrentBag`) effectively reuses arrays and how many new allocations still occur.
    - For unmanaged techniques, check if `Marshal.AllocHGlobal` and `Marshal.FreeHGlobal` (or `Dispose`) are balanced. Look for unmanaged memory leaks if `FreeHGlobal` is missed.
2.  **Garbage Collection (GC) Impact**:
    - **GC Pressure**: Techniques creating many short-lived large managed objects (like "Original Managed") will likely trigger more GCs, especially Gen2 collections for LOH objects.
    - **Pause Times**: Correlate GC events with application pauses.
    - **Heap Fragmentation**: Particularly relevant for the Large Object Heap (LOH) where `byte[,]` of this size reside. Pooling or unmanaged memory can mitigate LOH fragmentation.
3.  **Object Lifecycles**:
    - **Pooled Objects**: Track objects in `FixedSizeBitmapPool` to ensure they are returned and reused.
    - **IDisposable Objects**: Ensure `UnmanagedBitmap.Dispose()` is called, preventing finalizer-dependent cleanup which is less efficient.
4.  **CPU Usage**:
    - Compare CPU time spent in application code versus GC. Techniques that reduce GC pressure should show a higher proportion of CPU time spent in application logic.

## 🔬 Expected Findings

Generally, you might observe:

- **Standard Managed**: Potentially highest GC pressure due to frequent large allocations and deallocations on the LOH. Might show good performance if GC pauses are not dominant.
- **Pooled Managed**: Reduced GC pressure and fewer allocations if the pool is effectively utilized. Can improve performance by avoiding repeated allocation costs.
- **Pinned Managed**: Performance might be similar to standard managed for the computation itself. Pinning adds a small overhead but ensures direct pointer access is safe if needed for interop or specific optimizations (though in this pure C# kernel, direct array access is often optimized well by the JIT). The main benefit here is enabling `byte*` access to managed arrays.
- **IDisposable Unmanaged & Raw Unmanaged**: Lowest GC impact as memory is outside the GC heap. Performance can be very good, but carries the responsibility of manual memory management. `IDisposable` makes this safer. Potential for memory leaks if not handled correctly (especially with raw pointers).
- The `Parallel.For` ensures the processing part is CPU-bound, making memory allocation/deallocation strategies more prominent in overall performance differences.

## 🛠️ Customization

You can modify the following constants in `BitmapProcessor.cs` to change the workload:

```csharp
// In BitmapProcessor.cs
public const int IMAGE_HEIGHT = 10000; // Adjust rows
public const int IMAGE_WIDTH = 7000;  // Adjust columns
public const int KERNEL_SIZE = 5;     // Adjust kernel dimensions (note: kernel logic is hardcoded for 5x5 in terms of center)
// In Main method
const int NUM_ITERATIONS = 5;        // Adjust number of test iterations

// In FixedSizeBitmapPool.cs
private const int MAX_POOLED_ARRAYS = 10; // Max arrays to keep in pool
```
