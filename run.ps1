param (
   [string]$puzzleName = "",
   [switch]$optimize = $False
)
$puzzleDirs = (
    "test\puzzles\24hour-1-puzzles"
)

$excludes = @(
)

$solutionDir = "$PSScriptRoot\test\solutions"
$outputDir = Join-Path $solutionDir "output"
if (Test-Path -LiteralPath $outputDir)
{
    Remove-Item -LiteralPath $outputDir -Force -Recurse | Out-Null
}

$args = ("--output", $outputDir, "--report", "$PSScriptRoot\report.csv")

if ($optimize)
{
	$args += "--optimize"
}

if ($puzzleName -ne "")
{
	$args += "$PSScriptRoot\test\puzzles\24hour-1-puzzles\GEN$puzzleName.puzzle"
}
else
{
	foreach ($exclude in $excludes)
	{
		$args += "--exclude"
		$args += $exclude
	}

	foreach ($dir in $puzzleDirs)
	{
		$args += "$PSScriptRoot\$dir"
	}
}

& "$PSScriptRoot\bin\Release\OpusSolver.exe" $args

# Save a copy of the solutions so that we can scan them for best solutions later on
if ($optimize)
{
	$date = Get-Date -format "yyyyMMdd-HHmmss"
	Copy-Item "$outputDir\*" (Join-Path $solutionDir "$date")
}
