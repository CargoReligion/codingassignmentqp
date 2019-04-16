using System;

namespace QuadPay.Domain
{
    public class Installment
    {
        public Guid Id { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        private InstallmentStatus InstallmentStatus;
        public string PaymentReference { get; }
        // Date this Installment was marked 'Paid'
        public DateTime SettlementDate { get; }

        public Installment() { }

        public Installment(decimal amountDue, DateTime dueDate)
        {
            Id = Guid.NewGuid();
            Amount = amountDue;
            DueDate = dueDate;
            InstallmentStatus = InstallmentStatus.Pending;
        }

        public bool IsPaid { 
            get
            {
                return InstallmentStatus == InstallmentStatus.Paid;
            }
        }

        public bool IsDefaulted { 
            get
            {
                return InstallmentStatus == InstallmentStatus.Defaulted;
            }
        }

        public bool IsPending { 
            get
            {
                return InstallmentStatus == InstallmentStatus.Pending;
            }
        }

        public void SetPaid(string paymentReference) {
            // TODO
        }
    }

    public enum InstallmentStatus {
        Pending = 0, // Not yet paid
        Paid = 1, // Can be either paid with a charge, or covered by a refund
        Defaulted = 2 // Charge failed
    }
}