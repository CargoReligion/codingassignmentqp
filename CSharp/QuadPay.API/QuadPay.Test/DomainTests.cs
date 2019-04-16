using System;
using Xunit;
using QuadPay.Domain;
using FluentAssertions;
using System.Linq;
using QuadPay.Services;
using NSubstitute;

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
                var paymentPlan = new PaymentPlan(amount, PaymentServiceMock, installmentCount, installmentIntervalDays);
            });
            exception.Message.Should().Be(expectedErrorMessage);
        }

        [Theory]
        [InlineData(1000, 4, 2)]
        [InlineData(123.23, 2, 2)]
        public void ShouldCreateCorrectNumberOfInstallments(
            decimal amount,
            int installmentCount,
            int installmentIntervalDays)
        {
            var paymentPlan = new PaymentPlan(amount, PaymentServiceMock, installmentCount, installmentIntervalDays);

            paymentPlan.Installments.Count.Should().Be(installmentCount);
        }

        [Fact]
        public void ShouldCreateInstallmentsWithCorrectDueDates()
        {
            GivenATypicalPaymentPlan();

            var firstPayment = _givenAPaymentPlan.Installments.OrderBy(x => x.DueDate).First();
            firstPayment.DueDate.Should().Be(Today());

            var secondPayment = _givenAPaymentPlan.Installments.OrderByDescending(x => x.DueDate).First();
            secondPayment.DueDate.Should().Be(Today().AddDays(SomeInstallmentIntervalDays));
        }

        [Fact]
        public void ShouldCreateInstallmentsWithCorrectAmountDue()
        {
            GivenATypicalPaymentPlan();

            var firstPayment = _givenAPaymentPlan.Installments.OrderBy(x => x.DueDate).First();
            firstPayment.Amount.Should().Be(SomeAmount/SomeInstallmentCount);

            var secondPayment = _givenAPaymentPlan.Installments.OrderByDescending(x => x.DueDate).First();
            secondPayment.Amount.Should().Be(SomeAmount / SomeInstallmentCount);
        }

        [Fact]
        public void ShouldCreateDefaultNumberOfInstallmentsIfNotSpecified()
        {
            GivenADefaultPaymentPlan();

            _givenAPaymentPlan.Installments.Count.Should().Be(DefaultInstallmentCount);
        }

        [Fact]
        public void AllInstallmentsAreInitializedWithPendingStatus()
        {
            GivenADefaultPaymentPlan();
            foreach (var installment in _givenAPaymentPlan.Installments)
            {
                installment.IsPending.Should().BeTrue();
            }
        }

        [Fact]
        public void ShouldCreateInstallmentsWithDefaultIntervalsIfNotSpecified()
        {
            GivenADefaultPaymentPlan();

            ThenInstallmentsWithDueDatesBasedOffOfDefaultIntervalDaysAreCreated();
        }

        [Fact]
        public void ShouldReturnCorrectAmountToRefundAgainstPaidInstallments() {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(100, PaymentServiceMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            //Unclear on purpose of the return value
            var cashRefundAmount = paymentPlan.ApplyRefund(new Refund(Guid.NewGuid().ToString(), 100));
            Assert.Equal(25, cashRefundAmount);
            Assert.Equal(0, paymentPlan.OustandingBalance());
        }

        [Fact]
        public void ShouldReturnCorrectOutstandingBalance() {
            var paymentPlan = new PaymentPlan(100, PaymentServiceMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            var secondInstallment = paymentPlan.NextInstallment();
            paymentPlan.MakePayment(25, secondInstallment.Id);
            Assert.Equal(50, paymentPlan.OustandingBalance());
        }

        private void SetupPaymentService()
        {
            PaymentServiceMock.MakePayment(SomePayment).Returns(Guid.NewGuid());
        }

        private void GivenATypicalPaymentPlan()
        {
            _givenAPaymentPlan =
                new PaymentPlan(SomeAmount, PaymentServiceMock, SomeInstallmentCount, SomeInstallmentIntervalDays);
        }

        private void GivenADefaultPaymentPlan()
        {
            _givenAPaymentPlan = new PaymentPlan(SomeAmount, PaymentServiceMock);
        }

        private void ThenInstallmentsWithDueDatesBasedOffOfDefaultIntervalDaysAreCreated()
        {
            var today = Today();
            foreach (var installment in _givenAPaymentPlan.Installments.OrderBy(x => x.DueDate))
            {
                installment.DueDate.Should().Be(today);
                today = today.AddDays(DefaultIntervalDays);
            }
        }

        private PaymentPlan _givenAPaymentPlan;
        private const int SomeAmount = 100;
        private const int SomeInstallmentCount = 2;
        private const int SomeInstallmentIntervalDays = 10;
        private const int DefaultInstallmentCount = 4;
        private const int DefaultIntervalDays = 14;
        private const int SomePayment = 25;
        private Func<DateTime> Today = SystemTime.Now = () => new DateTime(2019, 01, 01);

        private IPaymentService PaymentServiceMock { get; set; } = Substitute.For<IPaymentService>();

        /*
            TODO
            Increase domain test coverage
         */
    }
}
