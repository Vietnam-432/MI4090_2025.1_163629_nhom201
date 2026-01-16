using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DTO;
using BUS;
using QuanLyDuLich.GUI.LoginForm;
using QuanLyDuLich.GUI.ManagerForm;
using QuanLyDuLich.GUI.GuideForm;
using QuanLyDuLich.GUI.CSRForm;
using QuanLyDuLich.GUI.AccountantForm;
namespace QuanLyDuLich
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoginView loginView = new LoginView();
            loginView.OnLoginSuccess += Login;
            MainContent.Content = loginView;
        }
        private void Login(object sender, System.EventArgs e)
        {
            
            switch (UserSession.Instance.CurrentUser.RoleId)

            {
                case 0:
                    this.Title = "Quản lý dịch vụ";
                    ManagerView managerView = new ManagerView();
                    managerView.RequestLogout += Logout;
                    MainContent.Content = managerView;
                break;
                case 1:
                    this.Title = "Kiểm toán dịch vụ";
                    AccountantForm aView = new AccountantForm();
                    aView.RequestLogout += Logout;
                    MainContent.Content = aView;
                    break;
                case 2:
                    this.Title = "Công việc hướng dẫn viên";
                    GuideForm guideView = new GuideForm();
                    guideView.RequestLogout += Logout;
                    MainContent.Content = guideView;
                    break;

                case 3:
                    this.Title = "Chăm sóc khách hàng";
                    CSRForm csrView = new CSRForm();
                    csrView.RequestLogout += Logout;
                    MainContent.Content = csrView;
                    break;
                   
            }
                
        }
        private void Logout(object sender, System.EventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.OnLoginSuccess += Login;
            this.Title = "Đăng nhập hệ thống";
            try
            {
                BUS.Controller.UserController con = new Controller.UserController();
                con.Logout();
                MainContent.Content = loginView;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}