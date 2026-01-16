using BUS;
using DTO;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyDuLich.GUI.View
{
    public partial class TourView : UserControl
    {
        private BUS.Controller.TourController _controller;

        public TourView()
        {
            InitializeComponent();
            InitController();
        }

        private void InitController()
        {
            try
            {
                // Khởi tạo controller thông qua Factory
                var factoryResult = BUS.Controller.ControllerFactory.Create(typeof(Tour));
                _controller = factoryResult as BUS.Controller.TourController;

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                if (_controller != null)
                {
                    // Lấy danh sách tour và hiển thị lên DataGrid
                    dgTours.ItemsSource = _controller.GetAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                dgTours.ItemsSource = new List<Tour>();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadData();
                return;
            }

            try
            {
                var conditions = new List<SearchEngine.Condition>
                {
                    new SearchEngine.Condition
                    {
                        Field = "TourName",
                        Operator = SearchEngine.Op.Like,
                        Value = keyword
                    }
                };
                dgTours.ItemsSource = SearchEngine.Search<Tour>(conditions);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadData();
        }

        // --- LỆNH THÊM TOUR ---
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Tạo đối tượng Tour mới
                Tour newTour = new Tour();
                // Tự động tạo mã ID dựa trên thời gian (ticks) để đảm bảo không trùng
                newTour.Id = "T" + DateTime.Now.Ticks.ToString().Substring(10);

                // 2. Mở cửa sổ nhập liệu (CRUView)
                CRUView view = new CRUView(newTour, false); // false = Chế độ thêm mới
                Window window = new Window
                {
                    Title = "Thêm Tour Mới",
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                // 3. Nếu người dùng nhấn "Lưu" (DialogResult trả về true)
                if (window.ShowDialog() == true)
                {
                    _controller.Add(newTour); // Lưu vào CSDL
                    LoadData(); // Cập nhật lại danh sách trên bảng
                    MessageBox.Show("Thêm tour thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm tour: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- LỆNH SỬA TOUR ---
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã chọn tour nào trên bảng chưa
            if (dgTours.SelectedItem is Tour selectedTour)
            {
                try
                {
                    // Mở cửa sổ nhập liệu với dữ liệu của tour đã chọn
                    CRUView view = new CRUView(selectedTour, true); // true = Chế độ cập nhật
                    Window window = new Window
                    {
                        Title = "Cập nhật Thông tin Tour",
                        Content = view,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize
                    };

                    if (window.ShowDialog() == true)
                    {
                        _controller.Update(selectedTour); // Cập nhật vào CSDL
                        LoadData(); // Cập nhật lại danh sách
                        MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi cập nhật: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một tour trong danh sách để sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- LỆNH XÓA TOUR ---
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTours.SelectedItem is Tour selectedTour)
            {
                // Hỏi xác nhận trước khi xóa
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa tour '{selectedTour.TourName}' (Mã: {selectedTour.Id}) không?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _controller.Delete(selectedTour.Id); // Xóa khỏi CSDL
                        LoadData(); // Cập nhật lại danh sách
                        MessageBox.Show("Đã xóa tour thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một tour cần xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}