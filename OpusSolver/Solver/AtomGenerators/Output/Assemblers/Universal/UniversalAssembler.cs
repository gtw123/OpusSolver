using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers.Universal
{
    /// <summary>
    /// Assembles arbitrary molecules from their component atoms.
    /// </summary>
    public class UniversalAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2(2, 1);

        public int Width { get; private set; }

        private IEnumerable<Molecule> m_products;
        private bool m_hasTriplex;
        private LoopingCoroutine<object> m_assembleCoroutine;

        private List<Arm> m_lowerArms;
        private List<Arm> m_upperArms;
        private List<Glyph> m_bonders = new List<Glyph>();
        private HashSet<Glyph> m_usedBonders = new HashSet<Glyph>();
        private ProductConveyor m_productConveyor;

        private Molecule m_currentProduct;
        private List<Atom> m_assembledAtoms;
        private int m_currentArm;

        public UniversalAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            Width = products.Max(p => p.Width);

            m_products = products;
            m_hasTriplex = products.Any(p => p.HasTriplex);
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            CreateBonders();
            CreateArms();
            CreateTracks();

            m_productConveyor = new ProductConveyor(this, writer, m_products);
        }

        private void CreateBonders()
        {
            var position = new Vector2(0, 0);
            AddBonder(ref position, 0, 0, HexRotation.R0, GlyphType.Bonding);
            AddBonder(ref position, Width + 2, 0, HexRotation.R60, GlyphType.Bonding);
            AddBonder(ref position, Width, 0, HexRotation.R120, GlyphType.Bonding);

            if (m_hasTriplex)
            {
                AddBonder(ref position, Width - 1, 0, HexRotation.R0, GlyphType.TriplexBonding);
                AddBonder(ref position, 3, 0, HexRotation.R120, GlyphType.TriplexBonding);
                AddBonder(ref position, 1, 1, HexRotation.R240, GlyphType.TriplexBonding);

                AddBonder(ref position, Width - 1, -1, HexRotation.R0, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, HexRotation.R60, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, HexRotation.R120, GlyphType.Unbonding);
            }
        }

        private void AddBonder(ref Vector2 position, int xOffset, int yOffset, HexRotation direction, GlyphType type)
        {
            position.X += xOffset;
            position.Y += yOffset;
            m_bonders.Add(new Glyph(this, position, direction, type));
        }

        private void CreateArms()
        {
            m_lowerArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(-Width + x + 1, -2), HexRotation.R60, ArmType.Piston, 2)).ToList();
            m_upperArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(x + 2, -1), HexRotation.R60, ArmType.Piston, 2)).ToList();
        }

        private void CreateTracks()
        {
            int lowerTrackLength = Width * 4 - 1;
            if (m_hasTriplex)
            {
                lowerTrackLength += Width * 4 + 2;
            }

            new Track(this, new Vector2(-Width + 1, -2), HexRotation.R0, lowerTrackLength);
            new Track(this, new Vector2(2, -1), HexRotation.R0, lowerTrackLength - Width - 1);
        }

        public override IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            return product.GetAtomsInInputOrder().Select(a => a.Element);
        }

        public override void AddAtom(Element element, int productID)
        {
            m_currentProduct = m_products.Single(product => product.ID == productID);
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            m_assembledAtoms = new List<Atom>();
            for (int y = m_currentProduct.Height - 1; y >= 0; y--)
            {
                m_currentArm = Width - 1;
                var atoms = m_currentProduct.GetRow(y).OrderByDescending(a => a.Position.X).ToList();
                for (int i = 0; i < atoms.Count - 1; i++)
                {
                    GrabAtom(atoms[i]);
                    yield return null;
                }

                var lastAtom = atoms[atoms.Count - 1];
                GrabAtom(lastAtom);
                FinishRow(y);

                if (y == 0)
                {
                    Writer.AdjustTime(-1);
                    m_productConveyor.MoveProductToOutputLocation(m_currentProduct);
                }
                yield return null;
            }
        }

        /// <summary>
        /// Returns all the atoms in allAtoms which are connected to startAtom.
        /// </summary>
        private static HashSet<Atom> GetConnectedAtoms(Atom startAtom, IEnumerable<Atom> allAtoms, bool ignoreTriplexBonds)
        {
            var seenAtoms = new HashSet<Atom>();
            var connectedAtoms = new HashSet<Atom>();
            var atomsToProcess = new Queue<Atom>([startAtom]);

            while (atomsToProcess.Count > 0)
            {
                var atom = atomsToProcess.Dequeue();
                seenAtoms.Add(atom);

                foreach (var (dir, bondType) in atom.Bonds)
                {
                    if (bondType == BondType.Single || !ignoreTriplexBonds && bondType == BondType.Triplex)
                    {
                        var otherAtomPosition = atom.Position.OffsetInDirection(dir, 1);
                        var otherAtom = allAtoms.Where(atom => atom.Position == otherAtomPosition).SingleOrDefault();
                        if (otherAtom != null && !seenAtoms.Contains(otherAtom))
                        {
                            connectedAtoms.Add(otherAtom);
                            atomsToProcess.Enqueue(otherAtom);
                        }
                    }
                }
            }

            return connectedAtoms;
        }

        /// <summary>
        /// Returns the atoms in a particular row that requiring grabbing in order to move the partially assembled
        /// molecule as a single unit. Assumes that the atoms in rows greater than "row" have already been assembled
        /// and bonded, and these atoms have been added to m_assembledAtoms.
        /// </summary>
        private IEnumerable<Atom> GetAtomsToGrabForRow(int row, bool ignoreTriplexBonds)
        {
            var connectedAtoms = new HashSet<Atom>();
            for (int x = Width - 1; x >= 0; x--)
            {
                var atom = m_currentProduct.GetAtom(new Vector2(x, row));
                if (atom != null && !connectedAtoms.Contains(atom))
                {
                    yield return atom;
                    connectedAtoms.UnionWith(GetConnectedAtoms(atom, m_assembledAtoms, ignoreTriplexBonds));
                }
            }
        }

        private void GrabAtom(Atom atom)
        {
            m_assembledAtoms.Add(atom);

            var arm = m_lowerArms[m_currentArm];
            if (atom.Bonds[HexRotation.R0] == BondType.Single)
            {
                // No need to grab as the atom will have just been bonded to the
                // existing atom on the bonder
                SetUsedBonders(GlyphType.Bonding, HexRotation.R0);
            }
            else
            {
                int distance = (Width - 1) - m_currentArm;
                Writer.AdjustTime(-distance);
                Writer.Write(arm, Enumerable.Repeat(Instruction.MovePositive, distance));
                Writer.Write(arm, Instruction.Grab);
            }

            // Move the atom to the rightmost side of the bonder
            Writer.Write(arm, Instruction.MovePositive);

            if (atom.Bonds[HexRotation.R180] != BondType.Single)
            {
                // Move the atom to the assembly area
                Writer.Write(arm, Enumerable.Repeat(Instruction.MovePositive, atom.Position.X + 1));
                m_currentArm--;
            }
        }

        private void FinishRow(int row)
        {
            var activeArms = m_lowerArms.GetRange(m_currentArm + 1, Width - (m_currentArm + 1));

            if (row == m_currentProduct.Height - 1)
            {
                FinishFirstRow(row, activeArms);
            }
            else
            {
                FinishOtherRow(row, activeArms);
            }
        }

        private void FinishFirstRow(int row, IEnumerable<Arm> activeArms)
        {
            var programmer = new BondProgrammer(Width, m_currentProduct, row);
            programmer.Generate();
            SetUsedBonders(programmer.UsedBonders);

            if (row == 0 && !programmer.Instructions.Any())
            {
                // Simple case: single-height molecule with no special bonds. Just drop it straight on the output location.
                Writer.Write(activeArms, new[] { Instruction.Extend, Instruction.Reset });
                return;
            }

            Writer.Write(activeArms, Instruction.Reset);
            Writer.AdjustTime(-2);

            // Work out which atoms we need to grab. Disregard triplex bonds as we haven't created those yet.
            var grabArms = GetAtomsToGrabForRow(row, ignoreTriplexBonds: true).Select(atom => m_upperArms[atom.Position.X]);
            Writer.Write(grabArms, [Instruction.Retract, Instruction.Grab, Instruction.Extend]);

            if (!programmer.Instructions.Any())
            {
                Writer.Write(m_upperArms, Instruction.Extend);
                return;
            }

            Writer.Write(m_upperArms, programmer.Instructions);
            Writer.Write(m_upperArms, programmer.ReturnInstructions);

            if (row == 0)
            {
                // For a single-height molecule we can drop it as soon as the upper arms have returned.
                Writer.Write(m_upperArms, Instruction.Drop);
            }
            else
            {
                Writer.Write(m_upperArms, Instruction.Extend);
            }
        }

        private void FinishOtherRow(int row, IEnumerable<Arm> activeArms)
        {
            Writer.Write(activeArms, Instruction.Extend);

            var programmer = new BondProgrammer(Width, m_currentProduct, row);
            programmer.Generate();
            SetUsedBonders(programmer.UsedBonders);

            Writer.Write(activeArms.Concat(m_upperArms), programmer.Instructions);

            // We don't need to return the lower arms before resetting them, so save some instructions by just doing a reset
            Writer.Write(activeArms, Instruction.Reset, updateTime: false);

            if (row > 0)
            {
                // Drop the molecule and re-grab it from the lower atoms
                Writer.Write(m_upperArms, [Instruction.Drop, Instruction.Retract]);

                var grabArms = GetAtomsToGrabForRow(row, ignoreTriplexBonds: false).Select(atom => m_upperArms[atom.Position.X]);
                Writer.Write(grabArms, Instruction.Grab);
                Writer.Write(m_upperArms, programmer.ReturnInstructions);
                Writer.Write(m_upperArms, Instruction.Extend);
            }
            else
            {
                // Last row - no need to re-grab
                Writer.Write(m_upperArms, programmer.ReturnInstructions);
                Writer.Write(m_upperArms, Instruction.Reset);
            }
        }

        private void SetUsedBonders(IEnumerable<(GlyphType, HexRotation?)> bonders)
        {
            foreach (var (type, direction) in bonders)
            {
                SetUsedBonders(type, direction);
            }
        }

        private void SetUsedBonders(GlyphType type, HexRotation? direction)
        {
            var bonders = m_bonders.Where(b => b.Type == type && (!direction.HasValue || b.Transform.Rotation == direction.Value));
            if (!bonders.Any())
            {
                throw new ArgumentException(Invariant($"Can't find bonder of type {type} and direction {direction}."));
            }

            m_usedBonders.UnionWith(bonders);
        }

        public override void OptimizeParts()
        {
            foreach (var bonder in m_bonders)
            {
                if (!m_usedBonders.Contains(bonder))
                {
                    bonder.Remove();
                }
            }
        }
    }
}
