using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace testMultiThread
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now);

            test();
            
            Console.ReadLine();

        }


        static void test()
        {
            //Task task1 = DelayAsync1(i);
            int i = 0;
            while (i < 5)
            {
                i++;
                new Action(async () =>
                {
                    Console.WriteLine("Begin: " + i);
                    await DelayAsync1(i);
                    Console.WriteLine("End: " + i);
                })();
                
            }
        }
        static async Task DelayAsync1(int i)
        {
            await Task.Delay(3000);
            Console.WriteLine("Rudy =>" + i);
            Console.WriteLine(DateTime.Now);
        }

        static async Task DelayAsync2()
        {
            await Task.Delay(3000);
            Console.WriteLine("Rudy=>222");
            Console.WriteLine(DateTime.Now);
        }

        static async Task DelayAsync3()
        {
            await Task.Delay(3000);
            Console.WriteLine("Rudy=>333");
            Console.WriteLine(DateTime.Now);
        }

        static async Task DelayAsync4()
        {
            await Task.Delay(3000);
            Console.WriteLine("Rudy=>444");
            Console.WriteLine(DateTime.Now);
        }

        static async Task DelayAsync5()
        {
            await Task.Delay(5000);
            Console.WriteLine("Rudy=>555");
            Console.WriteLine(DateTime.Now);
        }

        static async Task DelayAsync6()
        {
            await Task.Delay(3000);
            Console.WriteLine("Rudy=>666");
            Console.WriteLine(DateTime.Now);
        }
    }
}
