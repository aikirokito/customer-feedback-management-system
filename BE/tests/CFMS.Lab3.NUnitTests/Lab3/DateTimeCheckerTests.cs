using CFMS.Lab3.NUnitTests.Support;
using NUnit.Framework;

namespace CFMS.Lab3.NUnitTests.Lab3;

[TestFixture]
public class DateTimeCheckerTests
{
    [TestCase(2026, 1, 31, TestName = "Function1_UTCID01_Normal_January")]
    [TestCase(2026, 4, 30, TestName = "Function1_UTCID02_Normal_April")]
    [TestCase(2023, 2, 28, TestName = "Function1_UTCID03_Normal_NonLeapFebruary")]
    [TestCase(2000, 2, 29, TestName = "Function1_UTCID04_Boundary_YearDivisibleBy400")]
    [TestCase(1900, 2, 28, TestName = "Function1_UTCID05_Boundary_CenturyNotLeap")]
    [TestCase(2024, 2, 29, TestName = "Function1_UTCID06_Boundary_YearDivisibleBy4")]
    [TestCase(2026, 12, 31, TestName = "Function1_UTCID07_Normal_December")]
    [TestCase(2026, 11, 30, TestName = "Function1_UTCID08_Normal_November")]
    [TestCase(2026, 0, 0, TestName = "Function1_UTCID09_Abnormal_MonthBelowRange")]
    [TestCase(2026, 13, 0, TestName = "Function1_UTCID10_Abnormal_MonthAboveRange")]
    public void DayInMonth_ReturnsWorkbookExpected(int year, int month, int expected)
    {
        var actual = DateTimeChecker.DayInMonth(year, month);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(2006, 11, 15, true, TestName = "Function2_UTCID01_Normal_ValidNovemberDate")]
    [TestCase(2024, 6, 20, true, TestName = "Function2_UTCID02_Normal_ValidJuneDate")]
    [TestCase(2024, 0, 15, false, TestName = "Function2_UTCID03_Abnormal_MonthBelowRange")]
    [TestCase(2024, 13, 15, false, TestName = "Function2_UTCID04_Abnormal_MonthAboveRange")]
    [TestCase(2024, 1, 0, false, TestName = "Function2_UTCID05_Abnormal_DayBelowRange")]
    [TestCase(2024, 1, 1, true, TestName = "Function2_UTCID06_Boundary_FirstDay")]
    [TestCase(2024, 1, 31, true, TestName = "Function2_UTCID07_Boundary_LastDayOfJanuary")]
    [TestCase(2024, 4, 31, false, TestName = "Function2_UTCID08_Boundary_DayAboveAprilLimit")]
    [TestCase(2000, 2, 29, true, TestName = "Function2_UTCID09_Boundary_LeapCenturyFebruary29")]
    [TestCase(1900, 2, 29, false, TestName = "Function2_UTCID10_Boundary_NonLeapCenturyFebruary29")]
    [TestCase(2024, 2, 29, true, TestName = "Function2_UTCID11_Boundary_LeapFebruary29")]
    [TestCase(2023, 2, 29, false, TestName = "Function2_UTCID12_Boundary_NonLeapFebruary29")]
    public void CheckDate_ReturnsWorkbookExpected(int year, int month, int day, bool expected)
    {
        var actual = DateTimeChecker.CheckDate(year, month, day);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
