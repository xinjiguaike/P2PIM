using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiThreadUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            tbLog.Text += DateTime.Now + "\n";

            var task1 = Task.Run(() => test1());
            var task2 = Task.Run(() => test2());
            
            await Task.WhenAll(task1, task2);
            
            
        }

        public void test1()
        {
            int i = 0;
            while (true)
            {
                i++;
                Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
                Dispatcher.Invoke(new Action(async () =>//Current main thread is not the UI thread but the new created thread.
                {
                    Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
                    await DelayAsync1(i);
                }));
            }
        }

        public void test2()
        {
            int i = 0;
            while (true)
            {
                i++;
                Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
                Dispatcher.Invoke(new Action(async () =>//Current main thread is not the UI thread but the new created thread.
                {
                    Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
                    await DelayAsync2(i);
                }));
            }
        }

        public async Task DelayAsync1(int i)
        {
            Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
            tbLog.Text += DateTime.Now + " [" + i + "] =>DelayAsync1 Begin\n"; 
            await Task.Delay(3000);
            tbLog.Text += DateTime.Now + " [" + i + "] =>DelayAsync1 End\n"; 
        }

        public async Task DelayAsync2(int i)
        {
            Console.WriteLine("Rudy Thread=>" + Thread.CurrentThread.ManagedThreadId.ToString());
            tbLog.Text += DateTime.Now + " [" + i + "] =>DelayAsync2 Begin\n";
            await Task.Delay(3000);
            tbLog.Text += DateTime.Now + " [" + i + "] =>DelayAsync2 End\n";
        }
    }
}
