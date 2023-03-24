// See https://aka.ms/new-console-template for more information
using Haley.Utils;

IEnumerable<long> GetIds(int count = 5) {
    int i = count;
    while (i > 0) {
        yield return GeneralUtils.GetRandomBigInt(true);
        i--;
    }
}

foreach (var id in GetIds(1500)) {
    Console.WriteLine(id);
}

Console.ReadKey();