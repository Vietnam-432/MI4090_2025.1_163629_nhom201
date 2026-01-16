using BUS;
using DTO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for CRUView.xaml
    /// </summary>
    public partial class CRUView : UserControl
    {
        private IEntity _entity;
        private bool _isEditMode;

        // Dictionary lưu cặp: PropertyInfo - Control (FrameworkElement trong WPF)
        private Dictionary<PropertyInfo, FrameworkElement> _controlMap = new Dictionary<PropertyInfo, FrameworkElement>();

        public CRUView(IEntity entity, bool isEditMode = false)
        {
            InitializeComponent();

            this._entity = entity;
            this._isEditMode = isEditMode;

            // Đặt tiêu đề form dựa trên loại đối tượng
            string action = isEditMode ? "CẬP NHẬT" : "THÊM MỚI";
            string entityName = entity.GetType().Name.ToUpper();
            lblTitle.Text = $"{action} {entityName}";

            GenerateControls();
        }

        private void GenerateControls()
        {
            Type type = _entity.GetType();
            // Lấy tất cả property public
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                // Bỏ qua các thuộc tính không cần thiết
                if (prop.Name == "IsDirty") continue;

                // --- BỘ LỌC KIỂU DỮ LIỆU ---
                Type realType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                // Kiểm tra xem có phải là List<T> không
                bool isList = prop.PropertyType.IsGenericType &&
                              prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>);

                // Whitelist: Chỉ hiển thị kiểu đơn giản HOẶC là List
                bool isSimpleType = realType.IsPrimitive
                                    || realType == typeof(string)
                                    || realType == typeof(DateTime)
                                    || realType == typeof(decimal)
                                    || realType == typeof(double);

                // Nếu không phải simple type VÀ không phải List thì bỏ qua
                if (!isSimpleType && !isList) continue;

                // --- TẠO GIAO DIỆN TỪNG DÒNG (StackPanel ngang) ---
                StackPanel row = new StackPanel
                {
                    Orientation = Orientation.Vertical, // Xếp dọc cho đẹp: Label trên, Input dưới
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // 1. Tạo Label (TextBlock trong WPF)
                string labelText = prop.Name;
                // Nếu là List thì thêm chú thích cho rõ
                if (isList) labelText += " (Danh sách)";

                TextBlock lbl = new TextBlock
                {
                    Text = labelText,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkSlateGray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                row.Children.Add(lbl);

                // 2. Tạo Control nhập liệu tương ứng
                FrameworkElement inputControl = null;
                object value = prop.GetValue(_entity);

                // --- XỬ LÝ LIST<T> (NEW FEATURE) ---
                if (isList)
                {
                    StackPanel listPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // Nút thêm (+)
                    Button btnAdd = new Button
                    {
                        Content = "+",
                        FontWeight = FontWeights.Bold,
                        Width = 40,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0),
                        Background = Brushes.CornflowerBlue,
                        Foreground = Brushes.White,
                        ToolTip = "Thêm phần tử mới"
                    };

                    // Nút Reset (↺)
                    Button btnReset = new Button
                    {
                        Content = "↺",
                        FontWeight = FontWeights.Bold,
                        Width = 40,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0),
                        Background = Brushes.IndianRed,
                        Foreground = Brushes.White,
                        ToolTip = "Xóa hết danh sách"
                    };

                    // Hiển thị số lượng hiện tại
                    IList currentList = value as IList;
                    int count = currentList != null ? currentList.Count : 0;
                    TextBlock lblCount = new TextBlock
                    {
                        Text = $"{count} mục đã thêm (Sẽ lưu khi bấm 'Lưu lại')",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Gray
                    };

                    // Xử lý sự kiện khi ấn nút +
                    btnAdd.Click += (s, e) =>
                    {
                        // 1. Xác định kiểu dữ liệu của phần tử trong List (Ví dụ: List<Service> -> Service)
                        Type itemType = prop.PropertyType.GetGenericArguments()[0];

                        // 2. Kiểm tra xem itemType có implement IEntity không (để dùng được CRUView)
                        if (!typeof(IEntity).IsAssignableFrom(itemType))
                        {
                            MessageBox.Show($"Kiểu dữ liệu {itemType.Name} không hỗ trợ nhập liệu tự động (Cần implement IEntity).");
                            return;
                        }

                        // 3. Tạo đối tượng mới
                        IEntity newItem = (IEntity)Activator.CreateInstance(itemType);
                        // Tự sinh ID cho item con luôn
                        newItem.Id = Guid.NewGuid().ToString();

                        // --- [MỚI] TỰ ĐỘNG GÁN KHÓA NGOẠI (FOREIGN KEY) ---
                        // Lấy tên bảng của cha (Parent Entity), ví dụ Tour -> "Tours"
                        // Ta tìm attribute có tên là "TableAttribute" (của Dapper)
                        string parentTableName = "";
                        var parentAttributes = _entity.GetType().GetCustomAttributes(false);
                        var tableAttr = parentAttributes.FirstOrDefault(a => a.GetType().Name == "TableAttribute");

                        if (tableAttr != null)
                        {
                            // Dùng Reflection lấy property 'Name' của TableAttribute
                            PropertyInfo nameProp = tableAttr.GetType().GetProperty("Name");
                            if (nameProp != null)
                            {
                                parentTableName = nameProp.GetValue(tableAttr) as string;
                            }
                        }

                        // Nếu tìm được tên bảng cha, duyệt qua các property của con để tìm ForeignKey khớp
                        if (!string.IsNullOrEmpty(parentTableName))
                        {
                            foreach (var childProp in itemType.GetProperties())
                            {
                                var fkAttr = childProp.GetCustomAttribute<ForeignKeyAttribute>();
                                // Kiểm tra nếu ForeignTableName trỏ đúng vào bảng cha
                                if (fkAttr != null && fkAttr.ForeignTableName == parentTableName)
                                {
                                    // Gán ID của cha vào thuộc tính khóa ngoại của con
                                    if (childProp.PropertyType == typeof(string))
                                    {
                                        childProp.SetValue(newItem, _entity.Id);
                                    }
                                }
                            }
                        }
                        // ----------------------------------------------------

                        // 4. Mở cửa sổ mới chứa CRUView cho đối tượng con
                        Window childWindow = new Window
                        {
                            Title = "Thêm " + itemType.Name,
                            Content = new CRUView(newItem), // Đệ quy CRUView
                            SizeToContent = SizeToContent.WidthAndHeight,
                            ResizeMode = ResizeMode.NoResize,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Padding = new Thickness(10)
                        };

                        // 5. Nếu User ấn Lưu ở cửa sổ con (DialogResult = true)
                        if (childWindow.ShowDialog() == true)
                        {
                            // Lấy lại List hiện tại từ entity cha (đề phòng null)
                            var list = prop.GetValue(_entity);
                            if (list == null)
                            {
                                // Nếu List chưa khởi tạo -> Khởi tạo mới
                                list = Activator.CreateInstance(prop.PropertyType);
                                prop.SetValue(_entity, list);
                            }

                            // Thêm item mới vào List (chưa lưu DB ngay, chờ bấm Save cha)
                            ((IList)list).Add(newItem);

                            // Cập nhật lại số lượng hiển thị trên UI cha
                            lblCount.Text = $"{((IList)list).Count} mục đã thêm (Sẽ lưu khi bấm 'Lưu lại')";
                        }
                    };

                    // [MỚI] Xử lý sự kiện khi ấn nút Reset (Xóa hết danh sách)
                    btnReset.Click += (s, e) =>
                    {
                        var list = prop.GetValue(_entity) as IList;
                        if (list != null && list.Count > 0)
                        {
                            if (MessageBox.Show("Bạn có chắc muốn xóa hết danh sách các mục con này không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                list.Clear();
                                lblCount.Text = $"0 mục đã thêm (Sẽ lưu khi bấm 'Lưu lại')";
                            }
                        }
                    };

                    listPanel.Children.Add(btnAdd);
                    listPanel.Children.Add(btnReset); // Thêm nút reset vào panel
                    listPanel.Children.Add(lblCount);
                    inputControl = listPanel;
                }
                // --- XỬ LÝ DATETIME ---
                else if (realType == typeof(DateTime))
                {
                    DatePicker dtp = new DatePicker
                    {
                        Padding = new Thickness(5),
                        Height = 35,
                        SelectedDate = (value != null && (DateTime)value != DateTime.MinValue) ? (DateTime?)value : DateTime.Now
                    };
                    inputControl = dtp;
                }
                // --- XỬ LÝ BOOLEAN (CheckBox) ---
                else if (realType == typeof(bool))
                {
                    CheckBox chk = new CheckBox
                    {
                        Content = "Đã kích hoạt / Xác nhận",
                        IsChecked = value != null && (bool)value
                    };
                    inputControl = chk;
                }
                // --- XỬ LÝ SỐ & CHỮ (TextBox) ---
                else
                {
                    TextBox txt = new TextBox
                    {
                        Padding = new Thickness(5),
                        Height = 35,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Text = value?.ToString() ?? ""
                    };
                    inputControl = txt;
                }

                // --- LOGIC KHÓA ID KHI SỬA ---
                if (prop.Name == "Id")
                {
                    if (_isEditMode)
                    {
                        inputControl.IsEnabled = false;
                        inputControl.Opacity = 0.6; // Làm mờ đi
                    }
                }

                // --- XỬ LÝ ATTRIBUTE [DisplayOnly] ---
                if (prop.GetCustomAttribute<DisplayOnlyAttribute>() != null)
                {
                    inputControl.IsEnabled = false;
                    inputControl.Opacity = 0.6;
                }

                // Thêm vào Panel và Dictionary
                row.Children.Add(inputControl);
                MainPanel.Children.Add(row);

                // CHÚ Ý: Với List, ta không cần thêm vào _controlMap để lấy giá trị từ UI,
                // vì giá trị đã được add vào List của Entity cha khi đóng cửa sổ con rồi.
                if (!isList)
                {
                    _controlMap.Add(prop, inputControl);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Cập nhật các giá trị đơn giản từ UI vào Entity cha
                foreach (var item in _controlMap)
                {
                    var prop = item.Key;
                    var ctrl = item.Value;

                    // Nếu là ID và đang sửa thì không update lại (tránh lỗi)
                    if (prop.Name == "Id" && _isEditMode) continue;
                    // Nếu là DisplayOnly thì bỏ qua
                    if (prop.GetCustomAttribute<DisplayOnlyAttribute>() != null) continue;

                    object newValue = null;

                    // Lấy giá trị từ Control WPF
                    if (ctrl is DatePicker dtp)
                    {
                        newValue = dtp.SelectedDate ?? DateTime.Now;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        newValue = chk.IsChecked ?? false;
                    }
                    else if (ctrl is TextBox txt)
                    {
                        // Xử lý convert string sang số
                        if (prop.PropertyType == typeof(int))
                            newValue = string.IsNullOrWhiteSpace(txt.Text) ? 0 : int.Parse(txt.Text);
                        else if (prop.PropertyType == typeof(double))
                            newValue = string.IsNullOrWhiteSpace(txt.Text) ? 0.0 : double.Parse(txt.Text);
                        else
                            newValue = txt.Text;
                    }

                    // Gán giá trị ngược lại vào Entity
                    if (newValue != null)
                    {
                        Type t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(_entity, Convert.ChangeType(newValue, t));
                    }
                }

                // 2. [QUAN TRỌNG] Lưu danh sách con xuống Database
                // Duyệt qua tất cả property của Entity để tìm List
                PropertyInfo[] properties = _entity.GetType().GetProperties();
                foreach (PropertyInfo prop in properties)
                {
                    if (prop.PropertyType.IsGenericType &&
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // Lấy danh sách con hiện tại
                        var listVal = prop.GetValue(_entity) as IList;
                        if (listVal != null && listVal.Count > 0)
                        {
                            // Lấy kiểu dữ liệu của phần tử con (Ví dụ: Service)
                            Type itemType = prop.PropertyType.GetGenericArguments()[0];

                            // Tạo Controller tương ứng cho kiểu con này
                            object controller = null;
                            try
                            {
                                controller = Controller.ControllerFactory.Create(itemType);
                            }
                            catch { continue; } // Nếu không tạo được controller thì bỏ qua

                            // Lấy Method "Add" hoặc "Update" của controller
                            // Lưu ý: ControllerFactory trả về object, cần dùng Reflection để gọi hàm Add
                            MethodInfo addMethod = controller.GetType().GetMethod("Add");
                            MethodInfo updateMethod = controller.GetType().GetMethod("Update");

                            // Duyệt từng phần tử trong list con và lưu
                            foreach (var item in listVal)
                            {
                                IEntity childEntity = item as IEntity;
                                if (childEntity != null)
                                {
                                    // Đảm bảo khóa ngoại trỏ về cha đúng (phòng khi ID cha mới được sinh)
                                    UpdateForeignKey(childEntity, _entity);

                                    // Gọi hàm Add của Controller con
                                    // Ở đây ta giả định là Add mới (hoặc bạn có thể check ID để quyết định Update)
                                    // Do logic thêm list con thường là thêm mới, nên dùng Add.
                                    // Nếu muốn chặt chẽ: check xem item đã tồn tại trong DB chưa.
                                    try
                                    {
                                        addMethod.Invoke(controller, new object[] { childEntity });
                                    }
                                    catch
                                    {
                                        // Nếu Add lỗi (do trùng ID chẳng hạn), thử Update
                                        if (updateMethod != null)
                                            updateMethod.Invoke(controller, new object[] { childEntity });
                                    }
                                }
                            }
                        }
                    }
                }

                MessageBox.Show("Thành công");
                Window parentWindow = Window.GetWindow(this);

                if (parentWindow != null)
                {
                    // Báo cho cửa sổ cha biết là đã Save thành công
                    parentWindow.DialogResult = true;
                    parentWindow.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nhập liệu: " + ex.Message + "\n" + ex.InnerException?.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hàm hỗ trợ cập nhật lại Foreign Key cho con trỏ về cha (phòng trường hợp ID cha thay đổi hoặc chưa gán)
        private void UpdateForeignKey(IEntity child, IEntity parent)
        {
            string parentTableName = "";
            var parentAttributes = parent.GetType().GetCustomAttributes(false);
            var tableAttr = parentAttributes.FirstOrDefault(a => a.GetType().Name == "TableAttribute");

            if (tableAttr != null)
            {
                PropertyInfo nameProp = tableAttr.GetType().GetProperty("Name");
                if (nameProp != null) parentTableName = nameProp.GetValue(tableAttr) as string;
            }

            if (!string.IsNullOrEmpty(parentTableName))
            {
                foreach (var childProp in child.GetType().GetProperties())
                {
                    var fkAttr = childProp.GetCustomAttribute<ForeignKeyAttribute>();
                    if (fkAttr != null && fkAttr.ForeignTableName == parentTableName)
                    {
                        if (childProp.PropertyType == typeof(string))
                        {
                            childProp.SetValue(child, parent.Id);
                        }
                    }
                }
            }
        }
    }
}