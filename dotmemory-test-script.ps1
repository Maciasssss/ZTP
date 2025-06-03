param (
    [string]$AppPath = "C:\Users\Maciej\ZTP\bin\Release\net9.0\Ztp.dll" # << UPDATE THIS PATH
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
    
    if ($EnvironmentVars.Count -eq 0) {
        Write-Host "  (No specific environment variables set for this test - using defaults)" -ForegroundColor Cyan
    } else {
        foreach ($key in $EnvironmentVars.Keys) {
            Write-Host "  $key = $($EnvironmentVars[$key])" -ForegroundColor Cyan
        }
    }
    
    $OriginalEnvVars = @{}
    foreach ($key in $EnvironmentVars.Keys) {
        $OriginalEnvVars[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
        [Environment]::SetEnvironmentVariable($key, $EnvironmentVars[$key], "Process")
    }
    
    Write-Host "`n*** PREPARATION STEP ***" -ForegroundColor Yellow
    Write-Host "1. Make sure dotMemory is ready to attach to a process" -ForegroundColor Yellow
    Write-Host "2. Press Enter to start the test..." -ForegroundColor Yellow
    Read-Host
    
    Write-Host "`nRunning application with $TestName settings..." -ForegroundColor White
    Write-Host "Application Path: $AppPath" -ForegroundColor DarkGray
    Write-Host "DotMemory will show process ID after application starts (C# app prints it)" -ForegroundColor White
    
    & dotnet $AppPath # Execute the C# application
    
    Write-Host "`n*** TEST COMPLETE ***" -ForegroundColor Green
    Write-Host "1. Take your final dotMemory snapshot now" -ForegroundColor Yellow
    Write-Host "2. Press Enter when ready for the next test..." -ForegroundColor Yellow
    Read-Host
    
    foreach ($key in $EnvironmentVars.Keys) {
        if ($OriginalEnvVars.ContainsKey($key) -and $OriginalEnvVars[$key] -ne $null) {
            [Environment]::SetEnvironmentVariable($key, $OriginalEnvVars[$key], "Process")
        } else {
            [Environment]::SetEnvironmentVariable($key, $null, "Process") # Clear it if it wasn't set before
        }
    }
    
    Write-Host "Test '$TestName' completed." -ForegroundColor Green
    Write-Host "-----------------------------------------------"
}

Write-Host "=== Focused GC Testing for Matrix Operations (~7 Key Areas) ===" -ForegroundColor Magenta
Write-Host "`nPress Enter when dotMemory is open and ready..." -ForegroundColor Yellow
Read-Host

# --- Test 1: Workstation GC Mode (Covers 'a') ---
Test-GCConfiguration -TestName "1. Workstation GC Mode" `
    -EnvironmentVars @{ 
        "DOTNET_GCServer" = "0" 
    } `
    -Description "Forces Workstation GC. Concurrent by default. Baseline for non-server behavior."

# --- Test 2: Server GC Mode (Covers 'a') ---
Test-GCConfiguration -TestName "2. Server GC Mode" `
    -EnvironmentVars @{ 
        "DOTNET_GCServer" = "1" 
    } `
    -Description "Forces Server GC. Uses multiple heaps, concurrent for Gen2. Baseline for server behavior."

# --- Test 3: Heap Hard Limit Percentage (Covers 'b') ---
Test-GCConfiguration -TestName "3. Low Heap Hard Limit (4%)" `
    -EnvironmentVars @{ 
        "DOTNET_GCHeapHardLimitPercent" = "4" 
        "DOTNET_GCServer" = "0"
    } `
    -Description "Restricts GC heap to 4% of physical memory. Forces frequent GCs."

# --- Test 4: High Memory Percentage (Covers 'd') ---
Test-GCConfiguration -TestName "4. Low 'High Memory Percent' (e.g., 40%)" `
    -EnvironmentVars @{ 
        "DOTNET_GCHighMemoryPercent" = "40" # Default is 90. Lower means GC gets aggressive sooner.
        "DOTNET_GCServer" = "1"
    } `
    -Description "GC becomes more aggressive when heap load reaches 40% (default 90%)."

# --- Test 5: Large Object Heap Threshold (Covers 'e') ---
Test-GCConfiguration -TestName "5. Low LOH Threshold (32KB)" `
    -EnvironmentVars @{ 
        "DOTNET_GCLOHThreshold" = "32768" 
        "DOTNET_GCServer" = "0"
    } `
    -Description "Lowers LOH threshold to 32KB. More objects go to LOH."

# --- Test 6: Conserve Memory (Covers 'f') ---
Test-GCConfiguration -TestName "6. Aggressive Memory Conservation (Setting 7)" `
    -EnvironmentVars @{ 
        "DOTNET_GCConserveMemory" = "7" 
    } `
    -Description "Most aggressive memory conservation (value 7). GC tries hard to reduce memory footprint."

# --- Test 7: LOH Compaction with GC.Collect (Covers 'h', leverages existing C# ForceGC) ---
Test-GCConfiguration -TestName "7. LOH Compaction on GC.Collect (CompactOnce)" `
    -EnvironmentVars @{ 
        "DOTNET_GCLargeObjectHeapCompactionMode" = "CompactOnce" 
        "DOTNET_GCServer" = "0"; 
        "DOTNET_GCLOHThreshold" = "32768"; 
    } `
    -Description "Sets LOH compaction mode to CompactOnce. The C# app's ForceGC() will trigger GC with this mode."



Write-Host "`nAll focused tests completed!" -ForegroundColor Green
Write-Host "Review your dotMemory snapshots for analysis." -ForegroundColor Green