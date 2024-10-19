using System.Collections.Generic;
using System;
using System.Linq;
using OpusSolver.Solver.LowCost.Input.Complex;

namespace OpusSolver.Solver.LowCost.Input
{
    public class MoleculeDisassemblerFactory
    {
        private Func<ProgramWriter, ArmArea, IEnumerable<Molecule>, LowCostAtomGenerator> m_createDisassembler;
        private Dictionary<int, SolutionPlan.MoleculeElementInfo> m_reagentElementInfo;

        public LowCostAtomGenerator CreateDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> usedReagents)
        {
            return m_createDisassembler(writer, armArea, usedReagents);
        }
        
        public SolutionPlan.MoleculeElementInfo GetReagentElementInfo(Molecule molecule)
        {
            return m_reagentElementInfo[molecule.ID];
        }

        public MoleculeDisassemblerFactory(IEnumerable<Molecule> reagents, SolutionParameterSet paramSet)
        {
            var reverseElementOrder = paramSet.GetParameterValue(SolutionParameterRegistry.Common.ReverseReagentElementOrder);

            IEnumerable<Element> GetDefaultElementOrder(Molecule molecule) => molecule.GetAtomsInInputOrder().Reverse().Select(a => a.Element);

            if (reagents.All(r => r.Atoms.Count() == 1))
            {
                if (reagents.Count() > MonoatomicDisassembler.MaxReagents)
                {
                    throw new UnsupportedException($"LowCost solver can't currently handle more than {MonoatomicDisassembler.MaxReagents} monoatomic reagents (requested {reagents.Count()}).");
                }

                m_createDisassembler = (writer, armArea, usedReagents) => new MonoatomicDisassembler(writer, armArea, usedReagents);
                m_reagentElementInfo = reagents.ToDictionary(r => r.ID, r => new SolutionPlan.MoleculeElementInfo(GetDefaultElementOrder(r)));
            }
            else if (reagents.All(r => r.Atoms.Count() <= 2))
            {
                if (reagents.Count() > DiatomicDisassembler.MaxReagents)
                {
                    throw new UnsupportedException($"LowCost solver can't currently handle more than {DiatomicDisassembler.MaxReagents} diatomic reagents (requested {reagents.Count()}: {string.Join(", ", reagents.Select(r => r.Atoms.Count()))}).");
                }

                m_createDisassembler = (writer, armArea, usedReagents) => new DiatomicDisassembler(writer, armArea, usedReagents);
                m_reagentElementInfo = reagents.ToDictionary(r => r.ID, r => new SolutionPlan.MoleculeElementInfo(GetDefaultElementOrder(r), IsElementOrderReversible: true));
            }
            else if (reagents.All(r => r.Height == 1))
            {
                if (reagents.Count() > LinearDisassembler.MaxReagents)
                {
                    throw new UnsupportedException($"LowCost solver can't currently handle more than {LinearDisassembler.MaxReagents} linear reagents (requested {reagents.Count()}: {string.Join(", ", reagents.Select(r => r.Atoms.Count()))}).");
                }

                m_createDisassembler = (writer, armArea, usedReagents) => new LinearDisassembler(writer, armArea, usedReagents);
                m_reagentElementInfo = reagents.ToDictionary(r => r.ID, r => new SolutionPlan.MoleculeElementInfo(GetDefaultElementOrder(r)));
            }
            else
            {
                if (reagents.Count() > ComplexDisassembler.MaxReagents)
                {
                    throw new UnsupportedException($"LowCost solver can't currently handle more than {ComplexDisassembler.MaxReagents} complex reagents (requested {reagents.Count()}: {string.Join(", ", reagents.Select(r => r.Atoms.Count()))}).");
                }

                var dismantlers = ComplexDisassembler.CreateMoleculeDismantlers(reagents, reverseElementOrder);
                m_reagentElementInfo = reagents.ToDictionary(r => r.ID, r => new SolutionPlan.MoleculeElementInfo(dismantlers.Single(d => d.Molecule.ID == r.ID).GetElementOrder()));
                m_createDisassembler = (writer, armArea, usedReagents) => new ComplexDisassembler(writer, armArea, dismantlers);
            }
        }
    }
}
