using System;
using Microsoft.SPOT;

namespace Test
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return (s == null || s.Length == 0);
        }

        public static bool StartsWith(this string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        public static bool Contains(this string s, string value)
        {
            return s.IndexOf(value) > 0;
        }

        public static float TotalSeconds(this TimeSpan ts)
        {
            return ((float)ts.Ticks) / TimeSpan.TicksPerSecond;
        }

        public static float TotalMilliseconds(this TimeSpan ts)
        {
            return ((float)ts.Ticks) / TimeSpan.TicksPerMillisecond;
        }
    }
}
