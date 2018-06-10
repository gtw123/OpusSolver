using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution
{
    /// <summary>
    /// Represents a solution to a puzzle.
    /// </summary>
    public class PuzzleSolution
    {
        public List<GameObject> Objects { get; private set; }
        public Program Program { get; private set; }

        public PuzzleSolution(IEnumerable<GameObject> objects, Program program)
        {
            Objects = objects.ToList();
            Program = program;
        }

        public IEnumerable<T> GetObjects<T>() where T : GameObject
        {
            return Objects.OfType<T>();
        }
    }
}
