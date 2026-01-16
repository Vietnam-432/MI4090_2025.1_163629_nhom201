using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUS
{
    public static class Report
    {
        public enum TimeType
        {
            Day,
            Quarter,
            Year
        }

        public enum ReportType
        {
            BusinessOverview,
            BestSellingTours,
            CancellationRate,
            GuideReport
        }

        public static IEnumerable<T> ReportDataSource<T>(TimeType timeType, ReportType reportType, DateTime baseTime) where T : class
        {
            string sql = "";
            object param = null;

            // ===== XỬ LÝ THỜI GIAN =====
            DateTime from, to;

            switch (timeType)
            {
                case TimeType.Day:
                    from = baseTime.Date;
                    to = baseTime.Date.AddDays(1);
                    break;

                case TimeType.Quarter:
                    int quarter = (baseTime.Month - 1) / 3 + 1;
                    from = new DateTime(baseTime.Year, (quarter - 1) * 3 + 1, 1);
                    to = from.AddMonths(3);
                    break;

                case TimeType.Year:
                    from = new DateTime(baseTime.Year, 1, 1);
                    to = from.AddYears(1);
                    break;

                default:
                    throw new ArgumentException("Invalid TimeType");
            }

            // ===== SQL THEO REPORT TYPE =====
            switch (reportType)
            {
                case ReportType.BusinessOverview:
                    sql = @"
                        SELECT 
                            COUNT(*) AS TotalBookings,
                            IFNULL(SUM(p.Amount), 0) AS TotalRevenue,
                            SUM(CASE WHEN b.Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledBookings
                        FROM Bookings b
                        LEFT JOIN Payments p ON b.Id = p.BookingId
                        WHERE date(b.BookDate) BETWEEN @From AND @To
                    ";
                    break;

                case ReportType.BestSellingTours:
                    sql = @"
                        SELECT 
                            t.TourName,
                            SUM(b.AdultCount + b.ChildCount) AS TotalPassengers,
                            SUM(b.TotalAmount) AS Revenue
                        FROM Bookings b
                        JOIN Schedules s ON b.ScheID = s.Id
                        JOIN Tours t ON s.TourId = t.Id
                        WHERE b.Status <> 'Cancelled'
                          AND date(b.BookDate) BETWEEN @From AND @To
                        GROUP BY t.Id
                        ORDER BY TotalPassengers DESC
                    ";
                    break;

                case ReportType.CancellationRate:
                    sql = @"
                        SELECT 
                            COUNT(*) AS TotalBookings,
                            SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledBookings
                        FROM Bookings
                        WHERE date(BookDate) BETWEEN @From AND @To
                    ";
                    break;

                case ReportType.GuideReport:
                    sql = @"
                        SELECT 
                            g.FullName AS GuideName,
                            COUNT(s.Id) AS TotalTours,
                            SUM(s.Booked) AS TotalPassengers
                        FROM Schedules s
                        JOIN Guides g ON s.GuideID = g.Id
                        WHERE s.IsCancel = 0
                          AND date(s.DeDate) BETWEEN @From AND @To
                        GROUP BY g.Id
                    ";
                    break;
            }

            param = new
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd")
            };

            return Repository.Instance.Search<T>(sql, param);
        }
    }
}
