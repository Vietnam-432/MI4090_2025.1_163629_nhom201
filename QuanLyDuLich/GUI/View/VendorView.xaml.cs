using BUS;
using DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace QuanLyDuLich.GUI.View
{
    public partial class VendorView : UserControl
    {
        // Danh sách hiển thị lên DataGrid
        private ObservableCollection<Vendor> _vendors;

        // Controller để thao tác nghiệp vụ
        private Controller.VendorController _vendorController;

        public VendorView()
        {
            InitializeComponent();

            // Khởi tạo Controller
            _vendorController = new Controller.VendorController();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Gọi xuống DB lấy tất cả nhà cung cấp
                var list = _vendorController.GetAll();

                // Chuyển đổi sang ObservableCollection
                _vendors = new ObservableCollection<Vendor>(list);
                dgData.ItemsSource = _vendors;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                _vendors = new ObservableCollection<Vendor>();
                dgData.ItemsSource = _vendors;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Tạo đối tượng mới
            Vendor newVendor = new Vendor
            {
                Id = Guid.NewGuid().ToString(),
                ProvidedService = new List<Service>() // Khởi tạo list rỗng để tránh null
            };

            // Mở form thêm mới
            ShowCRUWindow(newVendor, false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            Vendor selected = dgData.SelectedItem as Vendor;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp cần sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Mở form cập nhật
            ShowCRUWindow(selected, true);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Vendor selected = dgData.SelectedItem as Vendor;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn xóa '{selected.VendorName}'?\nLưu ý: Không thể xóa nếu nhà cung cấp đang có dịch vụ đi kèm.",
                                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Gọi Controller để xóa trong DB
                    _vendorController.Delete(selected.Id);

                    // Cập nhật giao diện
                    _vendors.Remove(selected);
                    MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnViewService_Click(object sender, RoutedEventArgs e)
        {
            Vendor selected = dgData.SelectedItem as Vendor;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp để xem dịch vụ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // [THỰC TẾ] Load danh sách dịch vụ từ DB theo VendorId
                // Vì danh sách ProvidedService mặc định không được load bởi GetAll, ta cần query riêng.

                var conditions = new List<SearchEngine.Condition>
                {
                    new SearchEngine.Condition
                    {
                        Field = "VendorId",
                        Operator = SearchEngine.Op.Equal,
                        Value = selected.Id
                    }
                };

                // Tìm các dịch vụ của nhà cung cấp này
                var services = SearchEngine.Search<Service>(conditions).ToList();

                // Gán vào đối tượng để hiển thị
                selected.ProvidedService = services;

                if (services.Count == 0)
                {
                    MessageBox.Show($"Nhà cung cấp '{selected.VendorName}' hiện chưa cung cấp dịch vụ nào.", "Thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Vẫn hiện bảng rỗng để người dùng biết
                }

                ShowServiceWindow(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dịch vụ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowServiceWindow(Vendor vendor)
        {
            // Tạo cửa sổ hiển thị danh sách dịch vụ
            Window serviceWindow = new Window
            {
                Title = $"Danh sách dịch vụ - {vendor.VendorName}",
                Width = 700,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            DataGrid dgServices = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                ItemsSource = vendor.ProvidedService, // Bind vào danh sách vừa load
                Margin = new Thickness(10),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                RowHeaderWidth = 0
            };

            // Cột Tên Dịch Vụ
            dgServices.Columns.Add(new DataGridTextColumn
            {
                Header = "Tên Dịch Vụ",
                Binding = new Binding("ServiceName"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star)
            });

            // Cột Chi Phí
            dgServices.Columns.Add(new DataGridTextColumn
            {
                Header = "Chi Phí Dự Kiến",
                Binding = new Binding("EstimatedCost") { StringFormat = "N0" },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Cột Đơn Vị Tính
            dgServices.Columns.Add(new DataGridTextColumn
            {
                Header = "Đơn Vị Tính",
                Binding = new Binding("Unit"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            serviceWindow.Content = dgServices;
            serviceWindow.ShowDialog();
        }

        private void ShowCRUWindow(Vendor vendor, bool isEdit)
        {
            Window window = new Window
            {
                Title = isEdit ? "Cập Nhật Nhà Cung Cấp" : "Thêm Nhà Cung Cấp Mới",
                Content = new CRUView(vendor, isEdit),
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
                        _vendorController.Add(vendor);
                        _vendors.Add(vendor);
                        MessageBox.Show("Thêm mới thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Cập nhật vào DB
                        _vendorController.Update(vendor);
                        dgData.Items.Refresh();
                        MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Nếu lỗi khi update, reload lại để đồng bộ UI với DB
                    if (isEdit) LoadData();
                }
            }
            else
            {
                // Nếu người dùng hủy, reload lại để bỏ qua các thay đổi chưa lưu trên giao diện
                if (isEdit) LoadData();
            }
        }
    }
}