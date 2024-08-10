using System.Collections.Generic;
using System.Linq;
using System;

namespace OpusSolver.Solver.AtomGenerators.Output.Hex3
{
    /// <summary>
    /// Generates instructions for assembling a molecule that contains a center atom.
    /// </summary>
    public class CenterAtomMoleculeBuilder : MoleculeBuilder
    {
        private enum OperationType
        {
            RotateClockwise,
            RotateCounterclockwise,
            GrabAtom,
            Bond,
            MoveAwayFromBonder,
            MoveTowardBonder,
        }

        private class Operation
        {
            public OperationType Type;
            public HexRotation FinalRotation;
            public Atom Atom;
        }

        private readonly Atom m_centerAtom;
        public override Vector2 CenterAtomPosition => m_centerAtom.Position;

        private IEnumerable<Operation> AllOperations => m_centerOperations.Concat(m_clockwiseOperations).Concat(m_counterclockwiseOperations);
        private readonly List<Operation> m_centerOperations;
        private readonly List<Operation> m_clockwiseOperations;
        private readonly List<Operation> m_counterclockwiseOperations;

        private bool m_needAvoidBondBeforeCounterclockwiseOperations = false;
        private HexRotation m_currentDirection = HexRotation.R0;
        private readonly HexRotationDictionary<Atom> m_grabbedAtoms = new();
        private readonly HashSet<HexRotation> m_constructedClockwiseBonds = new(); // Whether a bond has been constructed in a clockwise direction from the atom in the specified radial direction

        public CenterAtomMoleculeBuilder(AssemblyArea assemblyArea, Molecule product, IEnumerable<Atom> centralAtoms)
            : base(assemblyArea, product)
        {
            if (!centralAtoms.Any())
            {
                throw new ArgumentException($"{nameof(CenterAtomMoleculeBuilder)} can't handle molecules with no center atom.");
            }

            m_centerAtom = ChooseCenterAtom(centralAtoms);

            m_centerOperations = GenerateCenterOperations().ToList();
            m_clockwiseOperations = GenerateClockwiseOperations().ToList();
            m_counterclockwiseOperations = GenerateCounterclockwiseOperations().ToList();

            // TODO: Can we avoid this in some cases by pivoting before rotating to LH bonder?
            if (m_needAvoidBondBeforeCounterclockwiseOperations && m_counterclockwiseOperations.Any())
            {
                var rot = m_centerOperations.Concat(m_clockwiseOperations).Last().FinalRotation;
                m_clockwiseOperations.Add(new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = rot });
            }

            GenerateInstructions();
        }

        private Atom ChooseCenterAtom(IEnumerable<Atom> centralAtoms)
        {
            // Get the number of atoms that would need a CCW bond if this was the center atom
            int GetCCWBondCount(Atom atom)
            {
                return Product.GetAdjacentAtoms(atom.Position, (dir, atom) =>
                    atom.Bonds[dir + HexRotation.R180] == BondType.None &&
                    atom.Bonds[dir - HexRotation.R120] == BondType.None &&
                    atom.Bonds[dir + HexRotation.R120] != BondType.None).Keys.Count;
            }

            int GetBondCount(Atom atom) => atom.Bonds.Values.Count(b => b != BondType.None);

            // Choose the center atom to minimise the number of CCW bonds (preferably none) required on the outer atoms,
            // then to maximise the number of bonds on the center atom itself. This to help avoid needing to do the extra
            // assembly steps, which take extra cycles and may require more bonders.
            return centralAtoms.OrderBy(atom => GetCCWBondCount(atom)).ThenByDescending(atom => GetBondCount(atom)).First();
        }

        public override IEnumerable<Element> GetElementsInBuildOrder()
        {
            return new[] { m_centerAtom.Element }.Concat(AllOperations.Where(op => op.Type == OperationType.GrabAtom).Select(op => op.Atom.Element));
        }

        public override OutputLocation OutputLocation
        {
            get
            {
                if (m_counterclockwiseOperations.Any())
                {
                    return OutputLocation.Left;
                }
                else if (m_clockwiseOperations.Any())
                {
                    return OutputLocation.Right;
                }
                else
                {
                    return OutputLocation.RightSimple;
                }
            }
        }

        public override HexRotation OutputRotation
        {
            get
            {
                if (!AllOperations.Any())
                {
                    // This occurs when the molecule has only 1 atom
                    return HexRotation.R0;
                }

                var rot = AllOperations.Last().FinalRotation;
                if (m_counterclockwiseOperations.Any())
                {
                    return HexRotation.R120 - rot;
                }
                else if (m_clockwiseOperations.Any())
                {
                    return HexRotation.R60 - rot;
                }
                else
                {
                    return HexRotation.R120 - rot;
                }
            }
        }

        private IEnumerable<Operation> GenerateCenterOperations()
        {
            // Get the atoms that need to be bonded to the center atom
            var atoms = Product.GetAdjacentAtoms(m_centerAtom.Position, (dir, atom) => atom.Bonds[dir + HexRotation.R180] != BondType.None);

            // After we've bonded the radial atoms to the center atom, we'll move the molecule down one cell,
            // which means the top two atoms are on top of a bonder. This could create an unwanted bond,
            // so we try to choose the last radial atom so that we end up with at least one gap in the
            // top two cells.
            HexRotation lastAtomDir;
            if (atoms.Count != HexRotation.Count)
            {
                lastAtomDir = atoms.Keys.FirstOrDefault(dir => !atoms.ContainsKey(dir.Rotate60Clockwise()));
            }
            else
            {
                // There are no empty gaps in the radial atoms, so instead try to make sure the top two
                // atoms actually do need a bond, to avoid creating an unwanted bond. If there is no such
                // atom then FirstOrDefault will return default(HexRotation) which is fine because in this
                // case we won't even bother moving the molecule down one cell (it'll already be complete).
                lastAtomDir = atoms.Keys.FirstOrDefault(dir => atoms[dir.Rotate60Clockwise()].Bonds[dir.Rotate180()] != BondType.None);
            }

            // Enumerate the atoms so that lastAtomDir is last
            var startDir = lastAtomDir.Rotate60Clockwise();
            m_currentDirection = atoms.EnumerateClockwise(startFrom: startDir).FirstOrDefault().Key;
            foreach (var (dir, atom) in atoms.EnumerateClockwise(startFrom: startDir))
            {
                var rotations = m_currentDirection.CalculateRotationsTo(dir, rotateClockwiseIf180Degrees: true);
                foreach (var op in AddRotationOperations(rotations, false, false))
                {
                    yield return op;
                }

                m_grabbedAtoms[dir] = atom;
                yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
            }
        }

        private IEnumerable<Operation> GenerateClockwiseOperations()
        {
            // Get the atoms that need to be bonded to the next atom in a clockwise direction
            var atoms = Product.GetAdjacentAtoms(m_centerAtom.Position, (dir, atom) => atom.Bonds[dir - HexRotation.R120] != BondType.None);

            m_currentDirection = m_currentDirection.Rotate60Clockwise();

            // Start with the atom next to the first one we've already grabbed
            var startDir = m_grabbedAtoms.Keys.FirstOrDefault().Rotate60Counterclockwise();
            foreach (var (dir, atom) in atoms.EnumerateCounterclockwise(startFrom: startDir))
            {
                // We can only do anything if the next atom has already been grabbed
                if (m_grabbedAtoms.ContainsKey(dir.Rotate60Clockwise()))
                {
                    var rotations = m_currentDirection.CalculateRotationsTo(dir, rotateClockwiseIf180Degrees: true);

                    // Check if rotating without moving would create any unwanted bonds
                    bool moveWhileRotating = rotations.Any(newDir =>
                        m_grabbedAtoms.ContainsKey(newDir) && m_grabbedAtoms.ContainsKey(newDir.Rotate60Clockwise())
                        && !atoms.ContainsKey(newDir));

                    foreach (var op in AddRotationOperations(rotations, moveWhileRotating, moveWhileRotating))
                    {
                        yield return op;
                    }

                    m_constructedClockwiseBonds.Add(dir);

                    if (!m_grabbedAtoms.ContainsKey(dir))
                    {
                        m_grabbedAtoms[dir] = atom;
                        yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
                    }
                    else
                    {
                        yield return new Operation { Type = OperationType.Bond, FinalRotation = m_currentDirection, Atom = atom };
                    }
                }
            }
        }

        private IEnumerable<Operation> GenerateCounterclockwiseOperations()
        {
            // Get the atoms that need to be bonded to the next atom in a counter-clockwise direction
            var atoms = Product.GetAdjacentAtoms(m_centerAtom.Position, (dir, atom) => atom.Bonds[dir + HexRotation.R120] != BondType.None);

            m_currentDirection -= HexRotation.R120;

            if (m_grabbedAtoms.ContainsKey(m_currentDirection) && m_grabbedAtoms.ContainsKey(m_currentDirection.Rotate60Counterclockwise()))
            {
                if (!atoms.ContainsKey(m_currentDirection))
                {
                    m_needAvoidBondBeforeCounterclockwiseOperations = true;
                }
            }

            // Start with the atom next to the first one we've already grabbed
            var startDir = m_grabbedAtoms.Keys.FirstOrDefault().Rotate60Clockwise();
            bool isFirstOp = true;
            foreach (var (dir, atom) in atoms.EnumerateClockwise(startFrom: startDir))
            {
                if (!m_constructedClockwiseBonds.Contains(dir.Rotate60Counterclockwise()))
                {
                    var rotations = m_currentDirection.CalculateRotationsTo(dir, rotateClockwiseIf180Degrees: false);

                    bool moveWhileRotating = false;
                    bool moveOnlyAfterRotating = false;
                    if (isFirstOp && m_needAvoidBondBeforeCounterclockwiseOperations)
                    {
                        // In this case we don't need to write the "move away" op, only the "move toward"
                        moveOnlyAfterRotating = true;
                    }
                    else
                    {
                        // Check if rotating without moving would create any unwanted bonds
                        moveWhileRotating = rotations.Any(newDir =>
                            m_grabbedAtoms.ContainsKey(newDir) && m_grabbedAtoms.ContainsKey(newDir.Rotate60Counterclockwise())
                            && !atoms.ContainsKey(newDir));
                    }

                    foreach (var op in AddRotationOperations(rotations, moveWhileRotating, moveWhileRotating || moveOnlyAfterRotating))
                    {
                        yield return op;
                    }

                    m_constructedClockwiseBonds.Add(dir.Rotate60Counterclockwise());

                    if (!m_grabbedAtoms.ContainsKey(dir))
                    {
                        m_grabbedAtoms[dir] = atom;
                        yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
                    }
                    else
                    {
                        yield return new Operation { Type = OperationType.Bond, FinalRotation = m_currentDirection, Atom = atom };
                    }

                    isFirstOp = false;
                }
            }
        }

        private IEnumerable<Operation> AddRotationOperations(IEnumerable<HexRotation> rotations, bool moveBeforeRotating, bool moveAfterRotating)
        {
            if (moveBeforeRotating)
            {
                yield return new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = m_currentDirection };
            }

            if (rotations.Any())
            {
                var type = (rotations.First() - m_currentDirection == HexRotation.R60) ? OperationType.RotateClockwise : OperationType.RotateCounterclockwise;
                foreach (var rot in rotations)
                {
                    m_currentDirection = rot;
                    yield return new Operation { Type = type, FinalRotation = m_currentDirection };
                }
            }

            if (moveAfterRotating)
            {
                yield return new Operation { Type = OperationType.MoveTowardBonder, FinalRotation = m_currentDirection };
            }
        }

        private void GenerateInstructions()
        {
            // Move the center atom to the RHS of the bonder
            Writer.WriteGrabResetAction(AssemblyArea.HorizontalArm, Instruction.MoveNegative);
            Writer.Write(AssemblyArea.AssemblyArm, Instruction.Grab);

            // Bond radial atoms to the center atom
            GenerateOperationInstructions(m_centerOperations);

            if (!m_clockwiseOperations.Any() && !m_counterclockwiseOperations.Any())
            {
                if (m_centerOperations.Any())
                {
                    // Move the other arm out the way to avoid a collision
                    Writer.Write(AssemblyArea.HorizontalArm, [Instruction.MoveNegative, Instruction.MovePositive], updateTime: false);
                }

                return;
            }

            // Move the molecule down one row so we can weld radial atoms in a clockwise direction
            Writer.Write(AssemblyArea.AssemblyArm, Instruction.MovePositive);
            GenerateOperationInstructions(m_clockwiseOperations);

            if (!m_counterclockwiseOperations.Any())
            {
                return;
            }

            // Rotate the molecule so we can weld radial atoms in a counterclockwise direction
            Writer.Write(AssemblyArea.AssemblyArm, Instruction.RotateCounterclockwise);
            GenerateOperationInstructions(m_counterclockwiseOperations,
                afterGrab: () => Writer.WriteGrabResetAction(AssemblyArea.HorizontalArm, Instruction.MovePositive));
        }

        private void GenerateOperationInstructions(IEnumerable<Operation> operations, Action afterGrab = null)
        {
            foreach (var op in operations)
            {
                switch (op.Type)
                {
                    case OperationType.RotateClockwise:
                        Writer.Write(AssemblyArea.AssemblyArm, Instruction.PivotClockwise);
                        break;
                    case OperationType.RotateCounterclockwise:
                        Writer.Write(AssemblyArea.AssemblyArm, Instruction.PivotCounterclockwise);
                        break;
                    case OperationType.GrabAtom:
                        Writer.NewFragment();
                        afterGrab?.Invoke();
                        break;
                    case OperationType.Bond:
                        break;
                    case OperationType.MoveAwayFromBonder:
                        Writer.Write(AssemblyArea.AssemblyArm, Instruction.MovePositive);
                        break;
                    case OperationType.MoveTowardBonder:
                        Writer.Write(AssemblyArea.AssemblyArm, Instruction.MoveNegative);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operation {op.Type}.");
                }
            }
        }
    }
}
