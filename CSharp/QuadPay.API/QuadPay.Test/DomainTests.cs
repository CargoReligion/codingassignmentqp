using System;
using Xunit;
using QuadPay.Domain;
using FluentAssertions;

namespace QuadPay.Test
{
    public class DomainTests
    {

        [Theory]
        [InlineData(-100, 4, 2, "Amount entered must be greater than zero. amount: -100")]
        [InlineData(0, 4, 14, "Amount entered must be greater than zero. amount: 0")]
        [InlineData(123.23, 0, 2, "There must be atleast one installment. installmentCount: 0")]
        [InlineData(123.23, -1, 2, "There must be atleast one installment. installmentCount: -1")]
        [InlineData(200, 4, -2, "There must be atleast one installment interval day. installmentIntervalDays: -2")]
        [InlineData(200, 4, 0, "There must be atleast one installment interval day. installmentIntervalDays: 0")]
        public void ShouldThrowExceptionForInvalidParameters(
            decimal amount, 
            int installmentCount, 
            int installmentIntervalDays,
            string expectedErrorMessage)
        {
            var exception = Assert.Throws<ArgumentException>(() => {
                var paymentPlan = new PaymentPlan(amount, installmentCount, installmentIntervalDays);
            });
            exception.Message.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData(1000, 4, 2)]
        [InlineData(123.23, 2, 2)]
        // TODO What other situations should we be testing?
        public void ShouldCreateCorrectNumberOfInstallments(decimal amount, int installmentCount, int installmentIntervalDays)
        {
            var paymentPlan = new PaymentPlan(amount, installmentCount, installmentIntervalDays);
            Assert.Equal(installmentCount, paymentPlan.Installments.Count);
        }

        [Fact]
        public void ShouldReturnCorrectAmountToRefundAgainstPaidInstallments() {
            var paymentPlan = new PaymentPlan(100, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            var cashRefundAmount = paymentPlan.ApplyRefund(new Refund(Guid.NewGuid().ToString(), 100));
            Assert.Equal(25, cashRefundAmount);
            Assert.Equal(0, paymentPlan.OustandingBalance());
        }

        [Fact]
        public void ShouldReturnCorrectOutstandingBalance() {
            var paymentPlan = new PaymentPlan(100, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            var secondInstallment = paymentPlan.NextInstallment();
            paymentPlan.MakePayment(25, secondInstallment.Id);
            Assert.Equal(50, paymentPlan.OustandingBalance());
        }

        /*
            TODO
            Increase domain test coverage
         */
    }
}
