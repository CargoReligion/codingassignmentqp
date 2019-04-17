using System;
using System.Collections.Generic;
using System.Text;

namespace QuadPay.Repositories.Dto
{
    // Readonly representation of read db
    public class PaymentPlanDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmountDue { get; set; }
        public DateTime OriginationDate { get; set; }
        public int NumberOfInstallments { get; set; }
        public int InstallmentIntervalDays { get; set; }
        public bool IsPaymentOnTime { get; set; }
    }
}
