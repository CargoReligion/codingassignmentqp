using System;

namespace QuadPay.Services
{
    public interface IPaymentService
    {
        decimal GetOnTimePaymentRatio(Guid userId);

        decimal GetOutstandingBalances(Guid userId);
    }
}
