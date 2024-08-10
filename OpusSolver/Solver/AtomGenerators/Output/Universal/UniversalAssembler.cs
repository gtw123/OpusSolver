using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Universal
{
    /// <summary>
    /// Assembles arbitrary molecules from their component atoms.
    /// </summary>
    public class UniversalAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2(2, 1);

        public int Width { get; private set; }

        private readonly IEnumerable<Molecule> m_products;
        private readonly LoopingCoroutine<object> m_assembleCoroutine;

        private readonly AssemblyArea m_assemblyArea;
        private IReadOnlyList<Arm> LowerArms => m_assemblyArea.LowerArms;
        private IReadOnlyList<Arm> UpperArms => m_assemblyArea.UpperArms;

        private readonly ProductConveyor m_productConveyor;

        private Molecule m_currentProduct;
        private List<Atom> m_assembledAtoms;
        private int m_currentArm;

        public UniversalAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            Width = products.Max(p => p.Width);
            m_assemblyArea = new AssemblyArea(this, writer, Width, products);

            m_products = products;
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);
            m_productConveyor = new ProductConveyor(this, writer, m_products);
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

            var arm = LowerArms[m_currentArm];
            if (atom.Bonds[HexRotation.R0] == BondType.Single)
            {
                // No need to grab as the atom will have just been bonded to the
                // existing atom on the bonder
                m_assemblyArea.SetUsedBonders(GlyphType.Bonding, HexRotation.R0);
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
            var activeArms = Enumerable.Range(m_currentArm + 1, Width - (m_currentArm + 1)).Select(i => LowerArms[i]);

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
            var programmer = new BondProgrammer(m_assemblyArea, m_currentProduct, row);
            programmer.Generate();

            if (row == 0 && !programmer.Instructions.Any())
            {
                // Simple case: single-height molecule with no special bonds. Just drop it straight on the output location.
                Writer.Write(activeArms, new[] { Instruction.Extend, Instruction.Reset });
                return;
            }

            Writer.Write(activeArms, Instruction.Reset);
            Writer.AdjustTime(-2);

            // Work out which atoms we need to grab. Disregard triplex bonds as we haven't created those yet.
            var grabArms = GetAtomsToGrabForRow(row, ignoreTriplexBonds: true).Select(atom => UpperArms[atom.Position.X]);
            Writer.Write(grabArms, [Instruction.Retract, Instruction.Grab, Instruction.Extend]);

            if (!programmer.Instructions.Any())
            {
                Writer.Write(UpperArms, Instruction.Extend);
                return;
            }

            Writer.Write(UpperArms, programmer.Instructions);
            Writer.Write(UpperArms, programmer.ReturnInstructions);

            if (row == 0)
            {
                // For a single-height molecule we can drop it as soon as the upper arms have returned.
                Writer.Write(UpperArms, Instruction.Drop);
            }
            else
            {
                Writer.Write(UpperArms, Instruction.Extend);
            }
        }

        private void FinishOtherRow(int row, IEnumerable<Arm> activeArms)
        {
            Writer.Write(activeArms, Instruction.Extend);

            var programmer = new BondProgrammer(m_assemblyArea, m_currentProduct, row);
            programmer.Generate();

            Writer.Write(activeArms.Concat(UpperArms), programmer.Instructions);

            // We don't need to return the lower arms before resetting them, so save some instructions by just doing a reset
            Writer.Write(activeArms, Instruction.Reset, updateTime: false);

            if (row > 0)
            {
                // Drop the molecule and re-grab it from the lower atoms
                Writer.Write(UpperArms, [Instruction.Drop, Instruction.Retract]);

                var grabArms = GetAtomsToGrabForRow(row, ignoreTriplexBonds: false).Select(atom => UpperArms[atom.Position.X]);
                Writer.Write(grabArms, Instruction.Grab);
                Writer.Write(UpperArms, programmer.ReturnInstructions);
                Writer.Write(UpperArms, Instruction.Extend);
            }
            else
            {
                // Last row - no need to re-grab
                Writer.Write(UpperArms, programmer.ReturnInstructions);
                Writer.Write(UpperArms, Instruction.Reset);
            }
        }

        public override void OptimizeParts()
        {
            m_assemblyArea.OptimizeParts();
        }
    }
}
