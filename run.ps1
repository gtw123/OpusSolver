param (
   [string]$puzzleName = "",
   [switch]$optimize = $False
)
$puzzleDirs = (
    "test\puzzles"
)

$excludes = (
    "double-conduit.puzzle",	# production
    "easy-conduit.puzzle",		# production
    "c544897394273089.puzzle",	# production
    "conduit-bug.puzzle",		# production
    "week5.puzzle", 		    # production
    "weekEX.puzzle",		    # disallows tracks
    "w1698789743.puzzle",		# disallows tracks
    "w2868339730.puzzle",		# disallows pistons and tracks
    "OM2020_W5_Overloaded.puzzle", 	# production
    "OM2021_W1.puzzle",		    # triplex bonds between non-fire atoms
    "OM2021_W4.puzzle", 		# production
    "w1698786588.puzzle",	    # production
    "w2450508212.puzzle",	    # triplex bonds between non-fire atoms
    "w2450512232.puzzle",	    # production
    "w2501728107.puzzle",	    # production
    "w2591419339.puzzle",	    # production
    "w2788067624.puzzle",	    # production
    "w2839120106.puzzle",	    # Bond 0 has unsupported bond type 8
    "w2946684660.puzzle",	    # production
    "w2946687073.puzzle",	    # triplex bonds between non-fire atoms
    "w2946687209.puzzle"	    # Bond 9 has unsupported bond type 8
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
	$args += "$PSScriptRoot\test\puzzles\24hour-1-test\GEN$puzzleName.puzzle"
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
