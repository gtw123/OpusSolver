$puzzleDirs = (
    "test\puzzles\24hour-1-test"
)

$args = @("--analyze", "--report", "$PSScriptRoot\puzzles.csv")

foreach ($dir in $puzzleDirs)
{
    $args += "$PSScriptRoot\$dir"
}

& "$PSScriptRoot\bin\Release\OpusSolver.exe" $args

