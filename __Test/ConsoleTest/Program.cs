﻿// See https://aka.ms/new-console-template for more information
using Haley.Services;
using Haley.Enums;
using ConsoleTest;
using ConsoleTest.Models;
using Haley.Abstractions;
using Haley.Models;
using Haley.Utils;

//new Testing().ConfigTest();
await new Testing().StorageTest();

Console.ReadKey();


class Testing {
    public void ConfigTest() {
        var cfgTest = new ConfigTest();
        cfgTest.RegisterTest();

        bool flag = true;
        do {
            Console.WriteLine($@"Enter an option to proceed.");
            var key = Console.ReadKey();
            Console.WriteLine($@"{Environment.NewLine}");
            switch (key.Key) {
                case ConsoleKey.Escape:
                break;
                case ConsoleKey.A:
                Console.WriteLine(cfgTest.Cfg.GetConfig<ConfigOne>(false)?.ToJson());
                break;
                case ConsoleKey.B:
                Console.WriteLine(cfgTest.Cfg.GetConfig<ConfigTwo>(false)?.ToJson());
                break;
                case ConsoleKey.C:
                var original = cfgTest.Cfg.GetConfig<ConfigOne>(false);
                original.Price = 17500;
                Console.WriteLine(original?.ToJson());
                break;
                case ConsoleKey.D:
                var copy = cfgTest.Cfg.GetConfig<ConfigOne>();
                copy.Price = 93400;
                Console.WriteLine(copy?.ToJson());
                break;
                case ConsoleKey.E:
                break;
                case ConsoleKey.D1:
                cfgTest.Cfg.SaveAll().Wait(); //Save all
                break;
                case ConsoleKey.D2:
                cfgTest.SaveConfigTest().Wait(); //Save one by one
                break;
                case ConsoleKey.D3:
                cfgTest.Cfg.DeleteAllFiles();
                break;
                case ConsoleKey.D4:
                cfgTest.Cfg.DeleteFile<ConfigOne>();
                break;
                case ConsoleKey.D5:
                cfgTest.Cfg.ResetConfig<ConfigTwo>();
                break;
                case ConsoleKey.D6:
                cfgTest.Cfg.ResetAllConfig();
                break;
                case ConsoleKey.D7:
                cfgTest.Cfg.SaveAll(askProvider: false).Wait(); //Save all
                break;
                case ConsoleKey.D8:
                cfgTest.Cfg.LoadConfig<ConfigOne>();
                break;
                case ConsoleKey.D9:
                cfgTest.Cfg.LoadAllConfig();
                break;
                default:
                break;
            }
        } while (flag);

        //cfgTest.SaveConfigTest().Wait();
        //cfgTest.SaveAll().Wait();
        //cfgTest.DeleteAll();


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
    }
    public async Task StorageTest() {
        try {
            var _agw = new AdapterGateway() { ThrowCRUDExceptions = true }; //Only for testing.
            var dss = new DiskStorageService().SetIndexer(new MariaDBIndexing(_agw, "mss_db"));
            await dss.RegisterClient("bcde", true);
            await dss.RegisterModule("test", true,"bcde",true);
            await dss.RegisterModule("93456", true,"bcde",true);
            await dss.RegisterModule("234asd", false,"bcde",true);
            await dss.RegisterModule("2342234", false,"bcde",true);
            await dss.RegisterModule("lingam", true,"bcde",true);

            await dss.Upload(new ObjectWriteRequest() {
                FileStream = new FileStream(@"C:\Users\tmp168\Downloads\PNCL Data Compliance - Frame 1(4).jpg",FileMode.Open),
            });
            Console.ReadKey();

        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }
}