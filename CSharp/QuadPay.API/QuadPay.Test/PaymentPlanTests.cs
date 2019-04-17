using System;
using Xunit;
using QuadPay.Domain;
using FluentAssertions;
using System.Linq;
using NSubstitute;
using QuadPay.ThirdPartyAPI;

namespace QuadPay.Test
{
    public class PaymentPlanTests
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
                var paymentPlan = new PaymentPlan(_userId, amount, ThirPartyPaymentAPIMock, installmentCount, installmentIntervalDays);
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
            var paymentPlan = new PaymentPlan(_userId, amount, ThirPartyPaymentAPIMock, installmentCount, installmentIntervalDays);

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
        public void ShouldReturnCorrectAmountToRefundAgainstPaidInstallments()
        {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            //Unclear on purpose of the return value
            var cashRefundAmount = paymentPlan.ApplyRefund(new Refund(Guid.NewGuid().ToString(), 100));
            cashRefundAmount.Should().Be(25);
            paymentPlan.OustandingBalance().Should().Be(0);
        }

        [Fact]
        public void ShouldReturnMaximumRefundsAvailable()
        {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            paymentPlan.ApplyRefund(new Refund(Guid.NewGuid().ToString(), 100));
            paymentPlan.MaximumRefundAvailable().Should().Be(100);
        }

        [Fact]
        public void ShouldReturnCorrectOutstandingBalance() {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);
            var secondInstallment = paymentPlan.NextInstallment();
            paymentPlan.MakePayment(25, secondInstallment.Id);
            Assert.Equal(50, paymentPlan.OustandingBalance());
        }

        [Fact]
        public void AmountPastDueIsReturnedForPendingStatuses()
        {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);

            var amountPastDue = paymentPlan.AmountPastDue(Today().AddMonths(3));
            Assert.Equal(75, amountPastDue);
        }

        [Fact]
        public void AmountPastDueIsReturnedForDefaultedStatuses()
        {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            paymentPlan.MakePayment(25, firstInstallment.Id);

            SetupPaymentServiceToDefault();
            var secondInstallment = paymentPlan.NextInstallment();
            paymentPlan.MakePayment(25, secondInstallment.Id);
            var amountPastDue = paymentPlan.AmountPastDue(Today().AddMonths(3));
            Assert.Equal(75, amountPastDue);
        }

        [Fact]
        public void ShouldThrowExceptionWhenInstallmentNotFoundWhileMakingPayment()
        {
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var randomInstallmentId = Guid.NewGuid();
            var exception = Assert.Throws<ArgumentException>(() => paymentPlan.MakePayment(25, randomInstallmentId));
            exception.Message.Should().Be($"No Installment Found for Provided installmentId: {randomInstallmentId}{Environment.NewLine}Parameter name: installmentId");
        }

        [Theory]
        [InlineData(20)]
        [InlineData(26)]
        public void ShouldThrowExceptionWhenAmountDoesNotMatch(decimal paymentAmount)
        {
            SetupPaymentService();
            var paymentPlan = new PaymentPlan(_userId, 100, ThirPartyPaymentAPIMock, 4);
            var firstInstallment = paymentPlan.FirstInstallment();
            var exception = Assert.Throws<ArgumentException>(() => paymentPlan.MakePayment(paymentAmount, firstInstallment.Id));
            exception.Message.Should().Be($"Payment amount must match installment amount.{Environment.NewLine}Parameter name: amount");
        }

        private void SetupPaymentService()
        {
            ThirPartyPaymentAPIMock.MakePayment(SomePayment).Returns(Guid.NewGuid());
        }

        private void SetupPaymentServiceToDefault()
        {
            ThirPartyPaymentAPIMock.MakePayment(SomePayment).Returns(default(Guid));
        }

        private void GivenATypicalPaymentPlan()
        {
            _givenAPaymentPlan =
                new PaymentPlan(_userId, SomeAmount, ThirPartyPaymentAPIMock, SomeInstallmentCount, SomeInstallmentIntervalDays);
        }

        private void GivenADefaultPaymentPlan()
        {
            _givenAPaymentPlan = new PaymentPlan(_userId, SomeAmount, ThirPartyPaymentAPIMock);
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
        private Guid _userId = Guid.NewGuid();
        private const int SomeAmount = 100;
        private const int SomeInstallmentCount = 2;
        private const int SomeInstallmentIntervalDays = 10;
        private const int DefaultInstallmentCount = 4;
        private const int DefaultIntervalDays = 14;
        private const int SomePayment = 25;
        private Func<DateTime> Today = SystemTime.Now = () => new DateTime(2019, 01, 01);

        private IThirdPartyPaymentAPI ThirPartyPaymentAPIMock { get; set; } = Substitute.For<IThirdPartyPaymentAPI>();
    }
}
