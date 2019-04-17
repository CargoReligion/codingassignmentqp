using System;
using System.Collections.Generic;
using System.Text;

namespace QuadPay.Domain
{
    public static class SystemTime
    {
        public static Func<DateTime> Now = () => DateTime.Now;
    }
}
