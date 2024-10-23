<#
	.SYNOPSIS
	Generates a list of all puzzles, their reagents and products, and the reactions required to generate
	the products.

#>	
	
$puzzleDirs = (
    "test\puzzles\24hour-1-puzzles"
)

$args = @("--analyze", "--report", "$PSScriptRoot\puzzles.csv")

foreach ($dir in $puzzleDirs)
{
    $args += "$PSScriptRoot\$dir"
}

& "$PSScriptRoot\bin\Release\OpusSolver.exe" $args

