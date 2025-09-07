namespace IDS.Interface.Loader
{
    public interface IOsteotomyHandler
    {
        string Name { get; }

        string Type { get; }

        double Thickness { get; }

        string[] Identifier { get; }

        double[,] Coordinate { get; }
    }
}
