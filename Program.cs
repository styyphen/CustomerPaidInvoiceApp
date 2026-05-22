namespace CustomerPaidInvoiceApp;

public class Program
{
    public static async Task Main(string[] args)
    {

        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"pp-monthly-update-new-version.csv");
        string[] files = await File.ReadAllLinesAsync(filePath);

        PropertyPrices prices = new PropertyPrices();
        List<PropertyPrices> lstProp = new List<PropertyPrices>();

        foreach (string result in files)
        {
            string[] stringColumns = result.Split(',');
            prices.Amount = double.Parse(stringColumns[1].Replace("\\", "").Replace("\"", ""));
            if (!string.IsNullOrEmpty(stringColumns[3].Replace("\\", "").Replace("\"", "").Split(" ")[0]))
            {
                prices.PostalCode = stringColumns[3].Replace("\\", "").Replace("\"", "").Split(" ")[0];
                lstProp.Add(prices);
            }

        }
        var postal = lstProp.GroupBy(p => p.PostalCode).Select(g => new
        {
            postalCode = g.Key,
            avarageAmount = g.Average(p => p.Amount)
        });

        foreach (var prop in postal)
        {
            Console.WriteLine($"{prop.postalCode} {prop.avarageAmount}");
        }

    }
}