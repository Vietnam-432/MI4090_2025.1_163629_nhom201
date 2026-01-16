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
    public partial class UserView : UserControl
    {
        private ObservableCollection<User> _users;
        private Controller.UserController _userController;

        public UserView()
        {
            InitializeComponent();
            _userController = new Controller.UserController();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Sử dụng Controller để lấy danh sách User
                // Lưu ý: BaseController.GetAll() có thể trả về danh sách rỗng nếu lỗi
                var userList = _userController.GetAll();

                // Chuyển đổi sang ObservableCollection để binding lên UI
                _users = new ObservableCollection<User>(userList);
                dgData.ItemsSource = _users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                _users = new ObservableCollection<User>();
                dgData.ItemsSource = _users;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Tạo User mới với ID tự sinh
            User newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                // Gán giá trị mặc định để tránh lỗi validate nếu người dùng quên nhập
                RoleId = User.RoleIdType.Emp
            };

            ShowCRUWindow(newUser, false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            User selected = dgData.SelectedItem as User;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn người dùng cần sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Clone object để tránh sửa trực tiếp trên Grid khi chưa bấm Lưu
            // Tuy nhiên với CRUView hiện tại đang binding trực tiếp, ta sẽ refresh lại nếu cancel (hoặc reload)
            ShowCRUWindow(selected, true);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            User selected = dgData.SelectedItem as User;
            if (selected == null) return;

            if (MessageBox.Show($"Xóa người dùng '{selected.UserName}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Gọi Controller để xóa (có kiểm tra quyền Admin bên trong)
                    _userController.DeleteUser(selected.UserName); // UserController dùng DeleteUser(username) thay vì Delete(id)
                    _users.Remove(selected);
                    MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowCRUWindow(User user, bool isEdit)
        {
            Window window = new Window
            {
                Title = isEdit ? "Cập Nhật User" : "Thêm User Mới",
                Content = new CRUView(user, isEdit),
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
                        // Thêm mới
                        _userController.AddUser(user);
                        _users.Add(user);
                    }
                    else
                    {
                        // Cập nhật
                        _userController.Update(user);
                        dgData.Items.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Nếu lỗi khi thêm mới thì remove khỏi list (nếu đã add - ở đây chưa add vào list UI nên ko sao)
                    // Nếu lỗi khi update, dữ liệu trên UI object đã bị thay đổi bởi CRUView, nên reload lại data
                    if (isEdit) LoadData();
                }
            }
            else
            {
                // Nếu user hủy bỏ (Cancel/Close window), reload lại data để revert các thay đổi trên object (do binding)
                if (isEdit) LoadData();
            }
        }
    }
}