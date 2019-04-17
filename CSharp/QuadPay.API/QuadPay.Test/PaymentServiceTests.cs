using FluentAssertions;
using NSubstitute;
using QuadPay.Repositories;
using QuadPay.Repositories.Dto;
using QuadPay.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QuadPay.Test
{
    public class PaymentServiceTests
    {
        [Fact]
        public void OnTimePaymentIs100Percent()
        {
            SetupPaymentService();
            GivenAnOntimePaymentPlan();
            GivenAnotherOntimePaymentPlan();
            SettingUpRepository();

            WhenCalculatingOntimePaymentRatio();

            ThenOntimePaymentRatioIs100Percent();
        }

        [Fact]
        public void OnTimePaymentIs50Percent()
        {
            SetupPaymentService();
            GivenAnOntimePaymentPlan();
            GivenALatePaymentPlan();
            SettingUpRepository();

            WhenCalculatingOntimePaymentRatio();

            ThenOntimePaymentRatioIs50Percent();
        }

        private void GivenAnOntimePaymentPlan()
        {
            _givenAPaymentPlan = new PaymentPlanDTO()
            {
                IsPaymentOnTime = true
            };
            _givenPaymentPlans.Add(_givenAPaymentPlan);
        }

        private void GivenALatePaymentPlan()
        {
            _givenAnotherPaymentPlan = new PaymentPlanDTO()
            {
                IsPaymentOnTime = false
            };
            _givenPaymentPlans.Add(_givenAnotherPaymentPlan);
        }

        private void GivenAnotherOntimePaymentPlan()
        {
            _givenAnotherPaymentPlan = new PaymentPlanDTO()
            {
                IsPaymentOnTime = true
            };
            _givenPaymentPlans.Add(_givenAnotherPaymentPlan);
        }

        private void SetupPaymentService()
        {
            _paymentService = new PaymentService(_paymentPlanRepository);
        }

        private void SettingUpRepository()
        {
            _paymentPlanRepository.GetPaymentPlansByUserId(_userId).Returns(_givenPaymentPlans);
        }

        private void WhenCalculatingOntimePaymentRatio()
        {
            _thenRatio = _paymentService.GetOnTimePaymentRatio(_userId);
        }

        private void ThenOntimePaymentRatioIs100Percent()
        {
            _thenRatio.Should().Be(1);
        }

        private void ThenOntimePaymentRatioIs50Percent()
        {
            _thenRatio.Should().Be(0.5m);
        }

        private PaymentService _paymentService;
        private IPaymentPlanRepository _paymentPlanRepository { get; set; } = Substitute.For<IPaymentPlanRepository>();

        private Guid _userId = Guid.NewGuid();

        private PaymentPlanDTO _givenAPaymentPlan;
        private PaymentPlanDTO _givenAnotherPaymentPlan;
        private List<PaymentPlanDTO> _givenPaymentPlans { get; set; } = new List<PaymentPlanDTO>();
        private decimal _thenRatio;
    }
}
