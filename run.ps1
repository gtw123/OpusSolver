<#
	.SYNOPSIS
	Runs OpusSolver to generate solutions for one or more puzzles. Solutions are saved to
	$PSScriptRoot\test\solutions\output and a report file (report.csv) will be saved to the current directory.
	
	.PARAMETER puzzleName
	Short name of a single puzzle file to generate solutions for, e.g. "123" means GEN123.
	If omitted, solutions will be generated for all puzzles.
	
	.PARAMETER solver
	The type of solver to use: lowcost (the default) or standard. Lowcost generates better solutions but
	doesn't work for all puzzles and is slower to run. Standard works with any puzzle but generates	suboptimal
	solutions.
	
	.PARAMETER optimize
	If true, generate a number of solutions for each puzzle and pick the best ones. This option will increase
	run time significantly but is more likely to generate a valid solution, especially for the lowcost solver.
	
	.PARAMETER parallel
	If true, run multiple instances of OpusSolver in parallel to speed up solution generation. Note that the
	console output and report file will be hard to read when using this option.
	
	.PARAMETER unsolvedOnly
	If true, generate solutions for only the puzzles that are known to not work with the specified solver. This
	option is useful for testing out improvements to the solver without needing to re-run all the puzzles.
	
#>

param (
   [string]$puzzleName = "",
   [string]$solver = "lowcost",
   [switch]$optimize = $false,
   [switch]$parallel = $false,
   [switch]$unsolvedOnly = $false
)

$unsolvedStandard = @()

$unsolvedLowCost = @(
	"GEN049",
	"GEN100",
	"GEN135",
	"GEN158",
	"GEN168",
	"GEN200",
	"GEN205",
	"GEN237",
	"GEN283",
	"GEN310",
	"GEN328",
	"GEN376",
	"GEN405",
	"GEN419",
	"GEN436",
	"GEN442",
	"GEN445",
	"GEN458",
	"GEN479",
	"GEN525",
	"GEN555",
	"GEN559",
	"GEN566",
	"GEN567",
	"GEN587",
	"GEN603",
	"GEN612",
	"GEN627",
	"GEN646",
	"GEN652",
	"GEN672",
	"GEN706",
	"GEN721",
	"GEN760",
	"GEN762",
	"GEN802",
	"GEN814",
	"GEN901",
	"GEN918",
	"GEN922",
	"GEN963"
)

$puzzleDirs = (
    "test\puzzles\24hour-1-puzzles"
)

$solutionDir = "$PSScriptRoot\test\solutions"
$outputDir = Join-Path $solutionDir "output"
if (Test-Path -LiteralPath $outputDir)
{
	Write-Host "Removing old solution files..."
    Remove-Item -LiteralPath $outputDir -Force -Recurse | Out-Null
}

$args = ("--output", $outputDir, "--solver", $solver)
if ($optimize)
{
	$args += "--optimize"
}

function CreateBatches($puzzles)
{
	$batches = New-Object System.Collections.Generic.List[System.Object]
	
	$numBatches = [Environment]::ProcessorCount
	if ($numBatches -ge $puzzles.Count)
	{
		$numBatches = $puzzles.Count
	}
	
	$firstIndex = 0;
	for ($i = 0; $i -lt $numBatches; $i++)
	{
		# Calculate the index so that the batches are approximately equal size (or as close as we can get)
		$lastIndex = [Math]::Floor(($i + 1) * $puzzles.Count / $numBatches);
		
		$batch = @()
		for ($j = $firstIndex; $j -lt $lastIndex; $j++)
		{
			$batch += $puzzles[$j]
		}
		
		$batches.Add($batch)
		
		$firstIndex = $lastIndex;
	}
	
	return $batches
}

$puzzleFiles = $null
if ($puzzleName -ne "")
{
	$puzzleFiles = @("$PSScriptRoot\test\puzzles\24hour-1-puzzles\GEN$puzzleName.puzzle")
}
elseif ($unsolvedOnly)
{
	$puzzleFiles = @()
	if ($solver -eq "lowcost")
	{
		$unsolved = $unsolvedLowcost
	}
	else
	{
		$unsolved = $unsolvedStandard
	}
	
	foreach ($name in $unsolved)
	{
		$puzzleFiles += "$PSScriptRoot\test\puzzles\24hour-1-puzzles\$name.puzzle"
	}
}

if ($parallel)
{
	$args += "--maxparallelverifiers"
	$args += "2"

	$stopwatch = [System.Diagnostics.StopWatch]::StartNew()		

	if ($puzzleFiles -eq $null)
	{
		$puzzleFiles = @()
		foreach ($dir in $puzzleDirs)
		{
			$puzzleFiles += @(Get-ChildItem -File "$PSScriptRoot\$dir\*.puzzle")
		}
	}
	
	$batches = CreateBatches($puzzleFiles)
	$batchIndex = 0
	$jobs = @()
	$reportFiles = @()
	foreach ($batch in $batches)
	{
		$jobArgs = $args + $batch

		$reportFile = "$PSScriptRoot\report_$solver_$batchIndex.csv"
		$reportFiles += $reportFile

		$jobArgs += "--report"
		$jobArgs += $reportFile

		$jobs += Start-Process -PassThru -NoNewWindow -FilePath "$PSScriptRoot\bin\Release\OpusSolver.exe" -Args $jobArgs
		
		$batchIndex++
	}

	foreach ($job in $jobs)
	{
		$job.WaitForExit()
	}

	$reportContent = ""
	foreach ($reportFile in $reportFiles)
	{
		$reportContent += (Get-Content -raw $reportFile)
		Remove-Item $reportFile
	}

	Set-Content -Path "$PSScriptRoot\report.csv" $reportContent

	Write-Host "Total execution time: $($stopwatch.Elapsed.TotalSeconds)"
}
else
{
	if ($puzzleFiles -ne $null)
	{
		$args += $puzzleFiles
	}
	else
	{
		$args += $puzzleDirs
	}
	
	& "$PSScriptRoot\bin\Release\OpusSolver.exe" $args
}

# Save a backup of the solutions so that we can scan them for best solutions later on
if (Test-Path $outputDir)
{
	$date = Get-Date -format "yyyyMMdd-HHmmss"
	$backupDir = (Join-Path $solutionDir "$date")
	mkdir $backupDir | Out-Null
	Copy-Item "$outputDir\*" $backupDir
}
