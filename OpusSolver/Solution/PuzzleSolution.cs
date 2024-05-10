using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solution
{
    /// <summary>
    /// Represents a solution to a puzzle.
    /// </summary>
    public class PuzzleSolution
    {
        public Puzzle Puzzle { get; private set; }
        public string Name { get; set; }
        public List<GameObject> Objects { get; private set; }
        public Program Program { get; private set; }

        public PuzzleSolution(Puzzle puzzle, IEnumerable<GameObject> objects, Program program)
        {
            Puzzle = puzzle;
            Name = "Generated solution";
            Objects = objects.ToList();
            Program = program;
        }

        public IEnumerable<T> GetObjects<T>() where T : GameObject
        {
            return Objects.OfType<T>();
        }
    }
}
