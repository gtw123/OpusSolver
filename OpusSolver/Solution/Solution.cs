using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    /// <summary>
    /// Represents a solution to a puzzle.
    /// </summary>
    public class Solution
    {
        public Puzzle Puzzle { get; private set; }
        public string Name { get; set; }
        public List<GameObject> Objects { get; private set; }
        public Program Program { get; private set; }

        public Metrics Metrics { get; set; }

        public bool HasErrors { get; set; }

        public Solution(Puzzle puzzle, string name, IEnumerable<GameObject> objects, Program program)
        {
            Puzzle = puzzle;
            Name = name;
            Objects = objects.ToList();
            Program = program;
        }

        public IEnumerable<T> GetObjects<T>() where T : GameObject
        {
            return Objects.OfType<T>();
        }
    }
}
