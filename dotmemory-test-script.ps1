
param (
    [string]$AppPath = "C:\Users\darth\Desktop\Ztp1\Ztp\bin\Release\net9.0\Ztp.dll"
)

function Test-GCConfiguration {
    param (
        [string]$TestName,
        [hashtable]$EnvironmentVars,
        [string]$Description
    )
    
    Write-Host "`n===============================================" -ForegroundColor Blue
    Write-Host "Test: $TestName" -ForegroundColor Green
    Write-Host "Description: $Description" -ForegroundColor Yellow
    Write-Host "Environment variables:" -ForegroundColor Cyan
    
    # Display the environment variables
    foreach ($key in $EnvironmentVars.Keys) {
        Write-Host "  $key = $($EnvironmentVars[$key])" -ForegroundColor Cyan
    }
    
    # Set environment variables for this test
    foreach ($key in $EnvironmentVars.Keys) {
        [Environment]::SetEnvironmentVariable($key, $EnvironmentVars[$key], "Process")
    }
    
    # Prompt user to prepare dotMemory
    Write-Host "`n*** PREPARATION STEP ***" -ForegroundColor Yellow
    Write-Host "1. Make sure dotMemory is ready to attach to a process" -ForegroundColor Yellow
    Write-Host "2. Press Enter to start the test..." -ForegroundColor Yellow
    Read-Host
    
    # Run the application with the current environment variables
    Write-Host "`nRunning application with $TestName settings..." -ForegroundColor White
    Write-Host "dotMemory will show process ID after application starts" -ForegroundColor White
    
    # Run in same window and capture the output
    & dotnet $AppPath
    
    # Application has completed - remind user to take final snapshot
    Write-Host "`n*** TEST COMPLETE ***" -ForegroundColor Green
    Write-Host "1. Take your final dotMemory snapshot now" -ForegroundColor Yellow
    Write-Host "2. Press Enter when ready for the next test..." -ForegroundColor Yellow
    Read-Host
    
    # Clear environment variables
    foreach ($key in $EnvironmentVars.Keys) {
        [Environment]::SetEnvironmentVariable($key, $null, "Process")
    }
    
    Write-Host "Test '$TestName' completed." -ForegroundColor Green
    Write-Host "-----------------------------------------------"
}

Write-Host "=== Optimized GC Testing for Matrix Operations ===" -ForegroundColor Magenta
Write-Host "This script uses GC settings that will show clear differences when multiplying large matrices." -ForegroundColor Magenta
Write-Host "`nPress Enter when dotMemory is open and ready..." -ForegroundColor Yellow
Read-Host

# Baseline test with default settings
Test-GCConfiguration -TestName "Baseline (Default Settings)" -EnvironmentVars @{} -Description "Default settings - baseline for comparison"

# Server GC - will use multiple heaps and GC threads
Test-GCConfiguration -TestName "Server GC" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
} -Description "Uses multiple GC heaps and dedicated threads - better for large matrices"

# Workstation GC - single background GC thread
Test-GCConfiguration -TestName "Workstation GC" -EnvironmentVars @{
    "DOTNET_GCServer" = "0"
} -Description "Single GC heap and thread - will be noticeably slower for large matrices"

# Aggressive memory limit - will cause more frequent collections
Test-GCConfiguration -TestName "Low Memory Limit (25%)" -EnvironmentVars @{
    "DOTNET_GCHeapHardLimitPercent" = "25"
} -Description "Restricts memory usage to 25% of physical memory - forces more frequent GCs"

# Very restrictive LOH threshold - more objects on LOH
Test-GCConfiguration -TestName "Low LOH Threshold (32KB)" -EnvironmentVars @{
    "DOTNET_GCLOHThreshold" = "32768"
} -Description "Lowers threshold for Large Object Heap to 32KB (default is 85KB)"

# High LOH threshold - fewer objects on LOH
Test-GCConfiguration -TestName "High LOH Threshold (500KB)" -EnvironmentVars @{
    "DOTNET_GCLOHThreshold" = "512000"
} -Description "Raises threshold for Large Object Heap to 500KB"

# High memory conservation - GC more aggressively
Test-GCConfiguration -TestName "Aggressive Memory Conservation" -EnvironmentVars @{
    "DOTNET_GCConserveMemory" = "9"
} -Description "Most aggressive memory conservation (9 on scale of 0-9)"

# Conserve memory with LOH compaction
Test-GCConfiguration -TestName "Memory Conservation + LOH Compaction" -EnvironmentVars @{
    "DOTNET_GCConserveMemory" = "7"
    "DOTNET_GCLargeObjectHeapCompactionMode" = "CompactOnce"
} -Description "Combines memory conservation with LOH compaction"

# No Concurrent GC - will cause longer pauses
Test-GCConfiguration -TestName "Non-Concurrent GC" -EnvironmentVars @{
    "DOTNET_GCConcurrent" = "0"
} -Description "Disables concurrent GC - will cause longer GC pauses but potentially better throughput"

# Background GC and Server GC - optimal for server workloads
Test-GCConfiguration -TestName "Server + Background GC" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
    "DOTNET_GCConcurrent" = "1"
} -Description "Server GC with background collections - typically best for high-throughput scenarios"

# No CPU groups - useful if you have many CPUs
Test-GCConfiguration -TestName "Server GC + No CPU Groups" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
    "DOTNET_GCCpuGroup" = "0"
    "DOTNET_GCHeapCount" = "8" 
} -Description "Server GC with explicit heap count, ignoring CPU groups"

Write-Host "`nAll tests completed!" -ForegroundColor Green
Write-Host "You can now analyze all snapshots in dotMemory." -ForegroundColor Green