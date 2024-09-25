using System.Collections.Generic;
using System;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input
{
    public class MoleculeDisassemblerFactory
    {
        private record class DisassemblerInfo(
            Func<SolverComponent, ProgramWriter, Vector2, MoleculeDisassembler> CreateAssembler,
            SolutionPlan.MoleculeElementInfo ElementInfo);

        private Dictionary<int, DisassemblerInfo> m_disassemblerInfo;

        public MoleculeDisassembler CreateDisassembler(Molecule molecule, SolverComponent parent, ProgramWriter writer, Vector2 position)
        {
            return m_disassemblerInfo[molecule.ID].CreateAssembler(parent, writer, position);
        }

        public SolutionPlan.MoleculeElementInfo GetReagentElementInfo(Molecule molecule)
        {
            return m_disassemblerInfo[molecule.ID].ElementInfo;
        }

        public MoleculeDisassemblerFactory(IEnumerable<Molecule> reagents)
        {
            m_disassemblerInfo = reagents.ToDictionary(r => r.ID, r => CreateDisassemblerInfo(r));
        }

        private DisassemblerInfo CreateDisassemblerInfo(Molecule molecule)
        {
            IEnumerable<Element> GetDefaultElementOrder(Molecule molecule) => molecule.GetAtomsInInputOrder().Select(a => a.Element);

            if (molecule.Atoms.Count() == 1)
            {
                return new DisassemblerInfo(null, new SolutionPlan.MoleculeElementInfo(GetDefaultElementOrder(molecule)));
            }
            else if (molecule.Atoms.Count() == 2)
            {
                return new DisassemblerInfo(null, new SolutionPlan.MoleculeElementInfo(GetDefaultElementOrder(molecule).Reverse(), IsElementOrderReversible: true));
            }

            throw new UnsupportedException("LowCost solver can't currently handle reagents with more than one atom.");
        }
    }
}
