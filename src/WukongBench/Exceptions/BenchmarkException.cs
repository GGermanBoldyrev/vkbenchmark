namespace WukongBench.Exceptions;

internal sealed class BenchmarkException : Exception
{
    public BenchmarkException(string message) : base(message)
    {
    }
}
