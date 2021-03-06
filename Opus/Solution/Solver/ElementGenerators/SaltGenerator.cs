﻿using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.ElementGenerators
{
    /// <summary>
    /// Generates salt from a cardinal element.
    /// </summary>
    public class SaltGenerator : ElementGenerator
    {
        public SaltGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        public override IEnumerable<Element> OutputElements => new[] { Element.Salt };

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var generated = Parent.RequestElement(new[] { Element.Salt }.Concat(PeriodicTable.Cardinals));
            if (generated != Element.Salt)
            {
                CommandSequence.Add(CommandType.Consume, generated, this);
                CommandSequence.Add(CommandType.Generate, Element.Salt, this);
            }
            else
            {
                PassThrough(generated);
            }

            return Element.Salt;
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.SaltGenerator(writer);
        }
    }
}
