using System;
using System.Collections.Generic;
using System.Linq;

namespace QuadPay.Domain
{
    public class PaymentPlan
    {
        private const int _defaultInstallmentCount = 4;
        private const int _defaultInstallmentIntervalDays = 14;

        public Guid Id { get; }
        public decimal TotalAmountDue { get; private set; }
        public IList<Installment> Installments { get; private set; }
        public IList<Refund> Refunds { get; private set; }
        public DateTime OriginationDate { get; }
        public int NumberOfInstallments { get; }
        public int InstallmentIntervalDays { get; }
        public PaymentPlan(
            decimal amount, 
            int installmentCount = _defaultInstallmentCount, 
            int installmentIntervalDays = _defaultInstallmentIntervalDays)
        {
            if (amount <= 0.0m)
                throw new ArgumentException($"Amount entered must be greater than zero. {nameof(amount)}: {amount}");

            if (installmentCount <= 0)
                throw new ArgumentException($"There must be atleast one installment. {nameof(installmentCount)}: {installmentCount}");

            if (installmentIntervalDays <= 0)
                throw new ArgumentException($"There must be atleast one installment interval day. {nameof(installmentIntervalDays)}: {installmentIntervalDays}");

            Id = Guid.NewGuid();
            TotalAmountDue = amount;
            OriginationDate = SystemTime.Now();
            NumberOfInstallments = installmentCount;
            InstallmentIntervalDays = installmentIntervalDays;
            InitializeInstallments();
        }

        // Installments are paid in order by Date
        public Installment NextInstallment() {
            // TODO
            return new Installment();
        }

        public Installment FirstInstallment() {
            // TODO
            return new Installment();
        }

        public decimal OustandingBalance() {
            // TODO
            return 0;
        }

        public decimal AmountPastDue(DateTime currentDate) {
            // TODO
            return 0;
        }

        public IList<Installment> PaidInstallments() {
            return Installments.Where(i => i.IsPaid).ToList<Installment>();
        }

        public IList<Installment> DefaultedInstallments() {
            return Installments.Where(i => i.IsDefaulted).ToList<Installment>();
        }

        public IList<Installment> PendingInstallments() {
            return Installments.Where(i => i.IsPending).ToList<Installment>();
        }

        public decimal MaximumRefundAvailable() {
            // TODO
            return 0;
        }

        // We only accept payments matching the Installment Amount.
        public void MakePayment(decimal amount, Guid installmentId) {

        }

        // Returns: Amount to refund via PaymentProvider
        public decimal ApplyRefund(Refund refund) {
            // TODO
            return 0;
        }

        // First Installment always occurs on PaymentPlan creation date
        private void InitializeInstallments() {
            Installments = new List<Installment>();
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