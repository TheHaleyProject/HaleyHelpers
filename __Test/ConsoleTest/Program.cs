// See https://aka.ms/new-console-template for more information
using Haley.Utils;
using Haley.Enums;
using ConsoleTest;

var cfgTest = new ConfigTest();

cfgTest.Register();



//IEnumerable<long> GetIds(int count = 5) {
//    int i = count;
//    while (i > 0) {
//        yield return RandomUtils.GetBigInt(11);
//        i--;
//    }
//}

//foreach (var id in GetIds(12)) {
//    Console.WriteLine(id);
//}

//var current = DateTime.UtcNow;
//var ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current =current.AddYears(1);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(5);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(10);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(15);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(100);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(150);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

//current = current.AddYears(350);
//ts = RandomUtils.GetTimeComponent(current);
//Console.WriteLine($@"For TS : {current.ToLongDateString()}");
//Console.WriteLine($@"Hours = {ts}");

Console.ReadKey();