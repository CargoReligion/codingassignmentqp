using QuadPay.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuadPay.Services
{
    public class PaymentService : IPaymentService
    {
        private IPaymentPlanRepository _paymentPlanRepository;

        public PaymentService(IPaymentPlanRepository paymentPlanRepository)
        {
            _paymentPlanRepository = paymentPlanRepository;
        }

        public decimal GetOnTimePaymentRatio(Guid userId)
        {
            var paymentPlans = _paymentPlanRepository.GetPaymentPlansByUserId(userId).ToList();
            decimal ontimePayments = paymentPlans.Where(x => x.IsPaymentOnTime).Count();
            decimal latePayments = paymentPlans.Where(x => !x.IsPaymentOnTime).Count();

            return ontimePayments / (ontimePayments + latePayments);
        }

        public decimal GetOutstandingBalances(Guid userId)
        {
            var paymentPlans = _paymentPlanRepository.GetPaymentPlansByUserId(userId).ToList();
            return paymentPlans.Sum(x => x.OustandingBalance);
        }
    }
}
