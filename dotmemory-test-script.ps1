# PowerShell script for testing GC variables in the same console window
param (
    [string]$AppPath = "C:\Users\darth\Desktop\Ztp1\Ztp\bin\Release\net9.0\Ztp.dll"
)

function Test-GCConfiguration {
    param (
        [string]$TestName,
        [hashtable]$EnvironmentVars
    )
    
    Write-Host "`n===============================================" -ForegroundColor Blue
    Write-Host "Test: $TestName" -ForegroundColor Green
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
    
    # Create a signal file to indicate dotMemory should take before snapshot
    $beforeSnapshotSignal = Join-Path $env:TEMP "gc_test_before_$([Guid]::NewGuid().ToString()).signal"
    New-Item -Path $beforeSnapshotSignal -ItemType File -Force | Out-Null
    
    # Run the application with the current environment variables
    Write-Host "`nRunning application with $TestName settings..." -ForegroundColor White
    Write-Host "dotMemory will show process ID after application starts" -ForegroundColor White
    
    # IMPORTANT: Run in same window and capture the output
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
    
    # Clean up signal file
    if (Test-Path $beforeSnapshotSignal) {
        Remove-Item $beforeSnapshotSignal -Force
    }
    
    Write-Host "Test '$TestName' completed." -ForegroundColor Green
    Write-Host "-----------------------------------------------"
}

# Show instructions for the user
Write-Host "=== .NET GC Testing with dotMemory (Same Window) ===" -ForegroundColor Magenta
Write-Host "This script will guide you through testing different GC configurations." -ForegroundColor Magenta
Write-Host "BEFORE CONTINUING:" -ForegroundColor Yellow
Write-Host "1. Start dotMemory" -ForegroundColor Yellow
Write-Host "2. For each test, the script will:" -ForegroundColor Yellow
Write-Host "   - Set up the correct environment variables" -ForegroundColor Yellow
Write-Host "   - Run the application directly in this window" -ForegroundColor Yellow
Write-Host "   - Let you take snapshots in dotMemory before and after" -ForegroundColor Yellow
Write-Host "`nPress Enter when dotMemory is open and ready..." -ForegroundColor Yellow
Read-Host

# Run tests with different GC configurations
Test-GCConfiguration -TestName "Baseline (Default Settings)" -EnvironmentVars @{}

Test-GCConfiguration -TestName "Workstation GC" -EnvironmentVars @{
    "DOTNET_GCServer" = "0"
}

Test-GCConfiguration -TestName "Server GC" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
}

Test-GCConfiguration -TestName "Heap Hard Limit (50%)" -EnvironmentVars @{
    "DOTNET_GCHeapHardLimitPercent" = "50"
}

Test-GCConfiguration -TestName "RetainVM Enabled" -EnvironmentVars @{
    "DOTNET_GCRetainVM" = "1"
}

Test-GCConfiguration -TestName "High Memory Percent (95%)" -EnvironmentVars @{
    "DOTNET_GCHighMemPercent" = "95"
}

Test-GCConfiguration -TestName "Large Object Heap Threshold (200KB)" -EnvironmentVars @{
    "DOTNET_GCLOHThreshold" = "1048888"
}

Test-GCConfiguration -TestName "Conserve Memory (Level 7)" -EnvironmentVars @{
    "DOTNET_GCConserveMemory" = "5"
}

Test-GCConfiguration -TestName "LOH Compaction" -EnvironmentVars @{
    "DOTNET_GCLargeObjectHeapCompactionMode" = "CompactOnce" 
}

Test-GCConfiguration -TestName "Sustained Low Latency" -EnvironmentVars @{
    "DOTNET_GCSustainedLowLatency" = "1"
}

Test-GCConfiguration -TestName "Server GC with LOH Compaction" -EnvironmentVars @{
    "DOTNET_GCServer" = "1"
    "DOTNET_GCLargeObjectHeapCompactionMode" = "CompactOnce"
}

Write-Host "`nAll tests completed!" -ForegroundColor Green
Write-Host "You can now analyze all snapshots in dotMemory." -ForegroundColor Green