using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpusSolver.Solver
{
    public class Recipe : IEquatable<Recipe>
    {
        public class ReactionUsage(Reaction reaction, int maxUsages) : IEquatable<ReactionUsage>
        {
            public Reaction Reaction { get; private set; } = reaction;
            public int MaxUsages { get; private set; } = maxUsages;
            public int CurrentUsages { get; private set; }

            public void RecordUsage()
            {
                if (CurrentUsages >= MaxUsages)
                {
                    throw new SolverException($"Attempted to use reaction ({Reaction}) more than the allowed number of times. Current usage count = {CurrentUsages}, max usage count = {MaxUsages}.");
                }

                CurrentUsages++;
            }

            public bool IsAvailable => CurrentUsages < MaxUsages;

            public override string ToString() => $"{MaxUsages}x {Reaction}";

            public bool Equals(ReactionUsage other)
            {
                return Reaction.Equals(other.Reaction) && MaxUsages == other.MaxUsages && CurrentUsages == other.CurrentUsages;
            }

            public override bool Equals(object obj) => Equals(obj as ReactionUsage);

            // TODO: Implement this properly
            public override int GetHashCode() => 0;
        }

        private readonly Dictionary<ReactionType, List<ReactionUsage>> m_reactions = new();

        public bool HasWaste { get; set; }



        public bool Equals(Recipe other)
        {
            if (m_reactions.Count != other.m_reactions.Count)
            {
                return false;
            }

            foreach (var (p1, p2) in m_reactions.OrderBy(p => p.Key).Zip(other.m_reactions.OrderBy(p => p.Key)))
            {
                if (p1.Key != p2.Key || !p1.Value.SequenceEqual(p2.Value))
                {
                    return false;
                }
            }
            
            return HasWaste == other.HasWaste;
        }

        public override bool Equals(object obj) => Equals(obj as Recipe);

        // TODO: Implement this properly. For now it doesn't matter as we don't expecting to be comparing many recipes
        // for a single puzzle.
        public override int GetHashCode() => 0;
       

        public void AddReaction(Reaction reaction, int usageCount)
        {
            if (usageCount == 0)
            {
                // Don't add unused reactions because that makes it more difficult to compare two recipes for equality
                return;
            }

            if (!m_reactions.TryGetValue(reaction.Type, out var reactionList))
            {
                reactionList = new();
                m_reactions[reaction.Type] = reactionList;
            }

            reactionList.Add(new ReactionUsage(reaction, usageCount));
        }

        public IEnumerable<ReactionType> GetAvailableReactionTypes()
        {
            return m_reactions.Where(u => u.Value.Any(r => r.IsAvailable)).Select(u => u.Key);
        }

        public bool HasAvailableReactions(ReactionType type, int? id = null, Element? inputElement = null, Element? outputElement = null)
        {
            return GetAvailableReactions(type, id, inputElement, outputElement).Any();
        }

        public IEnumerable<ReactionUsage> GetAvailableReactions(ReactionType type, int? id = null, Element? inputElement = null, Element? outputElement = null)
        {
            return GetReactionUsages(type, id, inputElement, outputElement).Where(r => r.IsAvailable);
        }

        private IEnumerable<ReactionUsage> GetReactionUsages(ReactionType type, int? id = null, Element? inputElement = null, Element? outputElement = null)
        {
            if (!m_reactions.TryGetValue(type, out var reactions))
            {
                return [];
            }

            IEnumerable<ReactionUsage> filteredReactions = reactions;
            if (id.HasValue)
            {
                filteredReactions = filteredReactions.Where(r => r.Reaction.ID == id.Value);
            }
            if (inputElement.HasValue)
            {
                filteredReactions = filteredReactions.Where(r => r.Reaction.Inputs.ContainsKey(inputElement.Value));
            }
            if (outputElement.HasValue)
            {
                filteredReactions = filteredReactions.Where(r => r.Reaction.Outputs.ContainsKey(outputElement.Value));
            }

            return filteredReactions;
        }

        public void RecordReactionUsage(ReactionType type, int? id = null, Element? inputElement = null, Element? outputElement = null)
        {
            string GetTypeMessage()
            {
                string message = $"type = {type}";
                message += (id.HasValue ? $", ID = {id.Value}" : "");
                message += (inputElement.HasValue ? $", input element = {inputElement.Value}" : "");
                message += (outputElement.HasValue ? $", output element = {outputElement.Value}" : "");
                return message;
            }

            var reactions = GetReactionUsages(type, id, inputElement, outputElement).ToArray();
            if (!reactions.Any())
            {
                throw new SolverException($"No reactions are defined that meet the critera ({GetTypeMessage()}).");
            }
            else if (reactions.Length > 1)
            {
                throw new SolverException($"More than one reaction was found that meet the critera ({GetTypeMessage()}).");
            }

            var reaction = reactions.First();
            reaction.RecordUsage();
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.AppendLine($"Has waste: {HasWaste}");

            var types = new[] { ReactionType.Reagent }.Concat(m_reactions.Keys.Where(k => k != ReactionType.Reagent && k != ReactionType.Product)).Concat([ReactionType.Product]);
            foreach (var type in types)
            {
                foreach (var usage in m_reactions[type].Where(r => r.MaxUsages > 0))
                {
                    str.AppendLine(usage.ToString());
                }
            }

            return str.ToString();
        }
    }
}
