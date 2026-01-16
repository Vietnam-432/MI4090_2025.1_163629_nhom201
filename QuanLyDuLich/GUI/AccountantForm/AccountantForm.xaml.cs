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
using System.Windows.Data;

namespace QuanLyDuLich.GUI.AccountantForm
{
    /// <summary>
    /// Interaction logic for AccountantForm.xaml
    /// Chức năng dành cho Kế toán: Quản lý thanh toán, đơn hàng và báo cáo doanh thu.
    /// </summary>
    public partial class AccountantForm : UserControl
    {
        public event EventHandler RequestLogout;

        private User _currentUser;
        private ObservableCollection<Booking> _bookings;
        private ObservableCollection<Payment> _payments;

        public AccountantForm()
        {
            InitializeComponent();
            _currentUser = UserSession.Instance.CurrentUser;
            dpBaseTime.SelectedDate = DateTime.Now;
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            if (_currentUser != null)
            {
                lblAccountantName.Text = "Kế toán: " + _currentUser.UserName;
                txtUsername.Text = _currentUser.UserName;
            }

            RefreshBookings();
            RefreshPayments();
        }

        // ===================== QUẢN LÝ ĐƠN HÀNG (BOOKING) =====================

        private void RefreshBookings()
        {
            try
            {
                var bookings = SearchEngine.Search<Booking>(new List<SearchEngine.Condition>());
                _bookings = new ObservableCollection<Booking>(bookings);
                dgBookings.ItemsSource = _bookings;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách đơn hàng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSearchBooking_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearchBooking.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                RefreshBookings();
                return;
            }

            var conditions = new List<SearchEngine.Condition>
            {
                new SearchEngine.Condition
                {
                    Field = "Id",
                    Operator = SearchEngine.Op.Like,
                    Value = keyword
                }
            };

            try
            {
                dgBookings.ItemsSource = SearchEngine.Search<Booking>(conditions);
            }
            catch
            {
                RefreshBookings();
            }
        }

        /// <summary>
        /// Xử lý sự kiện làm mới danh sách đơn hàng và thanh toán.
        /// Giải quyết lỗi CS1061: Đảm bảo phương thức này tồn tại để XAML có thể gọi tới.
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshBookings();
            RefreshPayments();
        }

        // ===================== QUẢN LÝ THANH TOÁN (PAYMENT) =====================

        private void RefreshPayments()
        {
            try
            {
                var payments = Repository.Instance.GetAll<Payment>();
                _payments = new ObservableCollection<Payment>(payments);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tải lịch sử thanh toán: " + ex.Message);
            }
        }

        private void BtnCreatePayment_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as Button)?.DataContext as Booking;
            if (booking == null) return;

            // Kiểm tra nếu đã thanh toán
            if (booking.Status == "Paid" || booking.Status == "Đã thanh toán")
            {
                MessageBox.Show("Đơn hàng này đã được thanh toán thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                $"Xác nhận thu tiền cho đơn {booking.Id}?\nSố tiền: {booking.TotalAmount:N0} VNĐ",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // 1. Ghi nhận phiếu thu
                Payment payment = new Payment
                {
                    Id = $"PAY-{DateTime.Now:yyMMdd}-{Guid.NewGuid():N}".Substring(0, 16).ToUpper(),
                    Amount = booking.TotalAmount,
                    PaymentDate = DateTime.Now,
                    Method = "Tiền mặt / Chuyển khoản",
                    BookingId = booking.Id
                };

                Repository.Instance.Insert(payment);

                // 2. Cập nhật trạng thái đơn hàng
                booking.Status = "Paid";
                Repository.Instance.Update(booking);

                RefreshBookings();
                RefreshPayments();

                MessageBox.Show("Ghi nhận thanh toán thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi trong quá trình thu tiền: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as Button)?.DataContext as Booking;
            if (booking == null) return;

            if (booking.Status == "Paid" || booking.Status == "Đã thanh toán")
            {
                MessageBox.Show("Không thể thay đổi trạng thái của đơn hàng đã thanh toán.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nextStatus = booking.Status == "Pending" ? "Cancelled" : "Pending";

            if (MessageBox.Show(
                $"Bạn có chắc chắn muốn chuyển trạng thái đơn {booking.Id} sang '{nextStatus}'?",
                "Xác nhận thay đổi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                booking.Status = nextStatus;
                Repository.Instance.Update(booking);
                RefreshBookings();
                MessageBox.Show("Cập nhật trạng thái thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message);
            }
        }

        // ===================== BÁO CÁO THỐNG KÊ (REPORT) =====================

        private void BtnLoadReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(cbReportType.SelectedItem is ComboBoxItem reportItem)) return;
                if (!(cbTimeType.SelectedItem is ComboBoxItem timeItem)) return;

                if (!Enum.TryParse(reportItem.Tag.ToString(), out Report.ReportType reportType))
                    throw new Exception("Loại báo cáo không hợp lệ.");

                Report.TimeType timeType;
                string timeTag = timeItem.Tag.ToString();

                // Xử lý logic Month an toàn
                if (timeTag == "Month")
                {
                    timeType = Report.TimeType.Day;
                }
                else if (!Enum.TryParse(timeTag, out timeType))
                {
                    throw new Exception("Loại thời gian không hợp lệ.");
                }

                DateTime baseDate = dpBaseTime.SelectedDate ?? DateTime.Now;

                var data = Report.ReportDataSource<dynamic>(timeType, reportType, baseDate);
                dgReport.ItemsSource = data;

                UpdateReportSummary(data, reportType);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết xuất báo cáo: " + ex.Message, "Lỗi báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateReportSummary(IEnumerable<dynamic> data, Report.ReportType type)
        {
            if (data == null || !data.Any())
            {
                lblTotalRevenue.Text = "0";
                return;
            }

            try
            {
                switch (type)
                {
                    case Report.ReportType.BusinessOverview:
                        lblSummaryTitle.Text = "TỔNG DOANH THU:";
                        var row = data.First() as IDictionary<string, object>;
                        decimal overviewTotal = Convert.ToDecimal(row?["TotalRevenue"] ?? 0);
                        lblTotalRevenue.Text = overviewTotal.ToString("N0") + " VNĐ";
                        break;

                    case Report.ReportType.BestSellingTours:
                        lblSummaryTitle.Text = "DOANH THU TOUR:";
                        decimal tourTotal = data.Sum(x =>
                        {
                            var d = x as IDictionary<string, object>;
                            return Convert.ToDecimal(d?["Revenue"] ?? 0);
                        });
                        lblTotalRevenue.Text = tourTotal.ToString("N0") + " VNĐ";
                        break;

                    case Report.ReportType.GuideReport:
                        lblSummaryTitle.Text = "TỔNG KHÁCH:";
                        long passengerCount = data.Sum(x =>
                        {
                            var d = x as IDictionary<string, object>;
                            return Convert.ToInt64(d?["TotalPassengers"] ?? 0);
                        });
                        lblTotalRevenue.Text = passengerCount.ToString("N0") + " khách";
                        break;
                }
            }
            catch (Exception ex)
            {
                lblTotalRevenue.Text = "Lỗi dữ liệu";
                Console.WriteLine("Lỗi summary: " + ex.Message);
            }
        }

        // ===================== TÀI KHOẢN (ACCOUNT) =====================

        private void BtnUpdateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(pbPassword.Password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu mới nếu muốn thay đổi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _currentUser.Password = pbPassword.Password;
                Repository.Instance.Update(_currentUser);
                pbPassword.Clear();
                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RequestLogout?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // ===================== BỘ CHUYỂN ĐỔI GIAO DIỆN (CONVERTER) =====================

    public class StatusToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return true;
            string status = value.ToString();

            return !(status == "Paid"
                  || status == "Completed"
                  || status == "Locked"
                  || status == "Đã thanh toán");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}