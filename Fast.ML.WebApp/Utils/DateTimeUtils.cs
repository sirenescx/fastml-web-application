using System;

namespace Fast.ML.WebApp.Utils;

public static class DateTimeUtils
{
    private const string DateFormat = "yyyy-MM-dd";
    
    public static string GetDateString(DateTime dateTime) => 
        dateTime.ToString(DateFormat);
}