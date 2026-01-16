using System;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;

namespace DTO
{
    public interface IEntity
    {
        [ExplicitKey]
        string Id { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        public string ForeignTableName { get; set; }
        public string ForeignFieldName { get; set; }
        public ForeignKeyAttribute(string table, string field)
        {
            ForeignTableName = table;
            ForeignFieldName = field;
        }
    }

    [Table("Tours")]
    public class Tour : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string TourName { get; set; }
        public double BasePrice { get; set; }
        public int Dura { get; set; }
        public string TourType { get; set; }
        public string Description { get; set; }

        // --- NAVIGATION PROPERTIES (ONE-TO-MANY) ---
       
        [Write(false)]
        public List<Schedule> Schedules { get; set; }
        [Write(false)]
        public List<Itinerary> Itineraries { get; set; }
        [Write(false)]
        public List<Service> Services { get; set; }

        public Tour() { }
        public Tour(string id, string tourName, double basePrice, int duration, string tourType, string description, string ServiceID)
        {
            Id = id;
            TourName = tourName;
            BasePrice = basePrice;
            Dura = duration;
            TourType = tourType;
            Description = description;
            Schedules = new List<Schedule>();
            Itineraries = new List<Itinerary>();
            Services = new List<Service>();

        }
    }

    [Table("Schedules")]
    public class Schedule : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public DateTime DeDate { get; set; }
        public DateTime ReDate { get; set; }
        public int MaxCap { get; set; }
        [DisplayOnly]
        public int Booked { get; set; }

        [ForeignKey("Tours", "Id")]
        public string TourId { get; set; }

        [ForeignKey("Guides", "Id")]
        public string GuideID { get; set; }
        public bool IsCancel { get; set; } = false;

        // --- NAVIGATION PROPERTIES ---
        [Write(false)]
        public Tour Tour { get; set; } 
        [Write(false)]
        public Guide Guide { get; set; } 

        public Schedule() { }
        public Schedule(string id, DateTime de, DateTime re, int max, int booked, string tourId, string guideId)
        {
            Id = id;
            DeDate = de;
            ReDate = re;
            MaxCap = max;
            Booked = booked;
            TourId = tourId;
            GuideID = guideId;
        }
    }

    [Table("Itinerarys")]
    public class Itinerary : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public int DayNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        [ForeignKey("Tours", "Id")]
        public string TourId { get; set; }

        // --- NAVIGATION PROPERTIES ---
        [Write(false)]
        public Tour Tour { get; set; }

        public Itinerary() { }
        public Itinerary(string id, int dayNumber, string title, string description, string tourId)
        {
            Id = id;
            DayNumber = dayNumber;
            Title = title;
            Description = description;
            TourId = tourId;
        }
    }

    [Table("Guides")]
    public class Guide : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNum { get; set; }
        public string Language { get; set; }
        public int ExYears { get; set; }
        [Write(false)]
        public List<Schedule> AssignedSchedules { get; set; }
        public Guide() { }
        public Guide(string id, string fullName, string phoneNum, string language, int exYears)
        {
            Id = id;
            FullName = fullName;
            PhoneNum = phoneNum;
            Language = language;
            ExYears = exYears;
            AssignedSchedules = new List<Schedule>();
        }
    }

    [Table("Bookings")]
    public class Booking : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public DateTime BookDate { get; set; }
        public double TotalAmount { get; set; }
        public string Status { get; set; }
        [DisplayOnly]
        public int AdultCount { get; set; }
        [DisplayOnly]
        public int ChildCount { get; set; }

        [ForeignKey("Customers", "Id")]
        public string CusID { get; set; }

        [ForeignKey("Schedules", "Id")]
        public string ScheID { get; set; }

        // --- NAVIGATION PROPERTIES (QUAN TRỌNG) ---
        [Write(false)]
        public Customer Customer { get; set; } 
        [Write(false)]
        public Schedule Schedule { get; set; } 
        [Write(false)]
        public Tour Tour { get; set; }
        [Write(false)]
        public List<Passenger> Passengers { get; set; } 
        [Write(false)]
        public List<Payment> Payments { get; set; } 

        public Booking() { }
        public Booking(string id, DateTime bookDate, double totalAmount, string status, int adultCount, int childCount, string cusId, string scheId)
        {
            Id = id;
            BookDate = bookDate;
            TotalAmount = totalAmount;
            Status = status;
            AdultCount = 0;
            ChildCount = 0;
            CusID = cusId;
            ScheID = scheId;
            Passengers = new List<Passenger>();
            Payments = new List<Payment>();

        }
    }

    [Table("Customers")]
    public class Customer : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string CitizenId { get; set; }
        [Write(false)]
        public List<Booking> Booked { get; set; }
        public Customer() { }

        public Customer(string id, string fullName, string phoneNumber, string email, string address, string citizenId)
        {
            Id = id;
            FullName = fullName;
            PhoneNumber = phoneNumber;
            Email = email;
            Address = address;
            CitizenId = citizenId;
            Booked = new List<Booking>();
        }
    }

    [Table("Passengers")]
    public class Passenger : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PassengerType { get; set; }

        [ForeignKey("Bookings", "Id")]
        public string BookingId { get; set; }

        // --- NAVIGATION PROPERTIES ---
        [Write(false)]
        public Booking Booking { get; set; }

        public Passenger() { }
        public Passenger(string id, string fullName, DateTime dateOfBirth, string gender, string passengerType, string bookingId)
        {
            Id = id;
            FullName = fullName;
            DateOfBirth = dateOfBirth;
            Gender = gender;
            PassengerType = passengerType;
            BookingId = bookingId;
        }
    }

    [Table("Payments")]
    public class Payment : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Method { get; set; }

        [ForeignKey("Bookings", "Id")]
        public string BookingId { get; set; }

        // --- NAVIGATION PROPERTIES ---
        [Write(false)]
        public Booking Booking { get; set; }

        public Payment()
        {
            PaymentDate = DateTime.Now;
        }
        public Payment(string id, double amount, DateTime paymentDate, string method, string bookingId)
        {
            Id = id;
            Amount = amount;
            PaymentDate = paymentDate;
            Method = method;
            BookingId = bookingId;
        }
    }

    [Table("Vendors")]
    public class Vendor : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string VendorName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string VendorType { get; set; }
        [Write(false)]
        public List<Service> ProvidedService { get; set; }
        public Vendor() { }
        public Vendor(string id, string vendorName, string phoneNumber, string address, string vendorType)
        {
            Id = id;
            VendorName = vendorName;
            PhoneNumber = phoneNumber;
            Address = address;
            VendorType = vendorType;
            ProvidedService = new List<Service>();
        }
    }

    [Table("Services")]
    public class Service : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public double EstimatedCost { get; set; }
        public string Unit { get; set; }

        [ForeignKey("Tours", "Id")]
        public string TourId { get; set; }

        [ForeignKey("Vendors", "Id")]
        public string VendorId { get; set; }

        // --- NAVIGATION PROPERTIES ---
        [Write(false)]
        public Tour Tour { get; set; }
        [Write(false)]
        public Vendor Vendor { get; set; } // Để hiển thị tên nhà cung cấp

        public Service() { }
        public Service(string id, string serviceName, double estimatedCost, string unit, string tourId, string vendorId)
        {
            Id = id;
            ServiceName = serviceName;
            EstimatedCost = estimatedCost;
            Unit = unit;
            TourId = tourId;
            VendorId = vendorId;
        }
    }

    [Table("Users")]
    public class User : IEntity
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }

        public static class RoleIdType
        {
            public static readonly int Admin = 0;
            public static readonly int Accountant= 1;
            public static readonly int Guide = 2;
            public static readonly int Emp = 3;
        }
    }

    public sealed class UserSession
    {
        private static readonly UserSession _instance = new UserSession();
        private UserSession() { }
        public static UserSession Instance => _instance;

        public User CurrentUser { get; private set; }

        public void Login(User user)
        {
            CurrentUser = user;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public bool IsLoggedIn => CurrentUser != null;

        public bool IsAdmin
        {
            get
            {
                if (CurrentUser == null) return false;
                return CurrentUser.RoleId == User.RoleIdType.Admin;
            }
        }

        public bool IsManager
        {
            get
            {
                if (CurrentUser == null) return false;
                return CurrentUser.RoleId == User.RoleIdType.Admin;
            }
        }

        public class BusinessOverviewDTO
        {
            public int TotalBookings { get; set; }
            public double TotalRevenue { get; set; }
            public int CancelledBookings { get; set; }
        }
        public class BestSellingTourDTO
        {
            public string TourName { get; set; }
            public int TotalPassengers { get; set; }
            public double Revenue { get; set; }
        }
        public class GuideReportDTO
        {
            public string GuideName { get; set; }
            public int TotalTours { get; set; }
            public int TotalPassengers { get; set; }
        }
        public class CancellationRateDTO
        {
            public int TotalBookings { get; set; }
            public int CancelledBookings { get; set; }
            public double CancelRate { get; set; }
        }
    }
}