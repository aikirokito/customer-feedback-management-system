namespace CFMS.Lab3.NUnitTests.Support;

/// <summary>
/// This reference implementation exists only for SWT301 Lab 3 automation of the
/// algorithm specified by the laboratory exercise. It is not CFMS production
/// functionality.
/// </summary>
public static class DateTimeChecker
{
    public static int DayInMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            return 0;
        }

        if (month == 2)
        {
            var isLeapYear = year % 400 == 0 || year % 100 != 0 && year % 4 == 0;
            return isLeapYear ? 29 : 28;
        }

        return month is 4 or 6 or 9 or 11 ? 30 : 31;
    }

    public static bool CheckDate(int year, int month, int day)
    {
        var daysInMonth = DayInMonth(year, month);
        return daysInMonth != 0 && day >= 1 && day <= daysInMonth;
    }
}
