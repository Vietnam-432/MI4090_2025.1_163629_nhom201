using Dapper;
using Dapper.Contrib.Extensions;
using DTO;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DAL
{
    public class Repository
    {
        private static readonly Repository _instance = new Repository();
        public static Repository Instance => _instance;

        private readonly string _connectionString = "Data Source=DATA.db";

        private Repository()
        {
            if (!File.Exists("DATA.db"))
                InitDatabase();
        }

        #region INIT DATABASE 
        private void InitDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            conn.Execute("PRAGMA foreign_keys = ON;");
            

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // 2. Chuỗi SQL tổng hợp (Bao gồm Table và Trigger)
                    string sqlScript = @"
                    -- TẠO CÁC BẢNG
                    CREATE TABLE IF NOT EXISTS Customers (
                        Id TEXT PRIMARY KEY, FullName TEXT, PhoneNumber TEXT, Email TEXT, Address TEXT, CitizenId TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Guides (
                        Id TEXT PRIMARY KEY, FullName TEXT, PhoneNum TEXT, Language TEXT, ExYears INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Tours (
                        Id TEXT PRIMARY KEY, TourName TEXT, BasePrice REAL, Dura INTEGER, TourType TEXT, Description TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Vendors (
                        Id TEXT PRIMARY KEY, VendorName TEXT, PhoneNumber TEXT, Address TEXT, VendorType TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Schedules (
                        Id TEXT PRIMARY KEY, DeDate TEXT, ReDate TEXT, MaxCap INTEGER, Booked INTEGER, 
                        TourId TEXT, GuideID TEXT, IsCancel INTEGER,
                        FOREIGN KEY(TourId) REFERENCES Tours(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Bookings (
                        Id TEXT PRIMARY KEY, BookDate TEXT, TotalAmount REAL, Status TEXT, 
                        AdultCount INTEGER, ChildCount INTEGER, CusID TEXT, ScheID TEXT,
                        FOREIGN KEY(CusID) REFERENCES Customers(Id),
                        FOREIGN KEY(ScheID) REFERENCES Schedules(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Passengers (
                        Id TEXT PRIMARY KEY, FullName TEXT, DateOfBirth TEXT, Gender TEXT, 
                        PassengerType TEXT, BookingId TEXT,
                        FOREIGN KEY(BookingId) REFERENCES Bookings(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Payments (
                        Id TEXT PRIMARY KEY, Amount REAL, PaymentDate TEXT, Method TEXT, BookingId TEXT,
                        FOREIGN KEY(BookingId) REFERENCES Bookings(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Itinerarys (
                        Id TEXT PRIMARY KEY, DayNumber INTEGER, Title TEXT, Description TEXT, TourId TEXT,
                        FOREIGN KEY(TourId) REFERENCES Tours(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Services (
                        Id TEXT PRIMARY KEY, ServiceName TEXT, EstimatedCost REAL, Unit TEXT, TourId TEXT, VendorId TEXT,
                        FOREIGN KEY(TourId) REFERENCES Tours(Id),
                        FOREIGN KEY(VendorId) REFERENCES Vendors(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Users (
                        Id TEXT PRIMARY KEY, UserName TEXT NOT NULL, Password TEXT NOT NULL, RoleId INTEGER
                    );

                    -- TẠO CÁC TRIGGER
                    CREATE TRIGGER IF NOT EXISTS Auto_Calculate_Booking_Price
                    AFTER UPDATE OF AdultCount, ChildCount ON Bookings
                    BEGIN
                        UPDATE Bookings
                        SET TotalAmount = (
                            SELECT (NEW.AdultCount * t.BasePrice) + (NEW.ChildCount * t.BasePrice * 0.5)
                            FROM Schedules s
                            JOIN Tours t ON s.TourId = t.Id
                            WHERE s.Id = NEW.ScheID
                        )
                        WHERE Id = NEW.Id;
                    END;

                    CREATE TRIGGER IF NOT EXISTS Prevent_Overbooking
                    BEFORE INSERT ON Bookings
                    BEGIN
                        SELECT CASE 
                            WHEN (SELECT Booked FROM Schedules WHERE Id = NEW.ScheID) + NEW.AdultCount + NEW.ChildCount > 
                                 (SELECT MaxCap FROM Schedules WHERE Id = NEW.ScheID)
                            THEN RAISE(ABORT, 'Lỗi: Lịch trình này đã hết chỗ!')
                        END;
                    END;

                    CREATE TRIGGER IF NOT EXISTS Update_Schedule_Booked_Insert
                    AFTER INSERT ON Bookings
                    BEGIN
                        UPDATE Schedules SET Booked = Booked + NEW.AdultCount + NEW.ChildCount WHERE Id = NEW.ScheID;
                    END;

                    CREATE TRIGGER IF NOT EXISTS Validate_Schedule_Dates
                    BEFORE INSERT ON Schedules
                    BEGIN
                        SELECT CASE 
                            WHEN NEW.DeDate >= NEW.ReDate
                            THEN RAISE(ABORT, 'Lỗi: Ngày về phải sau ngày khởi hành!')
                        END;
                    END;
                   INSERT INTO Users (Id, UserName, Password, RoleId) VALUES ('admin', 'admin', 'admin', 0);
                    ";

                    connection.Execute(sqlScript);
                    Console.WriteLine("Database initialized successfully.");
                }
            }


        #endregion

        #region BASIC CRUD
        public IEnumerable<T> GetAll<T>() where T : class
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var data = conn.GetAll<T>();
            return data ?? Enumerable.Empty<T>();
        }
        public void Insert<T>(T entity) where T : class, IEntity
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            conn.Insert(entity);
        }

        public void Update<T>(T entity) where T : class, IEntity
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            conn.Update(entity);
        }

        public void Delete<T>(string id) where T : class, IEntity
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var table = typeof(T).Name + "s";
            conn.Execute($"DELETE FROM {table} WHERE Id = @Id", new { Id = id });
        }
        #endregion

        #region SEARCH 
        public IEnumerable<T> Search<T>(string sql, object param = null)
            where T : class
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            conn.Execute("PRAGMA foreign_keys = ON;");

            var list = conn.Query<T>(sql, param).ToList();

            return SearchFactory.GetMoreInfo(list, conn);
        }
        #endregion

        // ==========================================================
        // ================= SEARCH FACTORY =========================
        // ==========================================================
        public static class SearchFactory
        {
            public static IEnumerable<T> GetMoreInfo<T>(
                IEnumerable<T> list,
                SqliteConnection conn
            ) where T : class
            {
                if (!list.Any())
                    return list;

                // ================= USER =================
                if (typeof(T) == typeof(User))
                    return list;

                // ================= TOUR =================
                if (typeof(T) == typeof(Tour))
                {
                    var tours = list.Cast<Tour>().ToList();
                    var ids = tours.Select(t => t.Id).ToList();

                    var schedules = conn.Query<Schedule>(
                        "SELECT * FROM Schedules WHERE TourId IN @Ids",
                        new { Ids = ids }).ToList();

                    var itineraries = conn.Query<Itinerary>(
                        "SELECT * FROM Itinerarys WHERE TourId IN @Ids",
                        new { Ids = ids }).ToList();

                    var services = conn.Query<Service>(
                        "SELECT * FROM Services WHERE TourId IN @Ids",
                        new { Ids = ids }).ToList();

                    foreach (var t in tours)
                    {
                        t.Schedules = schedules.Where(s => s.TourId == t.Id).ToList();
                        t.Itineraries = itineraries.Where(i => i.TourId == t.Id).ToList();
                        t.Services = services.Where(s => s.TourId == t.Id).ToList();
                    }

                    return tours.Cast<T>();
                }

                // ================= BOOKING =================
                if (typeof(T) == typeof(Booking))
                {
                    var bookings = list.Cast<Booking>().ToList();

                    var bookingIds = bookings.Select(b => b.Id).ToList();
                    var cusIds = bookings.Select(b => b.CusID).ToList();
                    var scheIds = bookings.Select(b => b.ScheID).ToList();

                    var customers = conn.Query<Customer>(
                        "SELECT * FROM Customers WHERE Id IN @Ids",
                        new { Ids = cusIds }).ToList();

                    var schedules = conn.Query<Schedule>(
                        "SELECT * FROM Schedules WHERE Id IN @Ids",
                        new { Ids = scheIds }).ToList();

                    var passengers = conn.Query<Passenger>(
                        "SELECT * FROM Passengers WHERE BookingId IN @Ids",
                        new { Ids = bookingIds }).ToList();

                    var payments = conn.Query<Payment>(
                        "SELECT * FROM Payments WHERE BookingId IN @Ids",
                        new { Ids = bookingIds }).ToList();

                    foreach (var b in bookings)
                    {
                        b.Customer = customers.FirstOrDefault(c => c.Id == b.CusID);
                        b.Schedule = schedules.FirstOrDefault(s => s.Id == b.ScheID);
                        b.Passengers = passengers.Where(p => p.BookingId == b.Id).ToList();
                        b.Payments = payments.Where(p => p.BookingId == b.Id).ToList();
                    }

                    return bookings.Cast<T>();
                }

                // ================= SCHEDULE =================
                if (typeof(T) == typeof(Schedule))
                {
                    var schedules = list.Cast<Schedule>().ToList();
                    var tourIds = schedules.Select(s => s.TourId).ToList();
                    var guideIds = schedules.Select(s => s.GuideID).ToList();

                    var tours = conn.Query<Tour>(
                        "SELECT * FROM Tours WHERE Id IN @Ids",
                        new { Ids = tourIds }).ToList();

                    var guides = conn.Query<Guide>(
                        "SELECT * FROM Guides WHERE Id IN @Ids",
                        new { Ids = guideIds }).ToList();

                    foreach (var s in schedules)
                    {
                        s.Tour = tours.FirstOrDefault(t => t.Id == s.TourId);
                        s.Guide = guides.FirstOrDefault(g => g.Id == s.GuideID);
                    }

                    return schedules.Cast<T>();
                }

                // ================= CUSTOMER =================
                if (typeof(T) == typeof(Customer))
                {
                    var customers = list.Cast<Customer>().ToList();
                    var ids = customers.Select(c => c.Id).ToList();

                    var bookings = conn.Query<Booking>(
                        "SELECT * FROM Bookings WHERE CusID IN @Ids",
                        new { Ids = ids }).ToList();

                    foreach (var c in customers)
                        c.Booked = bookings.Where(b => b.CusID == c.Id).ToList();

                    return customers.Cast<T>();
                }

                // ================= GUIDE =================
                if (typeof(T) == typeof(Guide))
                {
                    var guides = list.Cast<Guide>().ToList();
                    var ids = guides.Select(g => g.Id).ToList();

                    var schedules = conn.Query<Schedule>(
                        "SELECT * FROM Schedules WHERE GuideID IN @Ids",
                        new { Ids = ids }).ToList();

                    foreach (var g in guides)
                        g.AssignedSchedules = schedules.Where(s => s.GuideID == g.Id).ToList();

                    return guides.Cast<T>();
                }

                // ================= SERVICE =================
                if (typeof(T) == typeof(Service))
                {
                    var services = list.Cast<Service>().ToList();

                    var vendorIds = services.Select(s => s.VendorId).ToList();
                    var tourIds = services.Select(s => s.TourId).ToList();

                    var vendors = conn.Query<Vendor>(
                        "SELECT * FROM Vendors WHERE Id IN @Ids",
                        new { Ids = vendorIds }).ToList();

                    var tours = conn.Query<Tour>(
                        "SELECT * FROM Tours WHERE Id IN @Ids",
                        new { Ids = tourIds }).ToList();

                    foreach (var s in services)
                    {
                        s.Vendor = vendors.FirstOrDefault(v => v.Id == s.VendorId);
                        s.Tour = tours.FirstOrDefault(t => t.Id == s.TourId);
                    }

                    return services.Cast<T>();
                }

                // ================= PASSENGER / PAYMENT / ITINERARY =================
                return list;
            }
        }
    }
}
