
using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace CustomerPaidInvoiceApp.SessionTwo;

[MemoryDiagnoser]
[RankColumn]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class PostalGroupingBenchmark
{
    private List<PropertyPrices> _data = null!;
    private readonly string _csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"pp-monthly-update-new-version.csv");

    [GlobalSetup]
    public async Task Setup()
    {

        if (!File.Exists(_csvPath))
            throw new FileNotFoundException($"CSV file not found at: {_csvPath}");

        var lines = await File.ReadAllLinesAsync(_csvPath);
        _data = new List<PropertyPrices>(lines.Length);

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var columns = line.Split(',');
            if (columns.Length < 4) continue;

            var postalParts = columns[3].Trim('"').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string postalCode = postalParts.Length > 0 ? postalParts[0] : "";

            if (!string.IsNullOrEmpty(postalCode) &&
                double.TryParse(columns[1].Trim('"'), out double amount))
            {
                _data.Add(new PropertyPrices
                {
                    Amount = amount,
                    PostalCode = postalCode
                });
            }
        }

        Console.WriteLine($"Loaded {_data.Count:N0} records.");
    }

    [Benchmark(Baseline = true, Description = "LINQ GroupBy")]
    public void UsingLinq_GroupBy()
    {
        var result = _data.GroupBy(p => p.PostalCode)
                          .Select(g =>
                           new
                           {
                               g.Key,
                               Avg = g.Average(x => x.Amount)
                           })
                          .ToList();

        Consume(result);
    }

    [Benchmark(Description = "ForEach + Lambda")]
    public void UsingForEach_Lambda()
    {
        var dict = new Dictionary<string, (double total, int count)>(_data.Count / 80);

        _data.ForEach(p =>
        {
            if (dict.TryGetValue(p.PostalCode, out var existing))
                dict[p.PostalCode] = (existing.total + p.Amount, existing.count + 1);
            else
                dict[p.PostalCode] = (p.Amount, 1);
        });

        var result = dict.Select(kv => new { kv.Key, Avg = kv.Value.total / kv.Value.count }).ToList();
        Consume(result);
    }

    [Benchmark(Description = "Manual For + CollectionsMarshal")]
    public void UsingManualForLoop()
    {
        var dict = new Dictionary<string, (double total, int count)>(_data.Count / 50);

        foreach (var p in _data)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, p.PostalCode, out bool exists);
            entry = exists
                ? (entry.total + p.Amount, entry.count + 1)
                : (p.Amount, 1);
        }

        var result = new List<object>(dict.Count);
        foreach (var item in dict)
            result.Add(new { item.Key, Avg = item.Value.total / item.Value.count });

        Consume(result);
    }

    [Benchmark(Description = "Parallel.ForEach + ConcurrentDictionary")]
    public void UsingParallel_Concurrent()
    {
        ConcurrentBag<PropertyPrices> bag = new();
        Parallel.ForEach(_data, prop =>
        {
            bag.Add(prop);
        });

        ConcurrentDictionary<string, (double totalAmount, int count)> postalGroups = new();

        Parallel.ForEach(bag, prop =>
        {
            postalGroups.AddOrUpdate(
                prop.PostalCode,
                (prop.Amount, 1),
                (key, existing) => (existing.totalAmount + prop.Amount, existing.count + 1)
            );
        });

        // Materialize result
        var result = postalGroups.Select(kvp => new
        {
            kvp.Key,
            Avg = kvp.Value.totalAmount / kvp.Value.count
        }).ToList();

        Consume(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(List<T> list) => _ = list?.Count;
}