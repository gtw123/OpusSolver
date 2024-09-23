using System.Collections.Generic;
using System;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input
{
    public class MoleculeDisassemblerFactory
    {
        private class DisassemblerInfo(Func<SolverComponent, ProgramWriter, Vector2, MoleculeDisassembler> createAssembler, IEnumerable<Element> reagentElementOrder)
        {
            public Func<SolverComponent, ProgramWriter, Vector2, MoleculeDisassembler> CreateAssembler = createAssembler;
            public IEnumerable<Element> ReagentElementOrder = reagentElementOrder;
        }

        private Dictionary<int, DisassemblerInfo> m_disassemblerInfo;

        public MoleculeDisassembler CreateDisassembler(Molecule molecule, SolverComponent parent, ProgramWriter writer, Vector2 position)
        {
            return m_disassemblerInfo[molecule.ID].CreateAssembler(parent, writer, position);
        }

        public IEnumerable<Element> GetReagentElementOrder(Molecule molecule)
        {
            return m_disassemblerInfo[molecule.ID].ReagentElementOrder;
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
                return new DisassemblerInfo(null, GetDefaultElementOrder(molecule));
            }
            else if (molecule.Atoms.Count() == 2)
            {
                return new DisassemblerInfo(null, GetDefaultElementOrder(molecule).Reverse());
            }

            throw new UnsupportedException("LowCost solver can't currently handle reagents with more than one atom.");
        }
    }
}
