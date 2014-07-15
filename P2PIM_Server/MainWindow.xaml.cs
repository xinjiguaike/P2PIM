using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace P2PIM_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Server server;
        public MainWindow()
        {
            InitializeComponent();
            server = new Server();
            this.DataContext = server;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var task1 = Task.Run(() => server.ListenClientConnect());
            var task2 = Task.Run(() => server.ReceiveMessage());

            await Task.WhenAll(task1, task2);         
        }


        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
          
        }
    }
}
