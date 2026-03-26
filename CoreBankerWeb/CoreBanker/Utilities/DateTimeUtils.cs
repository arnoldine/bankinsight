using System;

namespace CoreBanker.Utilities
{
    public static class DateTimeUtils
    {
        public static string NowFormatted() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
