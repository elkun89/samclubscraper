using System;
using System.IO;
using System.Reflection;

namespace scraper
{
    public class Scraper
    {
        private static readonly DateTime StartTime = DateTime.Now;

        /*
        e8/19   e8/26   e9/02   e9/7
        M: -    M: 2h   M: 1h   M: 0
        T: 5h   T: 4h   T: 4h   T: 1h
        W: 7h   W: 2h   W: 2h   W: 1h
        T: 4h   T: 2h   T: 0    T: 0
        F: 2h   F: 0    F: 0    F: 0
        SS:     SS:     SS:     SS:
        == 18r   == 10r   ==  7r   ==  
        */

        public static String Path { get; } = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;

        public static void Main(String[] args)
        {
            new SamsGetter().Get();
            Console.WriteLine();
            Console.WriteLine("Done...");
            Console.WriteLine();
            Console.WriteLine($"Elapsed {DateTime.Now - StartTime}");
            Console.ReadLine();
        }
    }
}
