using BUS;
using DAL;
using DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyDuLich.GUI.GuideForm
{
    /// <summary>
    /// Interaction logic for GuideForm.xaml
    /// </summary>
    public partial class GuideForm : UserControl
    {
        // Sự kiện báo cho MainWindow biết người dùng muốn đăng xuất
        public event EventHandler RequestLogout;

        private Guide _currentGuide;
        private User _currentUserAccount;
        private ObservableCollection<Schedule> _mySchedules;

        public GuideForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // 1. Kiểm tra phiên đăng nhập từ UserSession
                if (UserSession.Instance.CurrentUser == null)
                {
                    MessageBox.Show("Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RequestLogout?.Invoke(this, EventArgs.Empty);
                    return;
                }

                _currentUserAccount = UserSession.Instance.CurrentUser;

                // Quy ước: Username của tài khoản chính là ID của Hướng dẫn viên
                string guideId = _currentUserAccount.UserName;

                // 2. Lấy thông tin chi tiết Guide từ DB qua SearchEngine
                var guideConditions = new List<SearchEngine.Condition>
                {
                    new SearchEngine.Condition { Field = "Id", Operator = SearchEngine.Op.Equal, Value = guideId }
                };

                var guides = SearchEngine.Search<Guide>(guideConditions);
                _currentGuide = guides.FirstOrDefault();

                if (_currentGuide == null)
                {
                    MessageBox.Show($"Không tìm thấy hồ sơ Hướng Dẫn Viên cho tài khoản '{guideId}'.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. Hiển thị thông tin Guide lên giao diện
                lblGuideName.Text = _currentGuide.FullName;
                txtFullName.Text = _currentGuide.FullName;
                txtPhone.Text = _currentGuide.PhoneNum;
                txtLanguage.Text = _currentGuide.Language;
                txtExp.Text = _currentGuide.ExYears.ToString();
                txtUsername.Text = _currentUserAccount.UserName;

                // 4. Tải danh sách lịch trình đã được phân công cho Guide này
                var scheConditions = new List<SearchEngine.Condition>
                {
                    new SearchEngine.Condition { Field = "GuideID", Operator = SearchEngine.Op.Equal, Value = guideId }
                };

                // Sắp xếp theo ngày đi tăng dần
                var sortOption = new SearchEngine.SortOption { Field = "DeDate", Direction = SearchEngine.SortDir.Asc };

                var schedules = SearchEngine.Search<Schedule>(scheConditions, sortOption).ToList();

                // Gán vào DataGrid
                _mySchedules = new ObservableCollection<Schedule>(schedules);
                dgMySchedules.ItemsSource = _mySchedules;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================================
        // XỬ LÝ XEM DANH SÁCH HÀNH KHÁCH
        // ==========================================================
        private void BtnViewPassengers_Click(object sender, RoutedEventArgs e)
        {
            // Xác định dòng dữ liệu đang được chọn
            Button btn = sender as Button;
            if (btn == null) return;

            Schedule selectedSchedule = btn.DataContext as Schedule;
            if (selectedSchedule == null) return;

            try
            {
                // 1. Truy vấn các Booking của lịch trình này
                var bookingConditions = new List<SearchEngine.Condition>
                {
                    new SearchEngine.Condition { Field = "ScheID", Operator = SearchEngine.Op.Equal, Value = selectedSchedule.Id }
                };

                // SearchEngine load Booking sẽ kèm theo danh sách Passengers (nhờ logic trong Repository.SearchFactory)
                var bookings = SearchEngine.Search<Booking>(bookingConditions).ToList();

                if (!bookings.Any())
                {
                    MessageBox.Show("Tour này hiện chưa có hành khách nào đặt chỗ.", "Thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Tập hợp tất cả hành khách từ các Booking khác nhau
                var allPassengers = new List<Passenger>();
                foreach (var b in bookings)
                {
                    if (b.Passengers != null)
                    {
                        allPassengers.AddRange(b.Passengers);
                    }
                }

                if (allPassengers.Count == 0)
                {
                    MessageBox.Show("Hệ thống chưa ghi nhận danh sách hành khách chi tiết cho các đơn đặt tour này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 3. Tạo cửa sổ popup hiển thị danh sách hành khách
                Window passengerWindow = new Window
                {
                    Title = $"Hành khách - Tour: {selectedSchedule.Tour?.TourName ?? selectedSchedule.TourId}",
                    Width = 700,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White
                };

                DataGrid dgPassengers = new DataGrid
                {
                    ItemsSource = allPassengers,
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    Margin = new Thickness(10),
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                    RowHeaderWidth = 0,
                    Background = Brushes.White
                };

                // Định nghĩa các cột cho bảng hành khách
                dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Họ và Tên", Binding = new Binding("FullName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
                dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Giới tính", Binding = new Binding("Gender"), Width = new DataGridLength(100) });
                dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Ngày sinh", Binding = new Binding("DateOfBirth") { StringFormat = "dd/MM/yyyy" }, Width = new DataGridLength(120) });
                dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Loại khách", Binding = new Binding("PassengerType"), Width = new DataGridLength(120) });

                passengerWindow.Content = dgPassengers;
                passengerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách khách: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================================
        // CẬP NHẬT HỒ SƠ CÁ NHÂN
        // ==========================================================
        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGuide == null || _currentUserAccount == null) return;

            try
            {
                // Validate thông tin
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtExp.Text, out int expYears) || expYears < 0)
                {
                    MessageBox.Show("Số năm kinh nghiệm không hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Cập nhật đối tượng Guide
                _currentGuide.FullName = txtFullName.Text.Trim();
                _currentGuide.PhoneNum = txtPhone.Text.Trim();
                _currentGuide.Language = txtLanguage.Text.Trim();
                _currentGuide.ExYears = expYears;

                // Lưu xuống DB (Dùng Repository trực tiếp để bypass quyền Manager)
                Repository.Instance.Update<Guide>(_currentGuide);

                // Xử lý đổi mật khẩu nếu có nhập
                string newPass = pbPassword.Password;
                string confirm = pbConfirmPassword.Password;
                bool isPassChanged = false;

                if (!string.IsNullOrEmpty(newPass))
                {
                    if (newPass != confirm)
                    {
                        MessageBox.Show("Mật khẩu xác nhận không trùng khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _currentUserAccount.Password = newPass;
                    Repository.Instance.Update<User>(_currentUserAccount);
                    isPassChanged = true;
                }

                string msg = "Cập nhật thông tin thành công!";
                if (isPassChanged) msg += "\nMật khẩu đã được thay đổi.";

                MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Cập nhật lại tên hiển thị trên header
                lblGuideName.Text = _currentGuide.FullName;
                pbPassword.Clear();
                pbConfirmPassword.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu thông tin: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RequestLogout?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}