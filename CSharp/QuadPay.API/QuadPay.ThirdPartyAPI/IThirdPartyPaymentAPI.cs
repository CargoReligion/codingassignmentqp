using System;
using System.Collections.Generic;
using System.Text;

namespace QuadPay.ThirdPartyAPI
{
    public interface IThirdPartyPaymentAPI
    {
        Guid MakePayment(decimal amount);
    }
}
