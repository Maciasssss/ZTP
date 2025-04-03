using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // Constants for matrix size
    const int LARGE_ROWS = 15000;
    const int LARGE_COLS = 15000;
    const int SMALL_ROWS = 5;
    const int SMALL_COLS = 5;

    static void Main(string[] args)
    {
        Console.WriteLine("GC Configuration Test - Simplified Version for dotMemory Profiling");
        Console.WriteLine("====================================================================");
        Process currentProcess = Process.GetCurrentProcess();
        Console.WriteLine($"\nCURRENT PROCESS ID: {currentProcess.Id}");
        Console.WriteLine("Press ESC to continue");
        ConsoleKeyInfo keyInfo;
        do
        {
            keyInfo = Console.ReadKey(true); 
        } while (keyInfo.Key != ConsoleKey.Escape);
        // Display test information
        string testName = "Standard Test with Current GC Settings";
        Console.WriteLine($"\nRunning test: {testName}");

        // Display current GC configuration
        DisplayGCConfiguration();

        // Measure memory before test
        DisplayMemoryUsage("Before test");

        // Run matrix operations
        Console.WriteLine("\n--- Matrix Multiplication Test ---");
        BasicMatrixMultiplication();

        // Display memory usage after matrix operations
        DisplayMemoryUsage("After matrix multiplication");

        // Run bitmap processing
        Console.WriteLine("\n--- Bitmap Processing Test ---");
        UnmanagedBitmapProcessing();

        // Display memory usage after all operations
        DisplayMemoryUsage("After all operations");

        // Wait for key press before exit
        Console.WriteLine("\nTest completed. Press any key to exit...");
        Console.ReadKey();
    }

    static void DisplayGCConfiguration()
    {
        Console.WriteLine("Current Garbage Collector Configuration:");
        Console.WriteLine($"- GC Mode: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
        Console.WriteLine($"- Concurrency Mode: {GCSettings.LatencyMode}");
        Console.WriteLine($"- Large Object Heap Compaction Mode: {GCSettings.LargeObjectHeapCompactionMode}");
        Console.WriteLine($"- Logical Processors: {Environment.ProcessorCount}");

        // Display relevant environment variables if set
        string[] gcEnvVars = {
            "DOTNET_GCServer", 
            "DOTNET_GCHeapHardLimit", 
            "DOTNET_GCHeapHardLimitPercent",
            "DOTNET_GCHeapHardLimitSOH",
            "DOTNET_GCHeapHardLimitLOH",
            "DOTNET_GCHighMemPercent",
            "DOTNET_GCLOHThreshold",
            "DOTNET_GCConserveMemory",
            "DOTNET_GCSustainedLowLatency",
            "DOTNET_GCLargeObjectHeapCompactionMode"
        };

        Console.WriteLine("\nActive GC Environment Variables:");
        bool foundAny = false;
        
        foreach (string varName in gcEnvVars)
        {
            string value = Environment.GetEnvironmentVariable(varName);
            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"- {varName} = {value}");
                foundAny = true;
            }
        }
        
        if (!foundAny)
        {
            Console.WriteLine("- No GC environment variables set");
        }
    }

    static void DisplayMemoryUsage(string label)
    {
        // Force collection for more accurate reporting
        GC.Collect(0, GCCollectionMode.Forced);

        // Get process memory information
        Process currentProcess = Process.GetCurrentProcess();

        Console.WriteLine($"\nMemory Usage ({label}):");
        Console.WriteLine($"- Managed Memory: {GC.GetTotalMemory(false) / 1024 / 1024:N2} MB");
        Console.WriteLine($"- Process Private Memory: {currentProcess.PrivateMemorySize64 / 1024 / 1024:N2} MB");
        Console.WriteLine($"- Working Set: {currentProcess.WorkingSet64 / 1024 / 1024:N2} MB");
        Console.WriteLine($"- Virtual Memory: {currentProcess.VirtualMemorySize64 / 1024 / 1024:N2} MB");

        // GC Collection information
        Console.WriteLine("- GC Collections:");
        Console.WriteLine($"  - Generation 0: {GC.CollectionCount(0)}");
        Console.WriteLine($"  - Generation 1: {GC.CollectionCount(1)}");
        Console.WriteLine($"  - Generation 2: {GC.CollectionCount(2)}");
    }

    static void BasicMatrixMultiplication()
    {
        Console.WriteLine("Initializing matrices...");

        // Initialize matrices
        double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
        double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];

        // Fill matrices with test data
        FillMatrix(largeMatrix);
        FillMatrix(smallMatrix);

        Console.WriteLine($"Large matrix size: {LARGE_ROWS}x{LARGE_COLS}");
        Console.WriteLine($"Small matrix size: {SMALL_ROWS}x{SMALL_COLS}");

        // Multiply matrices with time measurement
        Stopwatch sw = Stopwatch.StartNew();

        double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);

        sw.Stop();

        Console.WriteLine($"Matrix multiplication time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Result matrix size: {resultMatrix.GetLength(0)}x{resultMatrix.GetLength(1)}");
    }

    static void FillMatrix(double[,] matrix)
    {
        Random rand = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                matrix[i, j] = rand.NextDouble() * 10;
            }
        }
    }

    static double[,] MultiplyMatrixPart(double[,] largeMatrix, double[,] smallMatrix)
    {
        int largeRows = largeMatrix.GetLength(0);
        int largeCols = largeMatrix.GetLength(1);
        int smallSize = smallMatrix.GetLength(0);

        // Dimensions of result matrix after sliding window
        int resultRows = largeRows - smallSize + 1;
        int resultCols = largeCols - smallSize + 1;

        double[,] result = new double[resultRows, resultCols];

        // Slide window across large matrix
        for (int i = 0; i < resultRows; i++)
        {
            for (int j = 0; j < resultCols; j++)
            {
                double sum = 0;

                // Multiply each smallSize x smallSize element with corresponding values in large matrix
                for (int x = 0; x < smallSize; x++)
                {
                    for (int y = 0; y < smallSize; y++)
                    {
                        sum += largeMatrix[i + x, j + y] * smallMatrix[x, y];
                    }
                }

                result[i, j] = sum;
            }
        }

        return result;
    }

    static void UnmanagedBitmapProcessing()
    {
        Console.WriteLine("Creating test bitmap...");

        // Create test bitmap
        int width = 1000;
        int height = 800;

        // Create filter (represented as small matrix)
        double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
        FillMatrix(filter);

        // Normalize filter (so sum of elements equals 1)
        NormalizeFilter(filter);

        Console.WriteLine("Processing bitmap in unmanaged memory...");

        Stopwatch sw = Stopwatch.StartNew();

        // Create bitmap in unmanaged memory
        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            // Fill bitmap with test data
            FillRandomBitmap(bitmap);

            // Apply filter to bitmap using direct memory access
            ApplyFilterUnmanaged(bitmap, filter);
        }

        sw.Stop();

        Console.WriteLine($"Bitmap processing time in unmanaged memory: {sw.ElapsedMilliseconds} ms");
    }

    static void NormalizeFilter(double[,] filter)
    {
        double sum = 0;

        // Calculate sum of all elements
        for (int i = 0; i < filter.GetLength(0); i++)
        {
            for (int j = 0; j < filter.GetLength(1); j++)
            {
                sum += filter[i, j];
            }
        }

        // Normalize (if sum != 0)
        if (Math.Abs(sum) > 0.0001)
        {
            for (int i = 0; i < filter.GetLength(0); i++)
            {
                for (int j = 0; j < filter.GetLength(1); j++)
                {
                    filter[i, j] /= sum;
                }
            }
        }
    }

    static void FillRandomBitmap(Bitmap bitmap)
    {
        Random rand = new Random(42); // Fixed seed for reproducibility

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                int r = rand.Next(256);
                int g = rand.Next(256);
                int b = rand.Next(256);
                bitmap.SetPixel(x, y, Color.FromArgb(255, r, g, b));
            }
        }
    }

    static void ApplyFilterUnmanaged(Bitmap bitmap, double[,] filter)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Lock bitmap in memory
        BitmapData bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadWrite,
            bitmap.PixelFormat);

        try
        {
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int stride = bitmapData.Stride;
            IntPtr scan0 = bitmapData.Scan0;

            unsafe
            {
                // Create copy of image for processing
                byte[] inputBuffer = new byte[stride * height];
                Marshal.Copy(scan0, inputBuffer, 0, inputBuffer.Length);

                // Output buffer
                byte[] outputBuffer = new byte[stride * height];

                // Center part of filter
                int filterCenterX = filter.GetLength(1) / 2;
                int filterCenterY = filter.GetLength(0) / 2;

                // Process each pixel
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Base pixel index
                        int pixelOffset = y * stride + x * bytesPerPixel;

                        // Filters for each channel
                        double sumB = 0;
                        double sumG = 0;
                        double sumR = 0;

                        // Apply filter
                        for (int fy = 0; fy < filter.GetLength(0); fy++)
                        {
                            for (int fx = 0; fx < filter.GetLength(1); fx++)
                            {
                                // Image sampling position
                                int sampleY = y + fy - filterCenterY;
                                int sampleX = x + fx - filterCenterX;

                                // Wrap edges
                                sampleY = Math.Max(0, Math.Min(height - 1, sampleY));
                                sampleX = Math.Max(0, Math.Min(width - 1, sampleX));

                                // Sampled pixel index
                                int sampleOffset = sampleY * stride + sampleX * bytesPerPixel;

                                // Sample pixel and apply filter coefficient
                                double filterValue = filter[fy, fx];
                                sumB += inputBuffer[sampleOffset] * filterValue;
                                sumG += inputBuffer[sampleOffset + 1] * filterValue;
                                sumR += inputBuffer[sampleOffset + 2] * filterValue;
                            }
                        }

                        // Save filtered values to output buffer
                        outputBuffer[pixelOffset] = (byte)Math.Max(0, Math.Min(255, sumB));
                        outputBuffer[pixelOffset + 1] = (byte)Math.Max(0, Math.Min(255, sumG));
                        outputBuffer[pixelOffset + 2] = (byte)Math.Max(0, Math.Min(255, sumR));
                        outputBuffer[pixelOffset + 3] = inputBuffer[pixelOffset + 3];
                    }
                });

                // Copy processed data back to bitmap
                Marshal.Copy(outputBuffer, 0, scan0, outputBuffer.Length);
            }
        }
        finally
        {
            // Unlock bitmap
            bitmap.UnlockBits(bitmapData);
        }
    }
}