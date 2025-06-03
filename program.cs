using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Threading.Tasks;
using System.Threading; // For Thread.Sleep

public static class GCSettingsPrinter
{
    public static void PrintCurrentGCSettings()
    {
        Console.WriteLine("\n--- Current GC Settings ---");
        Console.WriteLine($"GC Server Mode: {GCSettings.IsServerGC}");
        Console.WriteLine($"GC Latency Mode: {GCSettings.LatencyMode}");
        if (GCSettings.IsServerGC)
        {
            // These are harder to get directly without more complex means or specific .NET versions/APIs
            // For .NET Core/.NET 5+, DOTNET_GCHeapCount is a primary way to control this for Server GC
            Console.WriteLine($"Note: For Server GC, heap count is often related to logical processor count or DOTNET_GCHeapCount.");
        }
        Console.WriteLine($"Large Object Heap Threshold (GCSettings.LargeObjectHeapCompactionMode): {GCSettings.LargeObjectHeapCompactionMode}");
        // Note: DOTNET_GCLOHThreshold directly sets the LOH threshold but isn't exposed via a simple GCSettings property.
        // Its effect will be seen in memory profiling (objects > threshold on LOH).
        Console.WriteLine($"DOTNET_GCLOHThreshold (if set): {Environment.GetEnvironmentVariable("DOTNET_GCLOHThreshold") ?? "Not Set or N/A"}");
        Console.WriteLine($"DOTNET_GCHeapHardLimitPercent (if set): {Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimitPercent") ?? "Not Set or N/A"}");
        Console.WriteLine($"DOTNET_GCConserveMemory (if set): {Environment.GetEnvironmentVariable("DOTNET_GCConserveMemory") ?? "Not Set or N/A"}");
        Console.WriteLine($"DOTNET_GCConcurrent (if set, for Workstation GC): {Environment.GetEnvironmentVariable("DOTNET_GCConcurrent") ?? "Not Set or N/A"}");
        Console.WriteLine($"DOTNET_GCCpuGroup (if set): {Environment.GetEnvironmentVariable("DOTNET_GCCpuGroup") ?? "Not Set or N/A"}");
        Console.WriteLine("---------------------------\n");
    }
}

public static class FixedSizeBitmapPool
{
    private static readonly ConcurrentBag<byte[,]> _pool = new ConcurrentBag<byte[,]>();
    private const int MAX_POOLED_ARRAYS = 10; // Max arrays to keep in pool

    public static byte[,] Rent() // Removed size parameters as they are fixed
    {
        if (_pool.TryTake(out byte[,] array))
        {
            // Console.WriteLine("Reusing pooled managed matrix.");
            return array;
        }
        // Console.WriteLine($"Pool empty. Creating new managed matrix {BitmapProcessor.IMAGE_HEIGHT}x{BitmapProcessor.IMAGE_WIDTH} for pooling system.");
        return new byte[BitmapProcessor.IMAGE_HEIGHT, BitmapProcessor.IMAGE_WIDTH];
    }

    public static void Return(byte[,] array)
    {
        if (array == null) return;

        // Basic check, though dimensions should always match now
        if (array.GetLength(0) != BitmapProcessor.IMAGE_HEIGHT || array.GetLength(1) != BitmapProcessor.IMAGE_WIDTH)
        {
            Console.WriteLine($"Warning: Attempted to return array of incorrect size to pool. Discarding.");
            return;
        }

        if (_pool.Count < MAX_POOLED_ARRAYS)
        {
            _pool.Add(array);
            // Console.WriteLine("Returned managed matrix to pool.");
        }
        // else: Pool is full, let GC handle this array
    }
}

public unsafe class UnmanagedBitmap : IDisposable
{
    public IntPtr Handle { get; private set; }
    public byte* DataPtr { get; private set; }
    public int Rows => BitmapProcessor.IMAGE_HEIGHT; // Fixed size
    public int Cols => BitmapProcessor.IMAGE_WIDTH;  // Fixed size
    private bool _disposed = false;

    public UnmanagedBitmap()
    {
        long sizeInBytes = (long)Rows * Cols;
        Handle = Marshal.AllocHGlobal(new IntPtr(sizeInBytes));
        DataPtr = (byte*)Handle.ToPointer();
    }

    public void FillDataPattern()
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                DataPtr[i * Cols + j] = (byte)((i + j * 2) % 256);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (Handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Handle);
                Handle = IntPtr.Zero;
                DataPtr = null;
            }
            _disposed = true;
        }
    }

    ~UnmanagedBitmap()
    {
        Dispose(false);
    }
}

public class BitmapProcessor
{
    public const int IMAGE_HEIGHT = 10000;
    public const int IMAGE_WIDTH = 7000;
    public const int KERNEL_SIZE = 5;

    // Pre-compute the kernel as it's constant
    private static readonly float[,] _kernel = CreateStaticKernel();

    private static float[,] CreateStaticKernel()
    {
        float[,] kernel = new float[KERNEL_SIZE, KERNEL_SIZE];
        float value = 1.0f / (KERNEL_SIZE * KERNEL_SIZE);
        for (int i = 0; i < KERNEL_SIZE; i++)
        {
            for (int j = 0; j < KERNEL_SIZE; j++)
            {
                kernel[i, j] = value;
            }
        }
        return kernel;
    }

    // --- Original Methods (Untouched logic, using fixed sizes) ---
    public static byte[,] CreateAndFillManagedMatrix()
    {
        // Console.WriteLine($"Creating managed matrix {IMAGE_HEIGHT}x{IMAGE_WIDTH}...");
        byte[,] matrix = new byte[IMAGE_HEIGHT, IMAGE_WIDTH];
        for (int i = 0; i < IMAGE_HEIGHT; i++)
        {
            for (int j = 0; j < IMAGE_WIDTH; j++)
            {
                matrix[i, j] = (byte)((i + j * 2) % 256);
            }
        }
        // Console.WriteLine("Managed matrix created.");
        return matrix;
    }

    public static unsafe IntPtr CreateAndFillUnmanagedMatrixRaw(out byte* dataPtr)
    {
        // Console.WriteLine($"Creating unmanaged matrix {IMAGE_HEIGHT}x{IMAGE_WIDTH}...");
        long sizeInBytes = (long)IMAGE_HEIGHT * IMAGE_WIDTH;
        IntPtr ptr = Marshal.AllocHGlobal(new IntPtr(sizeInBytes));
        dataPtr = (byte*)ptr.ToPointer();

        for (int i = 0; i < IMAGE_HEIGHT; i++)
        {
            for (int j = 0; j < IMAGE_WIDTH; j++)
            {
                dataPtr[i * IMAGE_WIDTH + j] = (byte)((i + j * 2) % 256);
            }
        }
        // Console.WriteLine("Unmanaged matrix created.");
        return ptr;
    }

    public static byte[,] ApplyKernelToManaged(byte[,] image)
    {
        const int kernelCenterY = KERNEL_SIZE / 2;
        const int kernelCenterX = KERNEL_SIZE / 2;
        byte[,] result = new byte[IMAGE_HEIGHT, IMAGE_WIDTH];

        Parallel.For(0, IMAGE_HEIGHT, r =>
        {
            for (int c = 0; c < IMAGE_WIDTH; c++)
            {
                float sum = 0;
                for (int kr = 0; kr < KERNEL_SIZE; kr++)
                {
                    for (int kc = 0; kc < KERNEL_SIZE; kc++)
                    {
                        int pixelY = r + kr - kernelCenterY;
                        int pixelX = c + kc - kernelCenterX;
                        pixelY = Math.Max(0, Math.Min(pixelY, IMAGE_HEIGHT - 1));
                        pixelX = Math.Max(0, Math.Min(pixelX, IMAGE_WIDTH - 1));
                        sum += image[pixelY, pixelX] * _kernel[kr, kc];
                    }
                }
                result[r, c] = (byte)Math.Max(0, Math.Min(255, Math.Round(sum)));
            }
        });
        return result;
    }

    public static unsafe IntPtr ApplyKernelToUnmanagedRaw(byte* inputImagePtr, out byte* outParamOutputImagePtr)
    {
        const int kernelCenterY = KERNEL_SIZE / 2;
        const int kernelCenterX = KERNEL_SIZE / 2;
        long sizeInBytes = (long)IMAGE_HEIGHT * IMAGE_WIDTH;
        IntPtr outputMemoryHandle = Marshal.AllocHGlobal(new IntPtr(sizeInBytes));
        byte* localOutputImagePtr = (byte*)outputMemoryHandle.ToPointer();
        outParamOutputImagePtr = localOutputImagePtr;

        Parallel.For(0, IMAGE_HEIGHT, r =>
        {
            for (int c = 0; c < IMAGE_WIDTH; c++)
            {
                float sum = 0;
                for (int kr = 0; kr < KERNEL_SIZE; kr++)
                {
                    for (int kc = 0; kc < KERNEL_SIZE; kc++)
                    {
                        int pixelY = r + kr - kernelCenterY;
                        int pixelX = c + kc - kernelCenterX;
                        pixelY = Math.Max(0, Math.Min(pixelY, IMAGE_HEIGHT - 1));
                        pixelX = Math.Max(0, Math.Min(pixelX, IMAGE_WIDTH - 1));
                        sum += inputImagePtr[pixelY * IMAGE_WIDTH + pixelX] * _kernel[kr, kc];
                    }
                }
                localOutputImagePtr[r * IMAGE_WIDTH + c] = (byte)Math.Max(0, Math.Min(255, Math.Round(sum)));
            }
        });
        return outputMemoryHandle;
    }

    // --- Optimized Methods (Using fixed sizes and pre-computed kernel) ---

    public static byte[,] CreateAndFillManagedMatrix_Pooled()
    {
        byte[,] matrix = FixedSizeBitmapPool.Rent();
        for (int i = 0; i < IMAGE_HEIGHT; i++)
        {
            for (int j = 0; j < IMAGE_WIDTH; j++)
            {
                matrix[i, j] = (byte)((i + j * 2) % 256);
            }
        }
        return matrix;
    }

    public static byte[,] ApplyKernelToManaged_Pooled(byte[,] image)
    {
        const int kernelCenterY = KERNEL_SIZE / 2;
        const int kernelCenterX = KERNEL_SIZE / 2;
        byte[,] result = FixedSizeBitmapPool.Rent();
        Parallel.For(0, IMAGE_HEIGHT, r =>
        {
            for (int c = 0; c < IMAGE_WIDTH; c++)
            {
                float sum = 0;
                for (int kr = 0; kr < KERNEL_SIZE; kr++)
                {
                    for (int kc = 0; kc < KERNEL_SIZE; kc++)
                    {
                        int pixelY = r + kr - kernelCenterY;
                        int pixelX = c + kc - kernelCenterX;
                        pixelY = Math.Max(0, Math.Min(pixelY, IMAGE_HEIGHT - 1));
                        pixelX = Math.Max(0, Math.Min(pixelX, IMAGE_WIDTH - 1));
                        sum += image[pixelY, pixelX] * _kernel[kr, kc];
                    }
                }
                result[r, c] = (byte)Math.Max(0, Math.Min(255, Math.Round(sum)));
            }
        });
        return result;
    }

    public static unsafe byte[,] ApplyKernelToManaged_Pinned(byte[,] image)
    {
        const int kernelCenterY = KERNEL_SIZE / 2;
        const int kernelCenterX = KERNEL_SIZE / 2;
        byte[,] result = new byte[IMAGE_HEIGHT, IMAGE_WIDTH];

        fixed (byte* inputImagePtrFixed = image)
        fixed (byte* outputImagePtrFixed = result)
        {
            byte* inputImagePtr = inputImagePtrFixed;
            byte* outputImagePtr = outputImagePtrFixed;
            Parallel.For(0, IMAGE_HEIGHT, r =>
            {
                for (int c = 0; c < IMAGE_WIDTH; c++)
                {
                    float sum = 0;
                    for (int kr = 0; kr < KERNEL_SIZE; kr++)
                    {
                        for (int kc = 0; kc < KERNEL_SIZE; kc++)
                        {
                            int pixelY = r + kr - kernelCenterY;
                            int pixelX = c + kc - kernelCenterX;
                            pixelY = Math.Max(0, Math.Min(pixelY, IMAGE_HEIGHT - 1));
                            pixelX = Math.Max(0, Math.Min(pixelX, IMAGE_WIDTH - 1));
                            sum += inputImagePtr[pixelY * IMAGE_WIDTH + pixelX] * _kernel[kr, kc];
                        }
                    }
                    outputImagePtr[r * IMAGE_WIDTH + c] = (byte)Math.Max(0, Math.Min(255, Math.Round(sum)));
                }
            });
        }
        return result;
    }

    public static UnmanagedBitmap CreateAndFillUnmanagedMatrix_Disposable()
    {
        var unmanagedBitmap = new UnmanagedBitmap();
        unmanagedBitmap.FillDataPattern();
        return unmanagedBitmap;
    }

    public static unsafe UnmanagedBitmap ApplyKernelToUnmanaged_Disposable(UnmanagedBitmap inputImage)
    {

        byte* inputImagePtr = inputImage.DataPtr;
        const int kernelCenterY = KERNEL_SIZE / 2;
        const int kernelCenterX = KERNEL_SIZE / 2;
        var outputImage = new UnmanagedBitmap();
        byte* outputImagePtr = outputImage.DataPtr;

        Parallel.For(0, IMAGE_HEIGHT, r =>
        {
            for (int c = 0; c < IMAGE_WIDTH; c++)
            {
                float sum = 0;
                for (int kr = 0; kr < KERNEL_SIZE; kr++)
                {
                    for (int kc = 0; kc < KERNEL_SIZE; kc++)
                    {
                        int pixelY = r + kr - kernelCenterY;
                        int pixelX = c + kc - kernelCenterX;
                        pixelY = Math.Max(0, Math.Min(pixelY, IMAGE_HEIGHT - 1));
                        pixelX = Math.Max(0, Math.Min(pixelX, IMAGE_WIDTH - 1));
                        sum += inputImagePtr[pixelY * IMAGE_WIDTH + pixelX] * _kernel[kr, kc];
                    }
                }
                outputImagePtr[r * IMAGE_WIDTH + c] = (byte)Math.Max(0, Math.Min(255, Math.Round(sum)));
            }
        });
        return outputImage;
    }

    public static void ForceGC()
    {
        // Console.WriteLine("\nForcing GC Collect...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        // Console.WriteLine("GC Collect complete.");
    }

    public static void PauseForSnapshot(string message)
    {
        Console.WriteLine($"\n>>> PAUSING: {message}. Press any key to continue (or take a snapshot)...");
        Console.ReadKey(true);
        Console.WriteLine("Continuing...\n");
    }

    public static unsafe void Main(string[] args)
    {
        GCSettingsPrinter.PrintCurrentGCSettings(); // Print GC settings at start

        Console.WriteLine("Attach your debugger/profiler (e.g., dotMemory) now to process ID: " + Process.GetCurrentProcess().Id);
        Console.WriteLine("Press any key to continue execution...");
        Console.ReadKey(true);
        Console.WriteLine("Continuing execution...");

        Console.WriteLine($"Image Dimensions: {IMAGE_HEIGHT}x{IMAGE_WIDTH}");
        Console.WriteLine($"Kernel Size: {KERNEL_SIZE}x{KERNEL_SIZE}");
        Console.WriteLine($"Using Parallel.For: true");
        Console.WriteLine($"Estimated memory for one image: {(long)IMAGE_HEIGHT * IMAGE_WIDTH / (1024.0 * 1024.0):F2} MB");
        Console.WriteLine("-----------------------------------------------------\n");

        Stopwatch sw = new Stopwatch();
        const int NUM_ITERATIONS = 5;

        // --- Part 1: Original Managed Memory (byte[,]) ---
        Console.WriteLine($"Starting Original Managed Memory Processing ({NUM_ITERATIONS} iterations)...");
        long totalTimeOriginalManaged = 0;
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            // Console.WriteLine($"\n--- Original Managed Iteration {i + 1}/{NUM_ITERATIONS} ---");
            byte[,] managedImage = CreateAndFillManagedMatrix();
            sw.Restart();
            byte[,] managedResult = ApplyKernelToManaged(managedImage);
            sw.Stop();
            totalTimeOriginalManaged += sw.ElapsedMilliseconds;
            // Console.WriteLine($"Original Managed processing time (Iter {i + 1}): {sw.ElapsedMilliseconds} ms");
            managedImage = null;
            managedResult = null;
        }
        Console.WriteLine($"Total Original Managed processing time for {NUM_ITERATIONS} iterations: {totalTimeOriginalManaged} ms. Avg: {totalTimeOriginalManaged / (double)NUM_ITERATIONS:F2} ms/iter.");
        ForceGC();
        PauseForSnapshot("After Original Managed Memory tests");

        // --- Part A: Pooled Managed Memory ---
        Console.WriteLine($"\n\nStarting Pooled Managed Memory Processing ({NUM_ITERATIONS} iterations)...");
        long totalTimePooledManaged = 0;
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            // Console.WriteLine($"\n--- Pooled Managed Iteration {i + 1}/{NUM_ITERATIONS} ---");
            byte[,] pooledImage = CreateAndFillManagedMatrix_Pooled();
            sw.Restart();
            byte[,] pooledResult = ApplyKernelToManaged_Pooled(pooledImage);
            sw.Stop();
            totalTimePooledManaged += sw.ElapsedMilliseconds;
            // Console.WriteLine($"Pooled Managed processing time (Iter {i + 1}): {sw.ElapsedMilliseconds} ms");
            FixedSizeBitmapPool.Return(pooledImage);
            FixedSizeBitmapPool.Return(pooledResult);
        }
        Console.WriteLine($"Total Pooled Managed processing time for {NUM_ITERATIONS} iterations: {totalTimePooledManaged} ms. Avg: {totalTimePooledManaged / (double)NUM_ITERATIONS:F2} ms/iter.");
        ForceGC();
        PauseForSnapshot("After Pooled Managed Memory tests");

        // --- Part B: Pinned Managed Memory (using `fixed`) ---
        // Note: CreateAndFillManagedMatrix_ForPinning is same as CreateAndFillManagedMatrix
        Console.WriteLine($"\n\nStarting Pinned Managed Memory Processing ({NUM_ITERATIONS} iterations)...");
        long totalTimePinnedManaged = 0;
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            // Console.WriteLine($"\n--- Pinned Managed Iteration {i + 1}/{NUM_ITERATIONS} ---");
            byte[,] pinnedImageSource = CreateAndFillManagedMatrix(); // Same creation
            sw.Restart();
            byte[,] pinnedResult = ApplyKernelToManaged_Pinned(pinnedImageSource);
            sw.Stop();
            totalTimePinnedManaged += sw.ElapsedMilliseconds;
            // Console.WriteLine($"Pinned Managed processing time (Iter {i + 1}): {sw.ElapsedMilliseconds} ms");
            pinnedImageSource = null;
            pinnedResult = null;
        }
        Console.WriteLine($"Total Pinned Managed processing time for {NUM_ITERATIONS} iterations: {totalTimePinnedManaged} ms. Avg: {totalTimePinnedManaged / (double)NUM_ITERATIONS:F2} ms/iter.");
        ForceGC();
        PauseForSnapshot("After Pinned Managed Memory tests");

        // --- Part C: IDisposable for Unmanaged Memory ---
        Console.WriteLine($"\n\nStarting IDisposable Unmanaged Memory Processing ({NUM_ITERATIONS} iterations)...");
        long totalTimeDisposableUnmanaged = 0;
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            // Console.WriteLine($"\n--- IDisposable Unmanaged Iteration {i + 1}/{NUM_ITERATIONS} ---");
            using (UnmanagedBitmap disposableInputBitmap = CreateAndFillUnmanagedMatrix_Disposable())
            using (UnmanagedBitmap disposableOutputBitmap = ApplyKernelToUnmanaged_Disposable(disposableInputBitmap))
            {
                sw.Restart(); // Restart timer just before the operation if creation isn't part of the timed measure
                              // For this example, ApplyKernelToUnmanaged_Disposable includes output allocation.
                              // If we only want to time the kernel application part after allocation:
                              // UnmanagedBitmap input = Create...; UnmanagedBitmap output = new UnmanagedBitmap(); sw.Restart(); ApplyKernelLogic(input, output); sw.Stop();
                              // The current ApplyKernelToUnmanaged_Disposable already returns a new UnmanagedBitmap, so timing it is fine.
            }
            // The actual operation is within ApplyKernelToUnmanaged_Disposable
            // Let's re-time it more accurately:
            UnmanagedBitmap inputForTiming = null;
            UnmanagedBitmap outputForTiming = null;
            try
            {
                inputForTiming = CreateAndFillUnmanagedMatrix_Disposable();
                sw.Restart();
                outputForTiming = ApplyKernelToUnmanaged_Disposable(inputForTiming);
                sw.Stop();
                totalTimeDisposableUnmanaged += sw.ElapsedMilliseconds;
                // Console.WriteLine($"IDisposable Unmanaged processing time (Iter {i + 1}): {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                inputForTiming?.Dispose();
                outputForTiming?.Dispose();
            }
        }
        Console.WriteLine($"Total IDisposable Unmanaged processing time for {NUM_ITERATIONS} iterations: {totalTimeDisposableUnmanaged} ms. Avg: {totalTimeDisposableUnmanaged / (double)NUM_ITERATIONS:F2} ms/iter.");
        ForceGC(); // Force GC to ensure finalizers run if Dispose was missed (though `using` handles it)
        PauseForSnapshot("After IDisposable Unmanaged Memory tests");


        // --- Original Unmanaged Memory (Raw pointers) ---
        Console.WriteLine($"\n\nStarting Original Raw Unmanaged Memory Processing ({NUM_ITERATIONS} iterations)...");
        long totalTimeRawUnmanaged = 0;
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            // Console.WriteLine($"\n--- Original Raw Unmanaged Iteration {i + 1}/{NUM_ITERATIONS} ---");
            byte* unmanagedInputDataPtr;
            IntPtr unmanagedInputHandle = IntPtr.Zero;
            byte* unmanagedOutputDataPtr = null;
            IntPtr unmanagedOutputHandle = IntPtr.Zero;
            try
            {
                unmanagedInputHandle = CreateAndFillUnmanagedMatrixRaw(out unmanagedInputDataPtr);
                sw.Restart();
                unmanagedOutputHandle = ApplyKernelToUnmanagedRaw(unmanagedInputDataPtr, out unmanagedOutputDataPtr);
                sw.Stop();
                totalTimeRawUnmanaged += sw.ElapsedMilliseconds;
                // Console.WriteLine($"Original Raw Unmanaged processing time (Iter {i + 1}): {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                if (unmanagedInputHandle != IntPtr.Zero) Marshal.FreeHGlobal(unmanagedInputHandle);
                if (unmanagedOutputHandle != IntPtr.Zero) Marshal.FreeHGlobal(unmanagedOutputHandle);
            }
        }
        Console.WriteLine($"Total Original Raw Unmanaged processing time for {NUM_ITERATIONS} iterations: {totalTimeRawUnmanaged} ms. Avg: {totalTimeRawUnmanaged / (double)NUM_ITERATIONS:F2} ms/iter.");
        ForceGC();
        PauseForSnapshot("After Original Raw Unmanaged Memory tests");

        Console.WriteLine("\n-----------------------------------------------------\n");
        Console.WriteLine("All processing complete. Press any key to exit.");
        Console.ReadKey();
    }
}