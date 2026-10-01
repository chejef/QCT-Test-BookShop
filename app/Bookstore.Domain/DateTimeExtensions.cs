namespace Bookstore.Domain
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset OneSecondToMidnight(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Date, dateTime.Offset).AddSeconds(86399);
        }

        public static DateTimeOffset StartOfMonth(this DateTimeOffset dateTime)
        {
            return dateTime.AddDays(1 - dateTime.Day);
        }
    }
}
