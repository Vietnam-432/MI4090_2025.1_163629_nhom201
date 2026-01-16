using BUS;
using DTO;
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

namespace QuanLyDuLich.GUI.View
{
    /// <summary>
    /// Interaction logic for GuideView.xaml
    /// </summary>
    public partial class GuideView : UserControl
    {
        private Controller.GuideController _controller;

        public GuideView()
        {
            InitializeComponent();
            try
            {
                // Khởi tạo Controller thông qua Factory
                _controller = (Controller.GuideController)Controller.ControllerFactory.Create(typeof(Guide));
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                // Lấy tất cả dữ liệu ban đầu
                dgGuides.ItemsSource = _controller.GetAll();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                LoadData();
                return;
            }

            try
            {
                // Tìm kiếm theo tên (FullName)
                var conditions = new List<SearchEngine.Condition>();
                conditions.Add(new SearchEngine.Condition
                {
                    Field = "FullName",
                    Operator = SearchEngine.Op.Like,
                    Value = keyword
                });

                var results = SearchEngine.Search<Guide>(conditions);
                dgGuides.ItemsSource = results;

                if (results == null || !results.Any())
                {
                    MessageBox.Show($"Không tìm thấy Hướng dẫn viên nào có tên chứa: '{keyword}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Tạo mới với ID tự sinh
            Guide newGuide = new Guide { Id = Guid.NewGuid().ToString() };
            ShowCRUWindow(newGuide, false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgGuides.SelectedItem is Guide selectedGuide)
            {
                ShowCRUWindow(selectedGuide, true);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn HDV cần sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgGuides.SelectedItem is Guide selectedGuide)
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa HDV '{selectedGuide.FullName}' không?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _controller.Delete(selectedGuide.Id);
                        LoadData();
                        MessageBox.Show("Đã xóa thành công!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn Hướng dẫn viên cần xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowCRUWindow(Guide guide, bool isEdit)
        {
            CRUView view = new CRUView(guide, isEdit);
            Window window = new Window
            {
                Title = isEdit ? "Cập nhật Hướng dẫn viên" : "Thêm mới Hướng dẫn viên",
                Content = view,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (window.ShowDialog() == true)
            {
                try
                {
                    if (!isEdit) _controller.Add(guide);
                    else _controller.Update(guide);

                    LoadData();
                    MessageBox.Show(isEdit ? "Cập nhật thành công!" : "Thêm mới thành công!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message);
                }
            }
        }

        // [MỚI] TÍNH NĂNG PHÂN CÔNG TOUR
        private void BtnAssign_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra đã chọn HDV chưa
            if (!(dgGuides.SelectedItem is Guide selectedGuide))
            {
                MessageBox.Show("Vui lòng chọn một Hướng dẫn viên để phân công!", "Chưa chọn HDV", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Lấy danh sách Tour sắp diễn ra
                var allSchedules = SearchEngine.Search<Schedule>(new List<SearchEngine.Condition>());

                var upcomingTours = allSchedules
                    .Where(s => s.DeDate > DateTime.Now && !s.IsCancel)
                    .OrderBy(s => s.DeDate)
                    .ToList();

                if (!upcomingTours.Any())
                {
                    MessageBox.Show("Không có lịch trình tour nào sắp diễn ra để phân công.", "Thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 3. Tạo cửa sổ chọn Tour
                Window assignWindow = new Window
                {
                    Title = $"Phân công cho HDV: {selectedGuide.FullName}",
                    Width = 900,
                    Height = 550,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize
                };

                Grid mainGrid = new Grid { Margin = new Thickness(10) };
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Label hướng dẫn
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Button Panel

                // Label hướng dẫn
                TextBlock lblNote = new TextBlock
                {
                    Text = "Chọn tour bên dưới để xem hành khách hoặc phân công.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14
                };
                Grid.SetRow(lblNote, 0);

                // DataGrid hiển thị danh sách Tour
                DataGrid dgTours = new DataGrid
                {
                    ItemsSource = upcomingTours,
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    SelectionMode = DataGridSelectionMode.Single,
                    Background = Brushes.WhiteSmoke,
                    RowHeaderWidth = 0,
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal
                };

                dgTours.Columns.Add(new DataGridTextColumn { Header = "Tên Tour", Binding = new Binding("Tour.TourName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
                dgTours.Columns.Add(new DataGridTextColumn { Header = "Ngày đi", Binding = new Binding("DeDate") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(150) });
                dgTours.Columns.Add(new DataGridTextColumn { Header = "Ngày về", Binding = new Binding("ReDate") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(150) });
                dgTours.Columns.Add(new DataGridTextColumn { Header = "HDV Hiện tại", Binding = new Binding("Guide.FullName") { TargetNullValue = "(Chưa có)" }, Width = new DataGridLength(150) });

                Grid.SetRow(dgTours, 1);

                // Panel chứa nút
                StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
                Grid.SetRow(btnPanel, 2);

                // Nút Xem Hành Khách
                Button btnViewPassengers = new Button
                {
                    Content = "👥 Xem Hành Khách",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = Cursors.Hand
                };

                // Nút Xác nhận
                Button btnConfirm = new Button
                {
                    Content = "✅ Xác Nhận Phân Công",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E44AD")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(15, 10, 15, 10),
                    Cursor = Cursors.Hand
                };

                btnPanel.Children.Add(btnViewPassengers);
                btnPanel.Children.Add(btnConfirm);

                // --- XỬ LÝ SỰ KIỆN XEM HÀNH KHÁCH ---
                btnViewPassengers.Click += (s, args) =>
                {
                    if (dgTours.SelectedItem is Schedule selectedSchedule)
                    {
                        try
                        {
                            // 1. Tìm tất cả Booking thuộc Schedule này
                            var bookingConditions = new List<SearchEngine.Condition>
                            {
                                new SearchEngine.Condition { Field = "ScheID", Operator = SearchEngine.Op.Equal, Value = selectedSchedule.Id }
                            };

                            // Search Booking sẽ tự động load list Passengers bên trong (nhờ SearchFactory)
                            var bookings = SearchEngine.Search<Booking>(bookingConditions).ToList();

                            // 2. Gom tất cả hành khách từ các booking lại
                            var allPassengers = new List<Passenger>();
                            foreach (var booking in bookings)
                            {
                                if (booking.Passengers != null)
                                {
                                    allPassengers.AddRange(booking.Passengers);
                                }
                            }

                            if (allPassengers.Count == 0)
                            {
                                MessageBox.Show("Chưa có hành khách nào đăng ký tour này.", "Thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            // 3. Hiển thị danh sách hành khách
                            Window passengerWindow = new Window
                            {
                                Title = $"Danh sách hành khách - Tour: {selectedSchedule.Tour?.TourName ?? "Không tên"}",
                                Width = 600,
                                Height = 400,
                                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                ResizeMode = ResizeMode.CanResize
                            };

                            DataGrid dgPassengers = new DataGrid
                            {
                                ItemsSource = allPassengers,
                                AutoGenerateColumns = false,
                                IsReadOnly = true,
                                Background = Brushes.White,
                                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                                RowHeaderWidth = 0
                            };

                            dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Họ và Tên", Binding = new Binding("FullName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                            dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Giới tính", Binding = new Binding("Gender"), Width = new DataGridLength(100) });
                            dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Ngày sinh", Binding = new Binding("DateOfBirth") { StringFormat = "dd/MM/yyyy" }, Width = new DataGridLength(120) });
                            dgPassengers.Columns.Add(new DataGridTextColumn { Header = "Loại khách", Binding = new Binding("PassengerType"), Width = new DataGridLength(100) });

                            passengerWindow.Content = dgPassengers;
                            passengerWindow.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi tải danh sách hành khách: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng chọn một tour để xem hành khách!", "Chưa chọn tour", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                // Xử lý sự kiện nút Xác nhận
                btnConfirm.Click += (s, args) =>
                {
                    if (dgTours.SelectedItem is Schedule selectedSchedule)
                    {
                        try
                        {
                            selectedSchedule.GuideID = selectedGuide.Id;

                            var scheduleController = (Controller.ScheduleController)Controller.ControllerFactory.Create(typeof(Schedule));
                            scheduleController.Update(selectedSchedule);

                            MessageBox.Show($"Đã phân công HDV {selectedGuide.FullName} cho tour thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            assignWindow.Close();
                        }
                        catch (Exception updateEx)
                        {
                            MessageBox.Show("Lỗi khi cập nhật: " + updateEx.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng chọn một tour trong danh sách!", "Chưa chọn tour", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                mainGrid.Children.Add(lblNote);
                mainGrid.Children.Add(dgTours);
                mainGrid.Children.Add(btnPanel);
                assignWindow.Content = mainGrid;

                assignWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách tour: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}