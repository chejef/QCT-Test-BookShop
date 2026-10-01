using System;

namespace Bookstore.Domain
{
    public static class DateTimeExtensions
    {
        public static DateTime OneSecondToMidnight(this DateTime dateTime)
        {
            return dateTime.Date.AddSeconds(86399);
        }

        public static DateTime StartOfMonth(this DateTime dateTime)
        {
            return dateTime.AddDays(1 - dateTime.Day);
        }

        public static DateTimeOffset OneSecondToMidnight(this DateTimeOffset dateTimeOffset)
        {
            return new DateTimeOffset(dateTimeOffset.Date, dateTimeOffset.Offset).AddSeconds(86399);
        }

        public static DateTimeOffset StartOfMonth(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.AddDays(1 - dateTimeOffset.Day);
        }
    }
}
