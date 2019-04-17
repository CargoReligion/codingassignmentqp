using QuadPay.ThirdPartyAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuadPay.Domain
{
    public class PaymentPlan
    {
        private const int _defaultInstallmentCount = 4;
        private const int _defaultInstallmentIntervalDays = 14;

        private IThirdPartyPaymentAPI _thirdPartyPaymentService;

        public Guid Id { get; }
        public Guid UserId { get; }
        public decimal TotalAmountDue { get; private set; }
        public IList<Installment> Installments { get; private set; } = new List<Installment>();
        public IList<Refund> Refunds { get; private set; } = new List<Refund>();
        public DateTime OriginationDate { get; }
        public int NumberOfInstallments { get; }
        public int InstallmentIntervalDays { get; }
        public bool IsPaymentOnTime => AmountPastDue(SystemTime.Now()) == 0;

        public PaymentPlan(
            Guid userId,
            decimal amount,
            IThirdPartyPaymentAPI thirdPartyPaymentService,
            int installmentCount = _defaultInstallmentCount, 
            int installmentIntervalDays = _defaultInstallmentIntervalDays)
        {
            if (amount <= 0.0m)
                throw new ArgumentException($"Amount entered must be greater than zero. {nameof(amount)}: {amount}");

            if (installmentCount <= 0)
                throw new ArgumentException($"There must be atleast one installment. {nameof(installmentCount)}: {installmentCount}");

            if (installmentIntervalDays <= 0)
                throw new ArgumentException($"There must be atleast one installment interval day. {nameof(installmentIntervalDays)}: {installmentIntervalDays}");
            UserId = userId;
            Id = Guid.NewGuid();
            TotalAmountDue = amount;
            OriginationDate = SystemTime.Now();
            NumberOfInstallments = installmentCount;
            InstallmentIntervalDays = installmentIntervalDays;
            _thirdPartyPaymentService = thirdPartyPaymentService;
            InitializeInstallments();
        }

        // Installments are paid in order by Date
        public Installment NextInstallment() {
            var next = Installments.Where(i => i.IsPending)
                           .OrderBy(i => i.DueDate)
                           .FirstOrDefault();

            return next;
        }

        public Installment FirstInstallment() {
            if (!Installments.Any())
                throw new ApplicationException($"No Installments were found for Payment Plan Id {Id.ToString()}");

            return Installments.OrderBy(i => i.DueDate).FirstOrDefault();
        }

        public decimal OustandingBalance() {
            var outstandingBalance = 
                Installments
                .Where(i => i.IsPending || i.IsDefaulted)
                .Sum(i => i.Amount);
            return outstandingBalance;
        }

        public decimal AmountPastDue(DateTime currentDate) {
            return Installments.Where(i => currentDate.CompareTo(i.DueDate) >= 1 && (i.IsPending || i.IsDefaulted))
                                .Select(i => i.Amount)
                                .Sum();
        }

        public IList<Installment> PaidInstallments() 
            => Installments.Where(i => i.IsPaid).ToList();

        public IList<Installment> DefaultedInstallments() 
            => Installments.Where(i => i.IsDefaulted).ToList();

        public IList<Installment> PendingInstallments() 
            => Installments.Where(i => i.IsPending).ToList();

        public decimal MaximumRefundAvailable() 
            => Refunds.Sum(r => r.Amount);

        // We only accept payments matching the Installment Amount.
        public void MakePayment(decimal amount, Guid installmentId) {
            if (!Installments.Any(i => i.Id == installmentId))
            {
                throw new ArgumentException($"No Installment Found for Provided installmentId: {installmentId}", nameof(installmentId));
            }

            var installment = Installments.Where(i => i.Id == installmentId).FirstOrDefault();

            if (installment.Amount != amount)
            {
                throw new ArgumentException($"Payment amount must match installment amount.", nameof(amount));
            }

            //Simulating a third party api call
            var paymentReferenceId = _thirdPartyPaymentService.MakePayment(amount);
            installment.SetStatus(paymentReferenceId);
        }

        // Returns: Amount to refund via PaymentProvider
        public decimal ApplyRefund(Refund refund) {
            var refundedAmountAgainstPaidInstallments = 
                Installments
                .Where(i => i.IsPaid)
                .Sum(i => i.Amount);

            Refunds.Add(refund);

            var refundBalance = refund.Amount;
            foreach (var installment in Installments)
            {
                if (refundBalance >= installment.Amount)
                    MakePayment(installment.Amount, installment.Id);

                refundBalance = refundBalance - installment.Amount;
            }

            return refundedAmountAgainstPaidInstallments;
        }

        // First Installment always occurs on PaymentPlan creation date
        private void InitializeInstallments() {
            var paymentAmountPerInstallment = TotalAmountDue / NumberOfInstallments;

            //initialize with first payment
            var initialPayment = new Installment(paymentAmountPerInstallment, OriginationDate);
            Installments.Add(initialPayment);

            var paymentDate = OriginationDate;

            //rest of the payments
            for (var i = 1; i < NumberOfInstallments; i++)
            {
                paymentDate = paymentDate.AddDays(InstallmentIntervalDays);
                var installmentPayment = new Installment(paymentAmountPerInstallment, paymentDate);
                Installments.Add(installmentPayment);
            }
        }
    }
}