using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace P2PIM_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public partial class MainWindow : Window
    {
        public User CurrentUser;

        public MainWindow()
        {
            InitializeComponent();
            
            CurrentUser = new User();
            this.DataContext = CurrentUser;

            lvOnlineUser.ItemsSource = CurrentUser.OnlineUsersList;
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            btnLogin.IsEnabled = false;
            btnLogout.IsEnabled = true;

            var task1 = CurrentUser.ReceiveMessageAsync();
            var task2 = CurrentUser.SendLogInOutMessageAsync("login");

            await Task.WhenAll(task1, task2);            
        }

        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            await CurrentUser.SendLogInOutMessageAsync("logout");
            CurrentUser.ClearOnlineUsersList();

            btnLogin.IsEnabled = true;
            btnLogout.IsEnabled = false;
        }
    }
}
