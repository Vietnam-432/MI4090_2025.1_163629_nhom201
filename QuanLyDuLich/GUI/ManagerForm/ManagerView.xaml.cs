using System;
using System.Windows;
using System.Windows.Controls;
using QuanLyDuLich.GUI.View; 

namespace QuanLyDuLich.GUI.ManagerForm
{
    public partial class ManagerView : UserControl
    {
        public event EventHandler RequestLogout;

        public ManagerView()
        {
            InitializeComponent();
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
       
            if (MainContent == null) return;

            var button = sender as RadioButton;
            if (button != null && button.Tag != null)
            {
                string tag = button.Tag.ToString();
                switch (tag)
                {
                    case "Tour":
                        MainContent.Content = new TourView();
                        break;
                    case "Guide":
                        MainContent.Content = new GuideView();
                        break;
                    case "Vendor":
                        MainContent.Content = new VendorView();
                        break;
                    case "User":
                        MainContent.Content = new UserView();
                        break;
                    case "Schedule":
                        MainContent.Content = new ScheduleView();
                        break;
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            RequestLogout?.Invoke(this, EventArgs.Empty);
        }
    }
}