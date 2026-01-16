using BUS;
using DAL;
using DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyDuLich.GUI.CSRForm
{
    /// <summary>
    /// Logic xử lý chính cho phân hệ Chăm sóc khách hàng (CSR).
    /// Tập trung vào quản lý thông tin khách hàng và điều phối đơn đặt tour (Booking).
    /// </summary>
    public partial class CSRForm : UserControl
    {
        public event EventHandler RequestLogout;

        private readonly User _currentUser;
        private BUS.Controller.CustomerController _customerController;
        private BUS.Controller.BookingController _bookingController;
        private BUS.Controller.PassengerController _passengerController;

        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Booking> _bookings;

        public CSRForm()
        {
            InitializeComponent();
            _currentUser = UserSession.Instance.CurrentUser;

            InitializeSystem();
        }

        private void InitializeSystem()
        {
            try
            {
                // Khởi tạo các controller cần thiết cho nghiệp vụ CSR
                _customerController = Controller.ControllerFactory.Create(typeof(Customer)) as Controller.CustomerController;
                _bookingController = Controller.ControllerFactory.Create(typeof(Booking)) as Controller.BookingController;
                _passengerController = Controller.ControllerFactory.Create(typeof(Passenger)) as Controller.PassengerController;

                if (_currentUser != null)
                {
                    lblCSRName.Text = $"Nhân viên: {_currentUser.UserName}";
                }

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            RefreshCustomers();
            RefreshBookings();
        }

        // ===================== NGHIỆP VỤ KHÁCH HÀNG =====================

        private void RefreshCustomers()
        {
            try
            {
                var data = _customerController?.GetAll() ?? Enumerable.Empty<Customer>();
                _customers = new ObservableCollection<Customer>(data);
                dgCustomers.ItemsSource = _customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải khách hàng: {ex.Message}");
            }
        }

        private void BtnSearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearchCustomer.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                RefreshCustomers();
                return;
            }

            var conditions = new List<SearchEngine.Condition>
            {
                new SearchEngine.Condition { Field = "FullName", Operator = SearchEngine.Op.Like, Value = keyword },
                new SearchEngine.Condition { Field = "PhoneNumber", Operator = SearchEngine.Op.Like, Value = keyword }
            };

            try
            {
                dgCustomers.ItemsSource = SearchEngine.Search<Customer>(conditions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tìm kiếm thất bại: {ex.Message}");
            }
        }

        private void BtnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Vui lòng nhập thông tin khách hàng mới vào biểu mẫu.");
        }

        // ===================== NGHIỆP VỤ ĐƠN ĐẶT (BOOKING) =====================

        private void RefreshBookings()
        {
            try
            {
                var data = SearchEngine.Search<Booking>(new List<SearchEngine.Condition>());
                _bookings = new ObservableCollection<Booking>(data);
                dgBookings.ItemsSource = _bookings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn: {ex.Message}");
            }
        }

        /// <summary>
        /// Sự kiện Đặt Tour nhanh: Mở quy trình chọn lịch trình và khai báo hành khách.
        /// </summary>
        private void BtnQuickBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Customer customer)
            {
                var scheduleView = new GUI.View.ScheduleView();

                // Áp dụng Style ẩn các nút quản trị trong ScheduleView
                var hideButtonsStyle = this.Resources["HideActionButtonsStyle"] as Style;
                var hideTitleStyle = this.Resources["HideScheduleTitleStyle"] as Style;
                if (hideButtonsStyle != null) scheduleView.Resources.Add(typeof(Button), hideButtonsStyle);
                if (hideTitleStyle != null) scheduleView.Resources.Add(typeof(TextBlock), hideTitleStyle);

                var mainLayout = new DockPanel();
                var btnNext = new Button
                {
                    Content = "TIẾP TỤC NHẬP HÀNH KHÁCH",
                    Padding = new Thickness(25, 10, 25, 10),
                    Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(15),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                DockPanel.SetDock(btnNext, Dock.Bottom);
                mainLayout.Children.Add(btnNext);
                mainLayout.Children.Add(scheduleView);

                Window popup = new Window
                {
                    Title = $"Bước 1: Chọn Lịch Trình - Khách hàng: {customer.FullName}",
                    Content = mainLayout,
                    Width = 950,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = Window.GetWindow(this)
                };

                btnNext.Click += (s, ev) =>
                {
                    var selectedSchedule = scheduleView.dgData.SelectedItem as Schedule;
                    if (selectedSchedule == null)
                    {
                        MessageBox.Show("Vui lòng chọn lịch trình trước khi tiếp tục!");
                        return;
                    }

                    if (selectedSchedule.Booked >= selectedSchedule.MaxCap)
                    {
                        MessageBox.Show("Lịch trình này đã hết chỗ!");
                        return;
                    }

                    // Chuyển sang bước 2: Nhập thông tin hành khách
                    ShowPassengerInfoDialog(popup, customer, selectedSchedule);
                };

                popup.ShowDialog();
            }
        }

        private void ShowPassengerInfoDialog(Window window, Customer customer, Schedule schedule)
        {
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Counts
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scroll list
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            var header = new TextBlock { Text = "THÔNG TIN HÀNH KHÁCH CHUYẾN ĐI", FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) };
            Grid.SetRow(header, 0);

            var countPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            countPanel.Children.Add(new TextBlock { Text = "Người lớn:", VerticalAlignment = VerticalAlignment.Center });
            var txtAdult = new TextBox { Width = 50, Text = "1", Margin = new Thickness(5, 0, 15, 0), Padding = new Thickness(3) };
            countPanel.Children.Add(txtAdult);
            countPanel.Children.Add(new TextBlock { Text = "Trẻ em:", VerticalAlignment = VerticalAlignment.Center });
            var txtChild = new TextBox { Width = 50, Text = "0", Margin = new Thickness(5, 0, 15, 0), Padding = new Thickness(3) };
            countPanel.Children.Add(txtChild);
            var btnGenerate = new Button { Content = "Tạo danh sách nhập liệu", Padding = new Thickness(10, 2, 10, 2) };
            countPanel.Children.Add(btnGenerate);
            Grid.SetRow(countPanel, 1);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 10, 0, 10) };
            var passengerStack = new StackPanel();
            scroll.Content = passengerStack;
            Grid.SetRow(scroll, 2);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnComplete = new Button { Content = "HOÀN TẤT ĐẶT TOUR", Padding = new Thickness(20, 10, 20, 10), Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            footer.Children.Add(btnComplete);
            Grid.SetRow(footer, 3);

            grid.Children.Add(header);
            grid.Children.Add(countPanel);
            grid.Children.Add(scroll);
            grid.Children.Add(footer);

            window.Title = $"Bước 2: Nhập Hành Khách - {schedule.Id}";
            window.Content = grid;

            List<PassengerInputGroup> inputGroups = new List<PassengerInputGroup>();

            btnGenerate.Click += (s, e) => {
                passengerStack.Children.Clear();
                inputGroups.Clear();
                if (!int.TryParse(txtAdult.Text, out int adults)) adults = 0;
                if (!int.TryParse(txtChild.Text, out int children)) children = 0;

                if (adults + children + schedule.Booked > schedule.MaxCap)
                {
                    MessageBox.Show("Số lượng khách vượt quá chỗ trống còn lại!");
                    return;
                }

                for (int i = 0; i < adults; i++) AddPassengerInput(passengerStack, inputGroups, "Người lớn", i + 1);
                for (int i = 0; i < children; i++) AddPassengerInput(passengerStack, inputGroups, "Trẻ em", i + 1);
            };

            btnComplete.Click += (s, e) => {
                if (inputGroups.Count == 0) { MessageBox.Show("Vui lòng tạo danh sách hành khách!"); return; }
                foreach (var group in inputGroups)
                {
                    if (string.IsNullOrWhiteSpace(group.TxtName.Text))
                    {
                        MessageBox.Show("Vui lòng nhập đầy đủ họ tên hành khách!");
                        return;
                    }
                }

                try
                {
                    int adults = int.Parse(txtAdult.Text);
                    int children = int.Parse(txtChild.Text);

                    Booking booking = new Booking
                    {
                        Id = "BK" + DateTime.Now.Ticks.ToString().Substring(10),
                        BookDate = DateTime.Now,
                        Status = "Pending",
                        CusID = customer.Id,
                        ScheID = schedule.Id,
                        AdultCount = adults,
                        ChildCount = children
                    };
                    _bookingController.Add(booking);

                    foreach (var group in inputGroups)
                    {
                        Passenger p = new Passenger
                        {
                            Id = "PS" + Guid.NewGuid().ToString().Substring(0, 8),
                            FullName = group.TxtName.Text,
                            DateOfBirth = group.DpDob.SelectedDate ?? DateTime.Now.AddYears(-20),
                            Gender = (group.CbGender.SelectedItem as ComboBoxItem)?.Content.ToString(),
                            PassengerType = group.Type,
                            BookingId = booking.Id
                        };
                        _passengerController.Add(p);
                    }

                    MessageBox.Show("Đặt tour và lưu danh sách hành khách thành công!");
                    window.DialogResult = true;
                    window.Close();
                    RefreshBookings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message);
                }
            };
        }

        private void AddPassengerInput(StackPanel parent, List<PassengerInputGroup> groups, string type, int index)
        {
            var border = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(0, 0, 0, 10) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = $"{type} {index}", FontWeight = FontWeights.Bold, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 5) });

            var fields = new UniformGrid { Columns = 3 };
            var txtName = new TextBox { Tag = "Họ tên" };
            fields.Children.Add(new StackPanel { Margin = new Thickness(2), Children = { new TextBlock { Text = "Họ tên:" }, txtName } });

            var dpDob = new DatePicker { SelectedDate = DateTime.Now.AddYears(type == "Người lớn" ? -25 : -10) };
            fields.Children.Add(new StackPanel { Margin = new Thickness(2), Children = { new TextBlock { Text = "Ngày sinh:" }, dpDob } });

            var cbGender = new ComboBox { SelectedIndex = 0 };
            cbGender.Items.Add(new ComboBoxItem { Content = "Nam" });
            cbGender.Items.Add(new ComboBoxItem { Content = "Nữ" });
            fields.Children.Add(new StackPanel { Margin = new Thickness(2), Children = { new TextBlock { Text = "Giới tính:" }, cbGender } });

            panel.Children.Add(fields);
            border.Child = panel;
            parent.Children.Add(border);
            groups.Add(new PassengerInputGroup { TxtName = txtName, DpDob = dpDob, CbGender = cbGender, Type = type });
        }

        private class PassengerInputGroup
        {
            public TextBox TxtName { get; set; }
            public DatePicker DpDob { get; set; }
            public ComboBox CbGender { get; set; }
            public string Type { get; set; }
        }

        private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (dgBookings.SelectedItem is Booking booking)
            {
                var result = MessageBox.Show($"Xác nhận hủy đơn hàng {booking.Id}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        booking.Status = "Cancelled";
                        _bookingController.Update(booking);
                        RefreshBookings();
                        MessageBox.Show("Đã hủy đơn thành công.");
                    }
                    catch (Exception ex) { MessageBox.Show($"Lỗi xử lý hủy đơn: {ex.Message}"); }
                }
            }
        }

        private void BtnFilterBooking_Click(object sender, RoutedEventArgs e)
        {
            if (cbBookingStatus.SelectedItem is ComboBoxItem selected)
            {
                string status = selected.Tag.ToString();
                if (status == "All") { RefreshBookings(); return; }
                var conditions = new List<SearchEngine.Condition> { new SearchEngine.Condition { Field = "Status", Operator = SearchEngine.Op.Equal, Value = status } };
                dgBookings.ItemsSource = SearchEngine.Search<Booking>(conditions);
            }
        }

        private void ScheduleView_Loaded(object sender, RoutedEventArgs e) { }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RequestLogout?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class StatusToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() != "Cancelled";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}