using DAL;
using DTO;
namespace BUS
{
    public class Controller
    {

        public abstract class BaseController<T> where T : class, IEntity
        {
            protected Repository _repo = Repository.Instance;
            protected abstract void Validate(T entity);

            public virtual void Add(T entity)
            {
                Validate(entity);
        
                        _repo.Insert<T>(entity);
                   
                
            }

            public virtual void Update(T entity)
            {
                Validate(entity);

               
                    _repo.Update<T>(entity);
               
               
            }

            public virtual void Delete(string id)
            {
                
                    _repo.Delete<T>(id);
               
            }

            public virtual IEnumerable<T> GetAll()
            {
                try
                {
                    var result = _repo.GetAll<T>();
                    return result ?? new List<T>(); // Nếu null thì trả về danh sách rỗng để không lỗi
                }
                catch
                {
                    return new List<T>(); // Nếu lỗi (bảng chưa có) thì trả về danh sách rỗng
                }
            }
        }
        public class UserController : BaseController<User>
        {
            protected override void Validate(User newUser)
            {
                if (string.IsNullOrWhiteSpace(newUser.UserName) || string.IsNullOrWhiteSpace(newUser.Password))
                    throw new Exception("Vui lòng nhập tên đăng nhập và mật khẩu!");
                if (!UserSession.Instance.IsAdmin)
                    throw new Exception("Truy cập trái phép bảng Users");
            }
            public void Login(string username, string password)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        throw new Exception("Vui lòng nhập tên đăng nhập và mật khẩu!");
                    }
                    string sql = "SELECT * FROM Users WHERE UserName = @UserName";
                    var param = new { UserName = username };
                    var resultList = Repository.Instance.Search<User>(sql, param);
                    var user = resultList.FirstOrDefault();
                    if (user == null)
                    {
                        throw new Exception("Tài khoản không tồn tại!");
                    }
                    if (user.Password != password)
                    {
                        throw new Exception("Mật khẩu không đúng!");
                    }
                    UserSession.Instance.Login(user);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            public void Logout()
            {
                UserSession.Instance.Logout();
            }
            public void AddUser(User newuser)
            {
                if (UserSession.Instance.IsAdmin)
                {
                    Repository.Instance.Insert<User>(newuser);
                }
                else
                    throw new Exception("Không có quyền thêm tài khoản");
            }
            public void DeleteUser(string username)
            {

                if (UserSession.Instance.IsAdmin)
                {

                    string sql = "SELECT * FROM Users WHERE UserName = @User";
                    var param = new { User = username };


                    var foundUsers = Repository.Instance.Search<User>(sql, param);


                    var targetUser = foundUsers.FirstOrDefault();


                    if (targetUser == null)
                    {
                        throw new Exception($"Không tìm thấy tài khoản có tên: {username}");
                    }


                    if (targetUser.Id == UserSession.Instance.CurrentUser.Id)
                    {
                        throw new Exception("Bạn không thể tự xóa tài khoản đang đăng nhập!");
                    }

                    Repository.Instance.Delete<User>(targetUser.Id);
                }
                else
                {
                    throw new Exception("Bạn không có quyền xóa tài khoản!");
                }
            }
        }
        public class TourController : BaseController<Tour>
        {
            protected override void Validate(Tour newTour)
            {
                if (string.IsNullOrWhiteSpace(newTour.Id))
                    throw new Exception("Không được để trống ID Tour");

                if (string.IsNullOrWhiteSpace(newTour.TourName))
                    throw new Exception("Không được để trống tên Tour");

                if (newTour.BasePrice < 0)
                    throw new Exception("Giá tiền không hợp lệ");

                if (newTour.Dura <= 0)
                    throw new Exception("Số ngày diễn ra không hợp lệ");

                if (string.IsNullOrWhiteSpace(newTour.TourType))
                    throw new Exception("Kiểu tour không hợp lệ");
            }
        }
        public class ScheduleController : BaseController<Schedule>
        {
            protected override void Validate(Schedule s)
            {
                if (string.IsNullOrWhiteSpace(s.Id)) throw new Exception("Mã lịch trình không được trống");
                if (s.DeDate >= s.ReDate) throw new Exception("Ngày về phải sau ngày đi");
                if (s.MaxCap <= 0) throw new Exception("Số chỗ tối đa phải lớn hơn 0");
                if (string.IsNullOrWhiteSpace(s.TourId)) throw new Exception("Phải chọn Tour cho lịch trình");
               
            }
        }
        public class ItineraryController: BaseController<Itinerary>
        {
            protected override void Validate(Itinerary i)
            {
                if (string.IsNullOrWhiteSpace(i.Id)) throw new Exception("ID không được trống");
                if (i.DayNumber <= 0) throw new Exception("Ngày thứ mấy phải lớn hơn 0");
                if (string.IsNullOrWhiteSpace(i.Title)) throw new Exception("Tiêu đề không được trống");
            }
        }
        public class GuideController:BaseController<Guide>
        {
            protected override void Validate(Guide g)
            {
                if (string.IsNullOrWhiteSpace(g.Id)) throw new Exception("Mã HDV không được trống");
                if (string.IsNullOrWhiteSpace(g.FullName)) throw new Exception("Tên HDV không được trống");
                if (g.ExYears < 0) throw new Exception("Số năm kinh nghiệm không hợp lệ");
            }
        }
        public class BookingController : BaseController<Booking>
        {
            protected override void Validate(Booking b)
            {
                if (string.IsNullOrWhiteSpace(b.Id)) throw new Exception("Mã Booking không được trống");
                if (b.TotalAmount < 0) throw new Exception("Tổng tiền không được âm");
                
               
            }
        }
        public class CustomerController : BaseController<Customer>
        {
            protected override void Validate(Customer c)
            {
                if (string.IsNullOrWhiteSpace(c.Id)) throw new Exception("Mã KH không được trống");
                if (string.IsNullOrWhiteSpace(c.FullName)) throw new Exception("Tên KH không được trống");
                if (string.IsNullOrWhiteSpace(c.PhoneNumber)) throw new Exception("SĐT không được trống");
                // Có thể thêm regex kiểm tra SĐT nếu cần
            }
        }
        public class PassengerController : BaseController<Passenger>
        {
            protected override void Validate(Passenger p)
            {
                if (string.IsNullOrWhiteSpace(p.Id)) throw new Exception("ID hành khách không được trống");
                if (string.IsNullOrWhiteSpace(p.FullName)) throw new Exception("Tên hành khách không được trống");
                if (string.IsNullOrWhiteSpace(p.BookingId)) throw new Exception("Phải gắn với mã Booking nào đó");
            }
        }
        public class PaymentController : BaseController<Payment>
        {
            protected override void Validate(Payment p)
            {
                if (string.IsNullOrWhiteSpace(p.Id)) throw new Exception("Mã thanh toán không được trống");
                if (p.Amount < 0) throw new Exception("Số tiền thanh toán phải không âm");
                if (string.IsNullOrWhiteSpace(p.BookingId)) throw new Exception("Phải thanh toán cho Booking nào đó");
            }
        }
        public class VendorController : BaseController<Vendor>
        {
            protected override void Validate(Vendor v)
            {
                if (string.IsNullOrWhiteSpace(v.Id)) throw new Exception("Mã nhà cung cấp không được trống");
                if (string.IsNullOrWhiteSpace(v.VendorName)) throw new Exception("Tên nhà cung cấp không được trống");
            }
        }
        public class ServiceController : BaseController<Service>
        {
            protected override void Validate(Service s)
            {
                if (string.IsNullOrWhiteSpace(s.Id)) throw new Exception("Mã dịch vụ không được trống");
                if (string.IsNullOrWhiteSpace(s.ServiceName)) throw new Exception("Tên dịch vụ không được trống");
                if (s.EstimatedCost < 0) throw new Exception("Chi phí dự kiến không được âm");
            }
        }
        public static class ControllerFactory
        {
            public static object Create(Type dtoType)
            {
                if (dtoType == typeof(Tour)) return new Controller.TourController();
                if (dtoType == typeof(Customer)) return new Controller.CustomerController();
                if (dtoType == typeof(Booking)) return new Controller.BookingController();
                if (dtoType == typeof(Service)) return new Controller.ServiceController();
                if (dtoType == typeof(Schedule)) return new Controller.ScheduleController();
                if (dtoType == typeof(Guide)) return new Controller.GuideController();
                if (dtoType == typeof(Payment)) return new Controller.PaymentController();
                if (dtoType == typeof(Vendor)) return new Controller.VendorController();
                if (dtoType == typeof(Passenger)) return new Controller.PassengerController();
                if (dtoType == typeof(User)) return new Controller.UserController();
                if (dtoType == typeof(Itinerary)) return new Controller.ItineraryController();
                throw new Exception("Chưa hỗ trợ Controller cho " + dtoType.Name);
            }
        }
    }
    
}
