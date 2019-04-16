using System;

namespace QuadPay.Services
{
    public interface IPaymentService
    {
        Guid MakePayment(decimal amount);
    }
}
