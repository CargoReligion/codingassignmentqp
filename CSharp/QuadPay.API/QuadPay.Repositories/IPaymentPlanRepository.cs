using QuadPay.Domain;
using QuadPay.Repositories.Dto;
using System;
using System.Collections.Generic;

namespace QuadPay.Repositories
{
    // Would get data from database
    public interface IPaymentPlanRepository
    {
        IEnumerable<PaymentPlanDTO> GetPaymentPlansByUserId(Guid userId);
    }
}
