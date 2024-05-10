namespace OpusSolver
{
    public enum GlyphType
    {
        Bonding,
        MultiBonding,
        TriplexBonding,
        Unbonding,
        Calcification,  // Air/Fire/Water/Earth -> Salt
        Duplication,    // Salt -> Air/Fire/Water/Earth
        Projection,     // 1 Quicksilver + 1 Metal -> 1 Next Metal
        Purification,   // 2 Metal -> 1 Next Metal
        Animismus,      // 2 Salt -> Vitae + Mors
        Disposal,
        Equilibrium,
        Unification,    // Air + Fire + Water + Earth -> Quintessence
        Dispersion      // Quintessence -> Air + Fire + Water + Earth
    }
}
