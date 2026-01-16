using BUS;
using DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyDuLich.GUI.View
{
    public partial class ScheduleView : UserControl
    {
        private ObservableCollection<Schedule> _schedules;
        private Controller.ScheduleController _scheduleController;

        public ScheduleView()
        {
            InitializeComponent();

            // Khởi tạo Controller từ BUS
            _scheduleController = new Controller.ScheduleController();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Sử dụng SearchEngine để lấy dữ liệu kèm theo thông tin Tour và Guide
                // (Controller.GetAll() chỉ trả về dữ liệu thô, không có Tên Tour/Guide)
                var schedules = SearchEngine.Search<Schedule>(new List<SearchEngine.Condition>());

                _schedules = new ObservableCollection<Schedule>(schedules);
                dgData.ItemsSource = _schedules;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                _schedules = new ObservableCollection<Schedule>();
                dgData.ItemsSource = _schedules;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Tạo lịch trình mới với ID tự sinh
            Schedule newSche = new Schedule
            {
                Id = Guid.NewGuid().ToString(),
                DeDate = DateTime.Now,
                ReDate = DateTime.Now.AddDays(1),
                MaxCap = 20, // Giá trị mặc định
                Booked = 0
            };

            ShowCRUWindow(newSche, false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            Schedule selected = dgData.SelectedItem as Schedule;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn lịch trình để phân công/sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mở CRUView
            ShowCRUWindow(selected, true);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Schedule selected = dgData.SelectedItem as Schedule;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn lịch trình để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Bạn có chắc muốn xóa lịch trình này?\nCảnh báo: Các Booking liên quan có thể bị ảnh hưởng.",
                                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Gọi Controller để xóa trong DB
                    _scheduleController.Delete(selected.Id);

                    // Cập nhật giao diện
                    _schedules.Remove(selected);
                    MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowCRUWindow(Schedule schedule, bool isEdit)
        {
            Window window = new Window
            {
                Title = isEdit ? "Cập Nhật Lịch Trình" : "Tạo Lịch Trình Mới",
                Content = new CRUView(schedule, isEdit),
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (window.ShowDialog() == true)
            {
                try
                {
                    if (!isEdit)
                    {
                        // Thêm mới vào DB
                        _scheduleController.Add(schedule);

                        // Reload lại toàn bộ để lấy thông tin Tour/Guide mới nhất từ DB
                        // (Vì object 'schedule' lúc này chưa có object Tour/Guide liên kết)
                        LoadData();
                        MessageBox.Show("Thêm lịch trình thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Cập nhật vào DB
                        _scheduleController.Update(schedule);

                        // Reload lại để cập nhật các thông tin liên kết nếu có thay đổi (VD: đổi TourId -> Tên Tour đổi theo)
                        LoadData();
                        MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Nếu lỗi khi update, reload lại để đồng bộ UI với DB (revert các thay đổi trên object binding)
                    if (isEdit) LoadData();
                }
            }
            else
            {
                // Nếu người dùng hủy bỏ form, reload lại để revert các thay đổi tạm thời trên giao diện
                if (isEdit) LoadData();
            }
        }
    }
}