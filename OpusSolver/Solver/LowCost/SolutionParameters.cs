namespace OpusSolver.Solver.LowCost
{
    public static class SolutionParameters
    {
        public const string UseLength3Arm = nameof(UseLength3Arm);
        public const string AddExtraDisassemblerAccessPoint = nameof(AddExtraDisassemblerAccessPoint);
        public const string UseBreadthFirstOrderForComplexProducts = nameof(UseBreadthFirstOrderForComplexProducts);
        public const string UseLeafAtomsFirstForComplexReagents = nameof(UseLeafAtomsFirstForComplexReagents);
        public const string ReverseReagentBondTraversalDirection = nameof(ReverseReagentBondTraversalDirection);
        public const string ReverseProductBondTraversalDirection = nameof(ReverseProductBondTraversalDirection);
        public const string UseArmlessAtomBuffer = nameof(UseArmlessAtomBuffer);
        public const string UseLength1ArmInAtomBuffer = nameof(UseLength1ArmInAtomBuffer);

        public const string AddDisassemblerExtraWidth = nameof(AddDisassemblerExtraWidth);
        public const string NoSaltGeneratorRotation = nameof(NoSaltGeneratorRotation);
    }
}