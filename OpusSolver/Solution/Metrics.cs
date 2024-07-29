namespace OpusSolver
{
    public class Metrics
    {
        public int Cost { get; set; }
        public int Cycles { get; set; }
        public int Area { get; set; }
        public int Instructions { get; set; }

        public void Add(Metrics other)
        {
            Cost += other.Cost;
            Cycles += other.Cycles;
            Area += other.Area;
            Instructions += other.Instructions;
        }
    }
}
