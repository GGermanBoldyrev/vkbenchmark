namespace WukongBench.Exceptions;

public sealed class BenchmarkException : Exception
{
    public BenchmarkException(string message) : base(message)
    {
    }
}
