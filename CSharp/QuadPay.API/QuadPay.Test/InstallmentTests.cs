using FluentAssertions;
using QuadPay.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QuadPay.Test
{
    public class InstallmentTests
    {
        [Fact]
        public void ShouldInitializeWithPendingStatus()
        {
            GivenATypicalInstallment();

            _givenAnInstallment.IsPending.Should().BeTrue();
        }

        [Fact]
        public void ShouldSetPaidStatus()
        {
            GivenATypicalInstallment();

            _givenAnInstallment.SetStatus(Guid.NewGuid());

            _givenAnInstallment.IsPaid.Should().BeTrue();
        }

        [Fact]
        public void ShouldSetDefaultedStatus()
        {
            GivenATypicalInstallment();

            _givenAnInstallment.SetStatus(default(Guid));

            _givenAnInstallment.IsDefaulted.Should().BeTrue();
        }

        private void GivenATypicalInstallment()
        {
            _givenAnInstallment =
                new Installment(SomeAmountDue, Today().Date);
        }

        private Installment _givenAnInstallment;
        private const decimal SomeAmountDue = 100;
        private Func<DateTime> Today = 
            SystemTime.Now = () => new DateTime(2019, 01, 01);
    }
}
