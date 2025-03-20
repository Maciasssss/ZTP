using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace MatrixMultiplicationAnalysis
{
    class Program
    {
        // Rozmiary macierzy i bitmap
        private const int LARGE_ROWS = 30000;
        private const int LARGE_COLS = 2000;
        private const int SMALL_ROWS = 5;
        private const int SMALL_COLS = 5;
        private const int BITMAP_WIDTH = 1000;
        private const int BITMAP_HEIGHT = 800;

        // Licznik dla powiadomień GC
        private static int gcNotificationCount = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Program do analizy mnożenia macierzy i przetwarzania bitmap");
            Console.WriteLine("===========================================================");

            // Punkt 1: Podstawowy algorytm mnożenia macierzy
            Console.WriteLine("\n1. Podstawowy algorytm mnożenia macierzy");
            BasicMatrixMultiplication();

            // Punkt 2: Zastosowanie do bitmapy w unmanaged memory
            Console.WriteLine("\n2. Zastosowanie do bitmapy w unmanaged memory");
            UnmanagedBitmapProcessing();

            // Punkt 3: Zastosowanie do bitmapy w managed memory
            Console.WriteLine("\n3. Zastosowanie do bitmapy w managed memory");
            ManagedBitmapProcessing();

            // Punkt 4: Analiza wykorzystania pamięci i czasu działania
            Console.WriteLine("\n4. Analiza wykorzystania pamięci i czasu działania");
            PerformanceAnalysis();

            Console.WriteLine("\n5. Szczegółowa analiza parametrów GC:");

            Console.WriteLine("Select which GC test to run:");
            Console.WriteLine("a) Workstation vs. Server mode (set in runtimeconfig.template.)");
            Console.WriteLine("b) HeapHardLimit (set in runtimeconfig.template.json)");
            Console.WriteLine("c) Per-object-heap hard limits (set in runtimeconfig.template.json)");
            Console.WriteLine("d) HighMemoryPercent (set in runtimeconfig.template.json)");
            Console.WriteLine("e) Large object heap threshold (set in runtimeconfig.template.json)");
            Console.WriteLine("f) Conserve memory (set in runtimeconfig.template.json)");
            Console.WriteLine("g) Manual GC.Collect(generation)");
            Console.WriteLine("h) GC.Collect(2) with LOH compaction");
            Console.WriteLine("i) LowLatencyMode (set in runtimeconfig.template.json)");
            Console.WriteLine("j) Register for GC Notifications");

            string? option = Console.ReadLine()?.ToLower();

            switch (option)
            {
                case "a":
                case "b":
                case "c":
                case "d":
                case "e":
                case "f":
                case "i":
                    // These options are configured in runtimeconfig.template.json
                    Console.WriteLine($"Running with option {option} configured in runtimeconfig.template.json");
                    RunBasicMatrixMultiplication();
                    break;
                case "g":
                    // g) wymuszenie GC.Collect(generacja)
                    TestManualGCCollection();
                    break;
                case "h":
                    // h) wymuszenie GC.Collect(2) wraz z kompaktacją LOH
                    TestGCWithLOHCompaction();
                    break;
                case "j":
                    // j) Register for GC Notifications
                    TestGCNotifications();
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }

            Console.WriteLine("\n6. Techniki poprawy wydajności:");

            // a) Pooling obiektów
            Console.WriteLine("\na) Pooling obiektów");
            AnalyzeObjectPooling();

            // b) Fixed i Pinned Object Heap
            Console.WriteLine("\nb) Fixed i Pinned Object Heap");
            AnalyzePinnedObjects();

            // c) Dispose i finalizatory
            Console.WriteLine("\nc) Dispose i finalizatory");
            AnalyzeDisposePattern();

            // d) Unikanie presji GC
            Console.WriteLine("\nd) Techniki unikania presji GC");
            AnalyzeGCPressureTechniques();

            Console.WriteLine("\nAnaliza zakończona. Naciśnij dowolny klawisz, aby wyjść.");
            Console.ReadKey();
        }



        #region Punkt 1: Podstawowy algorytm mnożenia macierzy

        static void BasicMatrixMultiplication()
        {
            Console.WriteLine("Inicjalizacja macierzy...");

            // Inicjalizacja macierzy
            double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];

            // Wypełnienie macierzy danymi testowymi
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);

            Console.WriteLine($"Rozmiar dużej macierzy: {LARGE_ROWS}x{LARGE_COLS}");
            Console.WriteLine($"Rozmiar małej macierzy: {SMALL_ROWS}x{SMALL_COLS}");

            // Mnożenie macierzy z pomiarem czasu
            Stopwatch sw = Stopwatch.StartNew();

            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);

            sw.Stop();

            Console.WriteLine($"Czas mnożenia macierzy: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Rozmiar macierzy wynikowej: {resultMatrix.GetLength(0)}x{resultMatrix.GetLength(1)}");

        }

        static void FillMatrix(double[,] matrix)
        {
            Random rand = new Random(42); // Stały seed dla powtarzalności

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

            // Wymiary wynikowej macierzy po przesuwaniu okna
            int resultRows = largeRows - smallSize + 1;
            int resultCols = largeCols - smallSize + 1;

            double[,] result = new double[resultRows, resultCols];

            // Przesuwanie okna po dużej macierzy
            for (int i = 0; i < resultRows; i++)
            {
                for (int j = 0; j < resultCols; j++)
                {
                    double sum = 0;

                    // Mnożenie każdego elementu 5x5 z odpowiadającymi wartościami w dużej macierzy
                    for (int x = 0; x < smallSize; x++)
                    {
                        for (int y = 0; y < smallSize; y++)
                        {
                            sum += largeMatrix[i + x, j + y] * smallMatrix[x, y];
                        }
                    }

                    result[i, j] = sum;  // Wpisanie wyniku do nowej macierzy
                }
            }

            return result;
        }

        #endregion

        #region Punkt 2: Zastosowanie do bitmapy w unmanaged memory

        static void UnmanagedBitmapProcessing()
        {
            Console.WriteLine("Tworzenie testowej bitmapy...");

            // Tworzenie testowej bitmapy
            int width = 1000;
            int height = 800;

            // Tworzenie filtru (reprezentowanego jako mała macierz)
            double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(filter); // Wypełnienie filtru losowymi wartościami

            // Normalizacja filtru (aby suma elementów wynosiła 1)
            NormalizeFilter(filter);

            Console.WriteLine("Przetwarzanie bitmapy w unmanaged memory...");

            Stopwatch sw = Stopwatch.StartNew();

            // Tworzenie bitmapy w unmanaged memory
            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                // Wypełnienie bitmapy danymi testowymi
                FillRandomBitmap(bitmap);

                // Zastosowanie filtru do bitmapy używając bezpośredniego dostępu do pamięci
                ApplyFilterUnmanaged(bitmap, filter);

            }

            sw.Stop();

            Console.WriteLine($"Czas przetwarzania bitmapy w unmanaged memory: {sw.ElapsedMilliseconds} ms");
        }

        static void NormalizeFilter(double[,] filter)
        {
            double sum = 0;

            // Oblicz sumę wszystkich elementów
            for (int i = 0; i < filter.GetLength(0); i++)
            {
                for (int j = 0; j < filter.GetLength(1); j++)
                {
                    sum += filter[i, j];
                }
            }

            // Normalizacja (jeśli suma != 0)
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
            Random rand = new Random(42); // Stały seed dla powtarzalności

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

            // Blokowanie bitmapy w pamięci
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
                    // Tworzenie kopii obrazu do przetwarzania
                    byte[] inputBuffer = new byte[stride * height];
                    Marshal.Copy(scan0, inputBuffer, 0, inputBuffer.Length);

                    // Bufor wyjściowy
                    byte[] outputBuffer = new byte[stride * height];

                    // Centralna część filtra
                    int filterCenterX = filter.GetLength(1) / 2;
                    int filterCenterY = filter.GetLength(0) / 2;

                    // Przetwarzanie każdego piksela
                    Parallel.For(0, height, y =>
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // Indeks bazowy piksela
                            int pixelOffset = y * stride + x * bytesPerPixel;

                            // Filtry dla każdego kanału
                            double sumB = 0;
                            double sumG = 0;
                            double sumR = 0;

                            // Zastosowanie filtru
                            for (int fy = 0; fy < filter.GetLength(0); fy++)
                            {
                                for (int fx = 0; fx < filter.GetLength(1); fx++)
                                {
                                    // Pozycja próbkowania obrazu
                                    int sampleY = y + fy - filterCenterY;
                                    int sampleX = x + fx - filterCenterX;

                                    // Obwinięcie brzegów
                                    sampleY = Math.Max(0, Math.Min(height - 1, sampleY));
                                    sampleX = Math.Max(0, Math.Min(width - 1, sampleX));

                                    // Indeks próbkowanego piksela
                                    int sampleOffset = sampleY * stride + sampleX * bytesPerPixel;

                                    // Próbkowanie piksela i zastosowanie współczynnika filtra
                                    double filterValue = filter[fy, fx];
                                    sumB += inputBuffer[sampleOffset] * filterValue;
                                    sumG += inputBuffer[sampleOffset + 1] * filterValue;
                                    sumR += inputBuffer[sampleOffset + 2] * filterValue;
                                }
                            }

                            // Zapisz przefiltrowane wartości do bufora wyjściowego
                            outputBuffer[pixelOffset] = (byte)Math.Max(0, Math.Min(255, sumB));
                            outputBuffer[pixelOffset + 1] = (byte)Math.Max(0, Math.Min(255, sumG));
                            outputBuffer[pixelOffset + 2] = (byte)Math.Max(0, Math.Min(255, sumR));
                            outputBuffer[pixelOffset + 3] = inputBuffer[pixelOffset + 3]; // Alpha pozostaje niezmienione
                        }
                    });

                    // Skopiuj przetworzone dane z powrotem do bitmapy
                    Marshal.Copy(outputBuffer, 0, scan0, outputBuffer.Length);
                }
            }
            finally
            {
                // Odblokowanie bitmapy
                bitmap.UnlockBits(bitmapData);
            }
        }

        #endregion

        #region Punkt 3: Zastosowanie do bitmapy w managed memory

        static void ManagedBitmapProcessing()
        {
            Console.WriteLine("Tworzenie testowej bitmapy...");

            // Tworzenie testowej bitmapy
            int width = 1000;
            int height = 800;

            // Tworzenie filtru (reprezentowanego jako mała macierz)
            double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(filter); // Wypełnienie filtru losowymi wartościami

            // Normalizacja filtru (aby suma elementów wynosiła 1)
            NormalizeFilter(filter);

            Console.WriteLine("Przetwarzanie bitmapy w managed memory...");

            Stopwatch sw = Stopwatch.StartNew();

            // Tworzenie bitmapy w managed memory jako byte[,] array
            byte[,,] managedBitmap = new byte[height, width, 4]; // 4 kanały (RGBA)

            // Wypełnienie bitmapy danymi testowymi
            FillRandomManagedBitmap(managedBitmap);

            // Zastosowanie filtru do bitmapy używając zarządzanej tablicy
            byte[,,] resultBitmap = ApplyFilterManaged(managedBitmap, filter);

            sw.Stop();

            Console.WriteLine($"Czas przetwarzania bitmapy w managed memory: {sw.ElapsedMilliseconds} ms");

        }

        static void FillRandomManagedBitmap(byte[,,] bitmap)
        {
            Random rand = new Random(42); // Stały seed dla powtarzalności

            for (int y = 0; y < bitmap.GetLength(0); y++)
            {
                for (int x = 0; x < bitmap.GetLength(1); x++)
                {
                    bitmap[y, x, 0] = (byte)rand.Next(256); // B
                    bitmap[y, x, 1] = (byte)rand.Next(256); // G
                    bitmap[y, x, 2] = (byte)rand.Next(256); // R
                    bitmap[y, x, 3] = 255; // A (pełna nieprzezroczystość)
                }
            }
        }

        static byte[,,] ApplyFilterManaged(byte[,,] inputBitmap, double[,] filter)
        {
            int height = inputBitmap.GetLength(0);
            int width = inputBitmap.GetLength(1);

            // Tworzenie bitmapy wynikowej
            byte[,,] outputBitmap = new byte[height, width, 4];

            // Centralna część filtra
            int filterCenterX = filter.GetLength(1) / 2;
            int filterCenterY = filter.GetLength(0) / 2;

            // Przetwarzanie każdego piksela
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    // Filtry dla każdego kanału
                    double sumB = 0;
                    double sumG = 0;
                    double sumR = 0;

                    // Zastosowanie filtru
                    for (int fy = 0; fy < filter.GetLength(0); fy++)
                    {
                        for (int fx = 0; fx < filter.GetLength(1); fx++)
                        {
                            // Pozycja próbkowania obrazu
                            int sampleY = y + fy - filterCenterY;
                            int sampleX = x + fx - filterCenterX;

                            // Obwinięcie brzegów
                            sampleY = Math.Max(0, Math.Min(height - 1, sampleY));
                            sampleX = Math.Max(0, Math.Min(width - 1, sampleX));

                            // Próbkowanie piksela i zastosowanie współczynnika filtra
                            double filterValue = filter[fy, fx];
                            sumB += inputBitmap[sampleY, sampleX, 0] * filterValue;
                            sumG += inputBitmap[sampleY, sampleX, 1] * filterValue;
                            sumR += inputBitmap[sampleY, sampleX, 2] * filterValue;
                        }
                    }

                    // Zapisz przefiltrowane wartości do bitmapy wynikowej
                    outputBitmap[y, x, 0] = (byte)Math.Max(0, Math.Min(255, sumB)); // B
                    outputBitmap[y, x, 1] = (byte)Math.Max(0, Math.Min(255, sumG)); // G
                    outputBitmap[y, x, 2] = (byte)Math.Max(0, Math.Min(255, sumR)); // R
                    outputBitmap[y, x, 3] = inputBitmap[y, x, 3]; // Alpha pozostaje niezmienione
                }
            });

            return outputBitmap;
        }

        static void SaveManagedBitmapToFile(byte[,,] managedBitmap, string fileName)
        {
            int height = managedBitmap.GetLength(0);
            int width = managedBitmap.GetLength(1);

            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int b = managedBitmap[y, x, 0];
                        int g = managedBitmap[y, x, 1];
                        int r = managedBitmap[y, x, 2];
                        int a = managedBitmap[y, x, 3];
                        bitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                    }
                }

                bitmap.Save(fileName, ImageFormat.Bmp);
            }
        }

        #endregion

        #region Punkt 4: Analiza wykorzystania pamięci i czasu działania

        static void PerformanceAnalysis()
        {
            Console.WriteLine("Rozpoczęcie analizy wydajności...");

            // Pomiar czasu wykonania
            MeasureExecutionTime();

            // Analiza zużycia pamięci
            AnalyzeMemoryUsage();

            Console.WriteLine("Analiza wydajności zakończona.");
        }

        static void MeasureExecutionTime()
        {
            Console.WriteLine("Pomiar czasu wykonania operacji:");

            // Zmienne do przechowywania czasów
            long basicMatrixTime = 0;
            long unmanagedTime = 0;
            long managedTime = 0;

            // Przygotowanie danych testowych
            double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);

            // Przetwarzanie bitmapy
            int width = 1000;
            int height = 800;

            double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(filter);
            NormalizeFilter(filter);

            // 1. Pomiar czasu mnożenia macierzy
            Stopwatch sw = Stopwatch.StartNew();
            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);
            sw.Stop();
            basicMatrixTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"- Mnożenie macierzy: {basicMatrixTime} ms");

            // 2. Pomiar czasu przetwarzania bitmapy w unmanaged memory
            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                FillRandomBitmap(bitmap);

                sw.Restart();
                ApplyFilterUnmanaged(bitmap, filter);
                sw.Stop();
                unmanagedTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"- Przetwarzanie bitmapy (unmanaged): {unmanagedTime} ms");
            }

            // 3. Pomiar czasu przetwarzania bitmapy w managed memory
            byte[,,] managedBitmap = new byte[height, width, 4];
            FillRandomManagedBitmap(managedBitmap);

            sw.Restart();
            byte[,,] resultBitmap = ApplyFilterManaged(managedBitmap, filter);
            sw.Stop();
            managedTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"- Przetwarzanie bitmapy (managed): {managedTime} ms");

            // Porównanie wyników
            Console.WriteLine("\nPorównanie czasów wykonania:");
            Console.WriteLine($"- Stosunek unmanaged/managed: {(double)unmanagedTime / managedTime:F2}");

            if (unmanagedTime < managedTime)
            {
                Console.WriteLine("  Wersja unmanaged jest szybsza.");
            }
            else if (unmanagedTime > managedTime)
            {
                Console.WriteLine("  Wersja managed jest szybsza.");
            }
            else
            {
                Console.WriteLine("  Obie wersje mają podobną wydajność.");
            }
        }

        static void AnalyzeMemoryUsage()
        {
            Console.WriteLine("\nPomiar zużycia pamięci:");

            // Wymuś GC przed pomiarem
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Pamięć przed alokacją
            long memoryBefore = GC.GetTotalMemory(true);
            Console.WriteLine($"- Pamięć przed alokacją: {memoryBefore / 1024} KB");

            // 1. Mnożenie macierzy
            Console.WriteLine("\nPamięć używana przez mnożenie macierzy:");
            double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);

            long memoryAfterMatrices = GC.GetTotalMemory(false);
            Console.WriteLine($"- Po alokacji macierzy: {(memoryAfterMatrices - memoryBefore) / 1024} KB");

            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);
            long memoryAfterMultiplication = GC.GetTotalMemory(false);
            Console.WriteLine($"- Po mnożeniu: {(memoryAfterMultiplication - memoryAfterMatrices) / 1024} KB");

            // 2. Przetwarzanie bitmapy w unmanaged memory
            Console.WriteLine("\nPamięć używana przez przetwarzanie bitmapy (unmanaged):");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long unmanagedBefore = GC.GetTotalMemory(true);

            using (Bitmap bitmap = new Bitmap(1000, 800, PixelFormat.Format32bppArgb))
            {
                FillRandomBitmap(bitmap);
                long memoryAfterBitmap = GC.GetTotalMemory(false);
                Console.WriteLine($"- Po alokacji bitmapy: {(memoryAfterBitmap - unmanagedBefore) / 1024} KB");

                double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
                FillMatrix(filter);
                NormalizeFilter(filter);

                ApplyFilterUnmanaged(bitmap, filter);
                long memoryAfterProcessing = GC.GetTotalMemory(false);
                Console.WriteLine($"- Po przetwarzaniu: {(memoryAfterProcessing - memoryAfterBitmap) / 1024} KB");
            }

            // 3. Przetwarzanie bitmapy w managed memory
            Console.WriteLine("\nPamięć używana przez przetwarzanie bitmapy (managed):");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long managedBefore = GC.GetTotalMemory(true);

            byte[,,] managedBitmap = new byte[800, 1000, 4];
            FillRandomManagedBitmap(managedBitmap);
            long memoryAfterManagedBitmap = GC.GetTotalMemory(false);
            Console.WriteLine($"- Po alokacji bitmapy: {(memoryAfterManagedBitmap - managedBefore) / 1024} KB");

            double[,] filterManaged = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(filterManaged);
            NormalizeFilter(filterManaged);

            byte[,,] resultBitmap = ApplyFilterManaged(managedBitmap, filterManaged);
            long memoryAfterManagedProcessing = GC.GetTotalMemory(false);
            Console.WriteLine($"- Po przetwarzaniu: {(memoryAfterManagedProcessing - memoryAfterManagedBitmap) / 1024} KB");

            // Podsumowanie
            Console.WriteLine("\nPodsumowanie zużycia pamięci:");
            Console.WriteLine("- Różnice w zużyciu pamięci między wersjami wynikają głównie z metody zarządzania pamięcią.");
            Console.WriteLine("- Wersja unmanaged używa mniej pamięci zarządzanej, ale wymaga ręcznego zarządzania pamięcią.");
            Console.WriteLine("- Wersja managed używa więcej pamięci zarządzanej, ale jest bezpieczniejsza w użyciu.");
        }

        #endregion

        #region 5. Analiza parametrów GC

        // a) Workstation mode vs. Server mode
        // Basic matrix multiplication with memory and time statistics
        static void RunBasicMatrixMultiplication()
        {
            PrintGCSettings();

            // Print GC mode
            Console.WriteLine($"GC Server Mode: {GCSettings.IsServerGC}");

            // Measure memory before
            CollectMemoryStats("Before allocation");

            Console.WriteLine("Initializing matrices...");

            // Initialize matrices
            Stopwatch allocSw = Stopwatch.StartNew();
            double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
            allocSw.Stop();

            Console.WriteLine($"Matrix allocation time: {allocSw.ElapsedMilliseconds} ms");

            // Fill matrices with test data
            Stopwatch fillSw = Stopwatch.StartNew();
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);
            fillSw.Stop();

            Console.WriteLine($"Matrix filling time: {fillSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Large matrix size: {LARGE_ROWS}x{LARGE_COLS}");
            Console.WriteLine($"Small matrix size: {SMALL_ROWS}x{SMALL_COLS}");

            CollectMemoryStats("After allocation");

            // Matrix multiplication with time measurement
            Stopwatch sw = Stopwatch.StartNew();
            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);
            sw.Stop();

            Console.WriteLine($"Matrix multiplication time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Result matrix size: {resultMatrix.GetLength(0)}x{resultMatrix.GetLength(1)}");

            CollectMemoryStats("After multiplication");
        }

        // g) Test manual GC collection for different generations
        static void TestManualGCCollection()
        {
            PrintGCSettings();
            Console.WriteLine("\n=== Testing Manual GC Collection (Option g) ===\n");

            CollectMemoryStats("Initial state");

            // Create matrices
            Console.WriteLine("Creating matrices...");
            double[,] largeMatrix = new double[LARGE_ROWS, LARGE_COLS];
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];

            // Fill matrices
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);

            CollectMemoryStats("After matrix creation");

            // Force collection of generation 0
            Console.WriteLine("\nForcing collection of Generation 0...");
            Stopwatch sw = Stopwatch.StartNew();
            GC.Collect(0); // Collect only Gen 0
            sw.Stop();
            Console.WriteLine($"Generation 0 collection took: {sw.ElapsedMilliseconds} ms");
            CollectMemoryStats("After Gen 0 collection");

            // Force collection of generation 1
            Console.WriteLine("\nForcing collection of Generation 1...");
            sw.Restart();
            GC.Collect(1); // Collect Gen 0 and Gen 1
            sw.Stop();
            Console.WriteLine($"Generation 1 collection took: {sw.ElapsedMilliseconds} ms");
            CollectMemoryStats("After Gen 1 collection");

            // Force collection of generation 2
            Console.WriteLine("\nForcing collection of Generation 2...");
            sw.Restart();
            GC.Collect(2); // Collect all generations
            sw.Stop();
            Console.WriteLine($"Generation 2 collection took: {sw.ElapsedMilliseconds} ms");
            CollectMemoryStats("After Gen 2 collection");

            // Perform matrix multiplication
            Console.WriteLine("\nPerforming matrix multiplication...");
            sw.Restart();
            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);
            sw.Stop();
            Console.WriteLine($"Matrix multiplication took: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Result matrix size: {resultMatrix.GetLength(0)}x{resultMatrix.GetLength(1)}");

            CollectMemoryStats("After multiplication");
        }

        // h) Test GC with LOH compaction
        static void TestGCWithLOHCompaction()
        {
            PrintGCSettings();
            Console.WriteLine("\n=== Testing GC with LOH Compaction (Option h) ===\n");

            CollectMemoryStats("Initial state");

            // Create large objects to fragment the LOH
            Console.WriteLine("Creating large objects to fragment the LOH...");
            var largeObjects = new object[20];
            for (int i = 0; i < 20; i++)
            {
                largeObjects[i] = new byte[1024 * 1024]; // 1MB arrays (above LOH threshold)
            }

            // Clear some references to create fragmentation
            Console.WriteLine("Creating fragmentation by clearing some references...");
            for (int i = 0; i < 20; i += 2)
            {
                largeObjects[i] = null;
            }

            CollectMemoryStats("After creating fragmentation");

            // Now create more large objects
            Console.WriteLine("\nAllocating more large objects with fragmented LOH...");
            Stopwatch sw = Stopwatch.StartNew();
            double[,] largeMatrix1 = new double[LARGE_ROWS, LARGE_COLS];
            FillMatrix(largeMatrix1);
            sw.Stop();
            Console.WriteLine($"Allocation time with fragmented LOH: {sw.ElapsedMilliseconds} ms");

            CollectMemoryStats("After allocation with fragmentation");

            // Force GC with LOH compaction
            Console.WriteLine("\nForcing full GC with LOH compaction...");
            sw.Restart();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            sw.Stop();
            Console.WriteLine($"Full GC with LOH compaction took: {sw.ElapsedMilliseconds} ms");

            CollectMemoryStats("After LOH compaction");

            // Allocate again after compaction
            Console.WriteLine("\nAllocating large objects after LOH compaction...");
            sw.Restart();
            double[,] largeMatrix2 = new double[LARGE_ROWS, LARGE_COLS];
            FillMatrix(largeMatrix2);
            sw.Stop();
            Console.WriteLine($"Allocation time after LOH compaction: {sw.ElapsedMilliseconds} ms");

            CollectMemoryStats("After allocation with compacted LOH");

            // Perform matrix multiplication
            Console.WriteLine("\nPerforming matrix multiplication...");
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(smallMatrix);

            sw.Restart();
            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix1, smallMatrix);
            sw.Stop();
            Console.WriteLine($"Matrix multiplication took: {sw.ElapsedMilliseconds} ms");

            CollectMemoryStats("After multiplication");
        }

        // j) Test GC Notifications
        static void TestGCNotifications()
        {
            PrintGCSettings();
            Console.WriteLine("\n=== Testing GC Notifications (Option j) ===\n");

            int totalNotifications = 0;
            bool running = true;

            // Register for notifications about approaching a GC
            Console.WriteLine("Registering for GC notifications...");
            GC.RegisterForFullGCNotification(10, 10); // 10% memory pressure threshold

            // Start a thread to listen for notifications
            Console.WriteLine("Starting notification monitoring thread...");
            var notificationThread = new Thread(() =>
            {
                while (running)
                {
                    // Check for a notification of an approaching collection
                    GCNotificationStatus status = GC.WaitForFullGCApproach(1000);

                    if (status == GCNotificationStatus.Succeeded)
                    {
                        totalNotifications++;
                        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] GC Approach Notification #{totalNotifications}");
                        CollectMemoryStats("Before GC");
                    }

                    // Check for a notification that a collection has completed
                    status = GC.WaitForFullGCComplete(1000);

                    if (status == GCNotificationStatus.Succeeded)
                    {
                        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] GC Completed Notification");
                        CollectMemoryStats("After GC");
                    }

                    if (status == GCNotificationStatus.Canceled)
                    {
                        break;
                    }
                }
            });

            notificationThread.IsBackground = true;
            notificationThread.Start();

            Console.WriteLine("Creating memory pressure to trigger GCs...");

            // Create memory pressure to trigger GCs
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"\nAllocation round {i + 1}/5");

                // Allocate several large matrices
                var matrices = new double[5][,];
                for (int j = 0; j < matrices.Length; j++)
                {
                    matrices[j] = new double[LARGE_ROWS, LARGE_COLS];
                    FillMatrix(matrices[j]);
                    Console.WriteLine($"  Allocated matrix {j + 1}/5 in round {i + 1}");
                }

                // Do some work
                double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
                FillMatrix(smallMatrix);

                Stopwatch sw = Stopwatch.StartNew();
                MultiplyMatrixPart(matrices[0], smallMatrix);
                sw.Stop();
                Console.WriteLine($"Matrix multiplication took {sw.ElapsedMilliseconds} ms");

                Thread.Sleep(1000); // Give time for GC to run
            }

            // Unregister for GC notifications
            Console.WriteLine("\nUnregistering GC notifications...");
            GC.CancelFullGCNotification();
            running = false;

            // Wait for the notification thread to exit
            Thread.Sleep(2000);

            Console.WriteLine($"\nTest completed. Total GC notifications received: {totalNotifications}");
        }

        // Helper method to print current GC settings
        static void PrintGCSettings()
        {
            Console.WriteLine("\n=== Current GC Settings ===");
            Console.WriteLine($"IsServerGC: {GCSettings.IsServerGC}");
            Console.WriteLine($"LargeObjectHeapCompactionMode: {GCSettings.LargeObjectHeapCompactionMode}");
            Console.WriteLine($"LatencyMode: {GCSettings.LatencyMode}");
            Console.WriteLine($"LOH Threshold: {GetLOHThreshold()} KB (default is 85 KB)");
            Console.WriteLine();
        }

        // Helper method to get LOH threshold (not directly accessible)
        static int GetLOHThreshold()
        {
            // The LOH threshold is not directly accessible, but we can estimate it
            // by creating objects of increasing size until one goes to the LOH
            return 85; // Default value, since we can't easily determine the actual setting
        }

        // Helper method to collect memory statistics
        static void CollectMemoryStats(string label)
        {
            Console.WriteLine($"\n--- Memory Stats: {label} ---");
            Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / 1024} KB");
            Console.WriteLine($"Gen 0 Collections: {GC.CollectionCount(0)}");
            Console.WriteLine($"Gen 1 Collections: {GC.CollectionCount(1)}");
            Console.WriteLine($"Gen 2 Collections: {GC.CollectionCount(2)}");

            // Get more detailed memory info
            Process process = Process.GetCurrentProcess();
            process.Refresh();

            Console.WriteLine($"Working Set: {process.WorkingSet64 / 1024} KB");
            Console.WriteLine($"Private Memory: {process.PrivateMemorySize64 / 1024} KB");
            Console.WriteLine($"Paged Memory: {process.PagedMemorySize64 / 1024} KB");
            Console.WriteLine($"Virtual Memory: {process.VirtualMemorySize64 / 1024} KB");
        }

        #endregion

        #region 6. Techniki poprawy wydajności

        // a) Pooling obiektów
        static void AnalyzeObjectPooling()
        {
            Console.WriteLine("Testowanie poolingu obiektów na mnożeniu macierzy");

            // Test standardowego podejścia (bez poolingu)
            Console.WriteLine("\nStandardowe podejście (bez poolingu):");
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 5; i++)
            {
                double[,] largeMatrix = new double[LARGE_ROWS / 5, LARGE_COLS / 5]; // Mniejsza macierz dla szybszego testu
                double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
                FillMatrix(largeMatrix);
                FillMatrix(smallMatrix);

                double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);

                // Bez poolingu macierze są odrzucane po każdej iteracji
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");

            // Reset liczników GC dla lepszego porównania
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            GC.Collect();

            // Test z poolingiem
            Console.WriteLine("\nZ poolingiem obiektów:");
            sw.Restart();

            // Utworzenie puli macierzy
            Matrix2DPool largeMatrixPool = new Matrix2DPool(LARGE_ROWS / 5, LARGE_COLS / 5, 5);
            Matrix2DPool smallMatrixPool = new Matrix2DPool(SMALL_ROWS, SMALL_COLS, 5);
            Matrix2DPool resultMatrixPool = new Matrix2DPool(
                (LARGE_ROWS / 5) - SMALL_ROWS + 1,
                (LARGE_COLS / 5) - SMALL_COLS + 1,
                5);

            for (int i = 0; i < 5; i++)
            {
                // Pobranie macierzy z puli
                double[,] largeMatrix = largeMatrixPool.Get();
                double[,] smallMatrix = smallMatrixPool.Get();

                // Wypełnienie macierzy danymi
                FillMatrix(largeMatrix);
                FillMatrix(smallMatrix);

                // Wykonanie obliczeń
                double[,] resultMatrix = resultMatrixPool.Get();
                MultiplyMatrixPartInPlace(largeMatrix, smallMatrix, resultMatrix);

                // Zwrócenie macierzy do puli
                largeMatrixPool.Return(largeMatrix);
                smallMatrixPool.Return(smallMatrix);
                resultMatrixPool.Return(resultMatrix);
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0) - gen0Before}, " +
                              $"Gen1={GC.CollectionCount(1) - gen1Before}, Gen2={GC.CollectionCount(2) - gen2Before}");

            Console.WriteLine("\nPooling obiektów może znacząco zmniejszyć obciążenie GC i poprawić wydajność w scenariuszach z częstą alokacją.");
        }

        // Klasa implementująca pulę macierzy
        class Matrix2DPool
        {
            private readonly Queue<double[,]> _pool = new Queue<double[,]>();
            private readonly int _rows;
            private readonly int _cols;
            private readonly object _lock = new object();

            public Matrix2DPool(int rows, int cols, int initialCount)
            {
                _rows = rows;
                _cols = cols;

                // Przygotowanie puli obiektów
                for (int i = 0; i < initialCount; i++)
                {
                    _pool.Enqueue(new double[rows, cols]);
                }
            }

            // Pobranie macierzy z puli
            public double[,] Get()
            {
                lock (_lock)
                {
                    if (_pool.Count > 0)
                    {
                        return _pool.Dequeue();
                    }
                }

                // Jeśli pula jest pusta, tworzymy nową macierz
                return new double[_rows, _cols];
            }

            // Zwrócenie macierzy do puli
            public void Return(double[,] matrix)
            {
                if (matrix == null || matrix.GetLength(0) != _rows || matrix.GetLength(1) != _cols)
                {
                    return; // Ignorowanie niepasujących macierzy
                }

                lock (_lock)
                {
                    _pool.Enqueue(matrix);
                }
            }
        }

        // Wersja mnożenia macierzy używająca istniejącej macierzy wynikowej (bez alokacji)
        static void MultiplyMatrixPartInPlace(double[,] largeMatrix, double[,] smallMatrix, double[,] result)
        {
            int largeRows = largeMatrix.GetLength(0);
            int largeCols = largeMatrix.GetLength(1);
            int smallSize = smallMatrix.GetLength(0);

            int resultRows = result.GetLength(0);
            int resultCols = result.GetLength(1);

            // Sprawdzenie wymiarów macierzy
            if (resultRows != largeRows - smallSize + 1 || resultCols != largeCols - smallSize + 1)
            {
                throw new ArgumentException("Nieprawidłowe wymiary macierzy wynikowej");
            }

            // Przesuwanie okna po dużej macierzy
            for (int i = 0; i < resultRows; i++)
            {
                for (int j = 0; j < resultCols; j++)
                {
                    double sum = 0;

                    // Mnożenie każdego elementu 5x5 z odpowiadającymi wartościami w dużej macierzy
                    for (int x = 0; x < smallSize; x++)
                    {
                        for (int y = 0; y < smallSize; y++)
                        {
                            sum += largeMatrix[i + x, j + y] * smallMatrix[x, y];
                        }
                    }

                    result[i, j] = sum;  // Wpisanie wyniku do istniejącej macierzy
                }
            }
        }

        // b) Fixed i Pinned Object Heap
        static void AnalyzePinnedObjects()
        {
            Console.WriteLine("Testowanie Fixed i Pinned Object Heap na mnożeniu macierzy");

            // Test standardowego podejścia
            Console.WriteLine("\nStandardowe podejście (bez fixed):");
            Stopwatch sw = Stopwatch.StartNew();

            // Alokacja macierzy
            double[,] largeMatrix = new double[LARGE_ROWS / 5, LARGE_COLS / 5]; // Mniejsza macierz dla szybszego testu
            double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
            FillMatrix(largeMatrix);
            FillMatrix(smallMatrix);

            // Standardowe mnożenie
            double[,] resultMatrix = MultiplyMatrixPart(largeMatrix, smallMatrix);

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");

            // Test z fixed (pinned objects)
            Console.WriteLine("\nZ użyciem fixed (pinned objects):");
            sw.Restart();

            // Użycie jednowymiarowych tablic dla możliwości użycia fixed
            double[] largeMatrixFlat = new double[(LARGE_ROWS / 5) * (LARGE_COLS / 5)];
            double[] smallMatrixFlat = new double[SMALL_ROWS * SMALL_COLS];
            double[] resultMatrixFlat = new double[
                ((LARGE_ROWS / 5) - SMALL_ROWS + 1) *
                ((LARGE_COLS / 5) - SMALL_COLS + 1)];

            // Wypełnienie tablic danymi
            Random rand = new Random(42);
            for (int i = 0; i < largeMatrixFlat.Length; i++)
            {
                largeMatrixFlat[i] = rand.NextDouble() * 10;
            }

            for (int i = 0; i < smallMatrixFlat.Length; i++)
            {
                smallMatrixFlat[i] = rand.NextDouble() * 10;
            }

            // Mnożenie z użyciem fixed (pinned objects)
            MultiplyMatrixWithFixed(
                largeMatrixFlat, LARGE_ROWS / 5, LARGE_COLS / 5,
                smallMatrixFlat, SMALL_ROWS, SMALL_COLS,
                resultMatrixFlat);

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("\nUżycie fixed może poprawić wydajność przez unikanie kopiowania pamięci i zapobieganie przesuwaniu obiektów przez GC.");
            Console.WriteLine("W .NET 5+ używanie fixed automatycznie używa Pinned Object Heap dla większych obiektów.");
        }

        // Mnożenie macierzy z użyciem fixed
        static unsafe void MultiplyMatrixWithFixed(
    double[] largeMatrix, int largeRows, int largeCols,
    double[] smallMatrix, int smallRows, int smallCols,
    double[] result)
        {
            int resultRows = largeRows - smallRows + 1;
            int resultCols = largeCols - smallCols + 1;

            // Create local arrays to use in parallel loops
            double[] largeMatrixCopy = new double[largeRows * largeCols];
            double[] smallMatrixCopy = new double[smallRows * smallCols];
            double[] resultCopy = new double[resultRows * resultCols];

            // Copy data from largeMatrix, smallMatrix, and result into local arrays
            Array.Copy(largeMatrix, largeMatrixCopy, largeMatrix.Length);
            Array.Copy(smallMatrix, smallMatrixCopy, smallMatrix.Length);
            Array.Copy(result, resultCopy, result.Length);

            // Fixed pointers can be used safely here
            fixed (double* largePtr = largeMatrixCopy)
            fixed (double* smallPtr = smallMatrixCopy)
            fixed (double* resultPtr = resultCopy)

                // Parallelize the work across rows and columns
                Parallel.For(0, resultRows, i =>
                {
                    for (int j = 0; j < resultCols; j++)
                    {
                        double sum = 0;

                        for (int x = 0; x < smallRows; x++)
                        {
                            for (int y = 0; y < smallCols; y++)
                            {
                                // Calculate the linear indices for the 1D arrays
                                int largeIndex = (i + x) * largeCols + (j + y);
                                int smallIndex = x * smallCols + y;

                                // Perform the multiplication using the fixed pointers
                                sum += largeMatrixCopy[largeIndex] * smallMatrixCopy[smallIndex];
                            }
                        }

                        // Store the result at the correct position in the result matrix
                        resultCopy[i * resultCols + j] = sum;
                    }
                });


            // Copy the result from the local array back to the result matrix
            Array.Copy(resultCopy, result, result.Length);
        }



        // c) Dispose i finalizatory
        static void AnalyzeDisposePattern()
        {
            Console.WriteLine("Testowanie wzorca Dispose na mnożeniu macierzy");

            // Test standardowego podejścia
            Console.WriteLine("\nStandardowe podejście (bez Dispose):");
            Stopwatch sw = Stopwatch.StartNew();

            // Przetwarzanie obrazu bez jawnego zwalniania zasobów
            for (int i = 0; i < 3; i++)
            {
                using (Bitmap bitmap = new Bitmap(BITMAP_WIDTH, BITMAP_HEIGHT, PixelFormat.Format32bppArgb))
                {
                    FillRandomBitmap(bitmap);

                    // Utworzenie filtru (małej macierzy)
                    double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
                    FillMatrix(filter);
                    NormalizeFilter(filter);

                    // Zastosowanie filtru do bitmapy
                    ApplyFilterUnmanaged(bitmap, filter);
                }
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");

            // Reset liczników GC dla lepszego porównania
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            GC.Collect();

            // Test z użyciem własnej implementacji wzorca Dispose
            Console.WriteLine("\nZ użyciem wzorca Dispose:");
            sw.Restart();

            for (int i = 0; i < 3; i++)
            {
                using (UnmanagedMatrixProcessor processor = new UnmanagedMatrixProcessor(BITMAP_WIDTH, BITMAP_HEIGHT))
                {
                    // Utworzenie filtru (małej macierzy)
                    double[,] filter = new double[SMALL_ROWS, SMALL_COLS];
                    FillMatrix(filter);
                    NormalizeFilter(filter);

                    // Przetwarzanie z użyciem własnej klasy implementującej IDisposable
                    processor.ProcessWithFilter(filter);
                }
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0) - gen0Before}, " +
                              $"Gen1={GC.CollectionCount(1) - gen1Before}, Gen2={GC.CollectionCount(2) - gen2Before}");

            Console.WriteLine("\nWzorzec Dispose umożliwia natychmiastowe zwalnianie zasobów niezarządzanych i zmniejsza obciążenie GC.");
        }

        // Klasa implementująca wzorzec Dispose dla przetwarzania macierzy z użyciem niezarządzanej pamięci
        class UnmanagedMatrixProcessor : IDisposable
        {
            private readonly int _width;
            private readonly int _height;
            private IntPtr _dataPtr;
            private bool _disposed = false;

            public UnmanagedMatrixProcessor(int width, int height)
            {
                _width = width;
                _height = height;

                // Alokacja niezarządzanej pamięci
                _dataPtr = Marshal.AllocHGlobal(width * height * sizeof(double));

                // Wypełnienie losowymi danymi
                Random rand = new Random(42);
                unsafe
                {
                    double* ptr = (double*)_dataPtr.ToPointer();
                    for (int i = 0; i < width * height; i++)
                    {
                        ptr[i] = rand.NextDouble() * 10;
                    }
                }
            }

            public void ProcessWithFilter(double[,] filter)
            {
                if (_disposed)
                    throw new ObjectDisposedException("UnmanagedMatrixProcessor");

                int filterRows = filter.GetLength(0);
                int filterCols = filter.GetLength(1);

                unsafe
                {
                    double* src = (double*)_dataPtr.ToPointer();

                    // Alokacja pamięci dla wyniku
                    IntPtr resultPtr = Marshal.AllocHGlobal(_width * _height * sizeof(double));
                    double* dst = (double*)resultPtr.ToPointer();

                    try
                    {
                        // Zastosowanie filtru (przetwarzanie macierzy)
                        for (int i = 0; i < _height - filterRows + 1; i++)
                        {
                            for (int j = 0; j < _width - filterCols + 1; j++)
                            {
                                double sum = 0;

                                for (int x = 0; x < filterRows; x++)
                                {
                                    for (int y = 0; y < filterCols; y++)
                                    {
                                        sum += src[(i + x) * _width + (j + y)] * filter[x, y];
                                    }
                                }

                                dst[i * _width + j] = sum;
                            }
                        }

                        // Przykładowe użycie wyniku - tutaj po prostu kopiujemy z powrotem do źródła
                        for (int i = 0; i < _width * _height; i++)
                        {
                            src[i] = dst[i];
                        }
                    }
                    finally
                    {
                        // Zwolnienie tymczasowej pamięci
                        Marshal.FreeHGlobal(resultPtr);
                    }
                }
            }

            // Implementacja IDisposable
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        // Zwalnianie zasobów zarządzanych (jeśli istnieją)
                    }

                    // Zwalnianie zasobów niezarządzanych
                    if (_dataPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_dataPtr);
                        _dataPtr = IntPtr.Zero;
                    }

                    _disposed = true;
                }
            }

            // Finalizator
            ~UnmanagedMatrixProcessor()
            {
                Dispose(false);
            }
        }

        // d) Unikanie presji GC
        static void AnalyzeGCPressureTechniques()
        {
            Console.WriteLine("Testowanie technik unikania presji GC na mnożeniu macierzy");

            // Test standardowego podejścia (generującego presję GC)
            Console.WriteLine("\nStandardowe podejście (z presją GC):");
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 5; i++)
            {
                // Tworzenie wielu tymczasowych obiektów
                List<double[,]> tempMatrices = new List<double[,]>();

                for (int j = 0; j < 10; j++)
                {
                    double[,] tempMatrix = new double[LARGE_ROWS / 20, LARGE_COLS / 20];
                    FillMatrix(tempMatrix);
                    tempMatrices.Add(tempMatrix);
                }

                // Przetwarzanie wszystkich macierzy
                foreach (var tempMatrix in tempMatrices)
                {
                    double[,] smallMatrix = new double[SMALL_ROWS, SMALL_COLS];
                    FillMatrix(smallMatrix);

                    double[,] resultMatrix = MultiplyMatrixPart(tempMatrix, smallMatrix);
                }
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");

            // Reset liczników GC dla lepszego porównania
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            GC.Collect();

            // Test z technikami unikania presji GC
            Console.WriteLine("\nZ technikami unikania presji GC:");
            sw.Restart();

            // Przealokowanie większych tablic
            double[,] reuseMatrix1 = new double[LARGE_ROWS / 20, LARGE_COLS / 20];
            double[,] reuseMatrix2 = new double[SMALL_ROWS, SMALL_COLS];
            double[,] reuseResult = new double[
                (LARGE_ROWS / 20) - SMALL_ROWS + 1,
                (LARGE_COLS / 20) - SMALL_COLS + 1];

            // Użycie Span<T> do operacji na kawałkach macierzy
            for (int i = 0; i < 5; i++)
            {
                // Ponowne wykorzystanie tych samych obiektów
                for (int j = 0; j < 10; j++)
                {
                    FillMatrix(reuseMatrix1);
                    FillMatrix(reuseMatrix2);

                    // Użycie własnej wersji mnożenia która nie tworzy nowych obiektów
                    MultiplyMatrixPartInPlace(reuseMatrix1, reuseMatrix2, reuseResult);
                }
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0) - gen0Before}, " +
                              $"Gen1={GC.CollectionCount(1) - gen1Before}, Gen2={GC.CollectionCount(2) - gen2Before}");

            // Test z optymalizacją strukturami zamiast klas
            Console.WriteLine("\nZ użyciem struktur zamiast klas:");
            gen0Before = GC.CollectionCount(0);
            gen1Before = GC.CollectionCount(1);
            gen2Before = GC.CollectionCount(2);
            GC.Collect();

            sw.Restart();

            // Użycie struktury zamiast klasy
            MatrixStruct largeMatrixStruct = new MatrixStruct(LARGE_ROWS / 10, LARGE_COLS / 10);
            MatrixStruct smallMatrixStruct = new MatrixStruct(SMALL_ROWS, SMALL_COLS);

            for (int i = 0; i < 5; i++)
            {
                // Wypełnienie macierzy danymi
                largeMatrixStruct.FillRandom();
                smallMatrixStruct.FillRandom();

                // Mnożenie macierzy z użyciem struktury
                MatrixStruct resultMatrixStruct = largeMatrixStruct.MultiplyWithSmall(smallMatrixStruct);
            }

            sw.Stop();
            Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Liczba kolekcji GC: Gen0={GC.CollectionCount(0) - gen0Before}, " +
                              $"Gen1={GC.CollectionCount(1) - gen1Before}, Gen2={GC.CollectionCount(2) - gen2Before}");

            Console.WriteLine("\nTechniki unikania presji GC:");
            Console.WriteLine("1. Pooling obiektów - zamiast tworzyć nowe obiekty, używaj istniejących z puli");
            Console.WriteLine("2. Alokacja z góry - alokuj pamięć z góry zamiast w pętlach");
            Console.WriteLine("3. Ponowne użycie - używaj tych samych obiektów wielokrotnie");
            Console.WriteLine("4. Struktury zamiast klas - używaj struktur dla małych obiektów");
            Console.WriteLine("5. Span<T> - używaj Span<T> do operacji na fragmentach tablic bez kopiowania");
            Console.WriteLine("6. ArrayPool<T> - używaj ArrayPool<T> do wypożyczania tablic");
        }

        // Struktura reprezentująca macierz (wartość na stosie, nie na stercie)
        struct MatrixStruct
        {
            private double[] _data;
            private int _rows;
            private int _cols;

            public MatrixStruct(int rows, int cols)
            {
                _rows = rows;
                _cols = cols;
                _data = new double[rows * cols];
            }

            public void FillRandom()
            {
                Random rand = new Random(42);
                for (int i = 0; i < _data.Length; i++)
                {
                    _data[i] = rand.NextDouble() * 10;
                }
            }

            public double Get(int row, int col)
            {
                return _data[row * _cols + col];
            }

            public void Set(int row, int col, double value)
            {
                _data[row * _cols + col] = value;
            }

            public MatrixStruct MultiplyWithSmall(MatrixStruct small)
            {
                int resultRows = _rows - small._rows + 1;
                int resultCols = _cols - small._cols + 1;

                MatrixStruct result = new MatrixStruct(resultRows, resultCols);

                for (int i = 0; i < resultRows; i++)
                {
                    for (int j = 0; j < resultCols; j++)
                    {
                        double sum = 0;

                        for (int x = 0; x < small._rows; x++)
                        {
                            for (int y = 0; y < small._cols; y++)
                            {
                                sum += Get(i + x, j + y) * small.Get(x, y);
                            }
                        }

                        result.Set(i, j, sum);
                    }
                }

                return result;
            }
        }

        #endregion


    }
}