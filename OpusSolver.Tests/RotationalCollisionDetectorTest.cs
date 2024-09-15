using NUnit.Framework;
using OpusSolver.Solver;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Tests
{
    [TestFixture]
    public class RotationalCollisionDetectorTest
    {
        private AtomCollection CreateAtoms()
        {
            return CreateAtomCollection(-4,
            [
                ( 4,         "F F F F F . . . . "),
                ( 3,        "F . . . . F . . . "),
                ( 2,       "F . . . . . F . . "),
                ( 1,      "F . . . . . . F . "),
                ( 0,     "F . . . F . . . F "),
                (-1,    ". . . . . F . . F "),
                (-2,   ". . . . . . F . F "),
                (-3,  ". . . . . . . F F "),
                (-4, ". . . . . . . . F "),
            ]);
        }

        private static AtomCollection CreateAtomCollection(int startX, IEnumerable<(int row, string line)> lines)
        {
            var atoms = new AtomCollection();

            foreach (var (row, line) in lines)
            {
                int x = startX;
                foreach (string element in line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries))
                {
                    if (element != ".")
                    {
                        atoms.AddAtom(new Atom(Element.Fire, HexRotation.All.ToDictionary(r => r, r => BondType.None), new(x, row)));
                    }

                    x++;
                }
            }

            return atoms;
        }

        private static IEnumerable<(Vector2 pos, bool expectedCollision)> CreateTestCases(int startX, IEnumerable<(int row, string line)> lines)
        {
            foreach (var (row, line) in lines)
            {
                int x = startX;
                foreach (string expectedCollision in line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries))
                {
                    // Only test positions that don't already have an atom in them
                    if (expectedCollision != ".")
                    {
                        yield return (new(x, row), expectedCollision == "T");
                    }

                    x++;
                }
            }
        }

        private GridState CreateGridState(Vector2 atomPos)
        {
            var gridState = new GridState();
            gridState.RegisterAtom(atomPos, Element.Salt, null);

            return gridState;
        }

        private static IEnumerable<(Vector2 pos, bool expectedCollision)> RotateClockwiseTestCases()
        {
            return CreateTestCases(-5,
            [
                (  5,                  "F F F F F F F F F F F F F F F"),
                (  4,                 "F . . . . . F F F F F F F F F"),
                (  3,                "F . T T T T . F F F F F F F F"),
                (  2,               "F . T T T T T . F F F F F F F"),
                (  1,              "F . T T T T T T . F F F F F F"),
                (  0,             "F . T T T . T T T . F F F F F"),
                ( -1,            "F F T T T T . T T . T F F F F"),
                ( -2,           "F F F T T T T . T . T T F F F"),
                ( -3,          "F F F F T T T T . . T T F F F"),
                ( -4,         "F F F F F F F T T . T T T F F"),
                ( -5,        "F F F F F F F F T T T T T F F"),
                ( -6,       "F F F F F F F F T T T T T F F"),
                ( -7,      "F F F F F F F F T T T T T F F"),
                ( -8,     "F F F F F F F F T T T T T T F"),
                ( -9,    "F F F F F F F F T T T T T T F"),
                (-10,   "F F F F F F F F T T T T T T F"),
                (-11,  "F F F F F F F F T T T T T F F"),
                (-12, "F F F F F F F F F F F F F F F"),
            ]);
        }

        [TestCaseSource(nameof(RotateClockwiseTestCases))]
        public void TestWillAtomsCollide_Clockwise((Vector2 pos, bool expectedCollision) testCase)
        {
            var detector = new RotationalCollisionDetector(CreateGridState(testCase.pos));
            Assert.That(detector.WillAtomsCollide(CreateAtoms(), new Transform2D(), new Transform2D(new(-4, -3), HexRotation.R60), HexRotation.R300), Is.EqualTo(testCase.expectedCollision));
        }

        private static IEnumerable<(Vector2 pos, bool expectedCollision)> RotateCounterclockwiseTestCases()
        {
            return CreateTestCases(-6,
            [
                (  5,                 "F F F F F F F F F F F F F"),
                (  4,                "F T . . . . . F F F F F F"),
                (  3,               "F T . T T T T . F F F F F"),
                (  2,              "F T . T T T T T . F F F F"),
                (  1,             "F T . T T T T T T . F F F"),
                (  0,            "F T . T T . . T T T . F F"),
                ( -1,           "F T T T T T T . T T . F F"),
                ( -2,          "F T T T T T T T . T . F F"),
                ( -3,         "F T T T F T T T T . . F F"),
                ( -4,        "F F T T F F T T T T . F F"),
                ( -5,       "F F T T T F T T T T T F ."),
                ( -6,      "F F F T T F F T T T T T F"),
                ( -7,     "F F F F T T F F F F F F F"),
                ( -8,    "F F F F F T F F F F F F F"),
                ( -9,   "F F F F F F T F F F F F F"),
                (-10,  "F F F F F F F T F F F F F"),
                (-11, "F F F F F F F F F F F F F"),
            ]);
        }

        [TestCaseSource(nameof(RotateCounterclockwiseTestCases))]
        public void TestWillAtomsCollide_Counterclockwise((Vector2 pos, bool expectedCollision) testCase)
        {
            var detector = new RotationalCollisionDetector(CreateGridState(testCase.pos));
            Assert.That(detector.WillAtomsCollide(CreateAtoms(), new Transform2D(), new Transform2D(new(6, -5), HexRotation.R120), HexRotation.R60), Is.EqualTo(testCase.expectedCollision));
        }

        private static IEnumerable<(Vector2 pos, bool expectedCollision)> RotateCounterclockwiseTestCases_SmallRotation()
        {
            return CreateTestCases(-6,
            [
                ( 7,             "F F F F F F F F F F F F F"),
                ( 6,            "F F T F F F F F F F F F F"),
                ( 5,           "F T T T T T T F F F F F F"),
                ( 4,          "F T . . . . . T F F F F F"),
                ( 3,         "F T . F F F F . T F F F F"),
                ( 2,        "F T . F F F F F . T F F F"),
                ( 1,       "F T . F F T T T T . T F F"),
                ( 0,      "F F . T F . . T T T . F F"),
                (-1,     "F F T T F F F . T T . F F"),
                (-2,    "F F F T T F F F . T . F F"),
                (-3,   "F F F F T T F F F . . T F"),
                (-4,  "F F F F F F F F F F . F F"),
                (-5, "F F F F F F F F F F F F F"),
            ]);
        }

        [TestCaseSource(nameof(RotateCounterclockwiseTestCases_SmallRotation))]
        public void TestWillAtomsCollide_Counterclockwise_SmallRotation((Vector2 pos, bool expectedCollision) testCase)
        {
            var detector = new RotationalCollisionDetector(CreateGridState(testCase.pos));
            Assert.That(detector.WillAtomsCollide(CreateAtoms(), new Transform2D(), new Transform2D(new(-1, 0), HexRotation.R0), HexRotation.R60), Is.EqualTo(testCase.expectedCollision));
        }
    }
}
