using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CustomerPaidInvoiceApp.SessionTwo;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine(" Starting Postal Grouping Benchmarks...\n");

        var config = DefaultConfig.Instance
            .AddJob(Job.Default
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(8)
                .WithRuntime(CoreRuntime.Core10_0))
            .WithArtifactsPath("BenchmarkResults")
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
            .WithOption(ConfigOptions.JoinSummary, true);

        BenchmarkRunner.Run<PostalGroupingBenchmark>(config);

        Console.WriteLine("\n Benchmarking finished! Check the 'BenchmarkResults' folder.");
    }
}
