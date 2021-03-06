﻿using NRules.Fluent.Dsl;
using NRules.IntegrationTests.TestAssets;
using Xunit;

namespace NRules.IntegrationTests
{
    public class TwoFactCalculateRuleTest : BaseRuleTestFixture
    {
        [Fact]
        public void Fire_OneMatchingFactOfEachKind_FiresOnce()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
            var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = "Valid Value 1" };
            Session.Insert(fact1);
            Session.Insert(fact2);

            //Act
            Session.Fire();

            //Assert
            AssertFiredOnce();
            Assert.Equal("Valid Value 1|Valid Value 2", GetFiredFact<CalculatedFact3>().Value);
        }

        [Fact]
        public void Fire_OneMatchingFactOfEachKindSecondFactUpdated_FiresOnce()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
            var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = "Valid Value 1" };
            Session.Insert(fact1);
            Session.Insert(fact2);

            fact2.TestProperty = "Valid Value 22";
            Session.Update(fact2);

            //Act
            Session.Fire();

            //Assert
            AssertFiredOnce();
            Assert.Equal("Valid Value 1|Valid Value 22", GetFiredFact<CalculatedFact3>().Value);
        }

        [Fact]
        public void Fire_OneMatchingFactOfEachKindFireThenSecondFactUpdated_FiresTwice()
        {
            //Arrange - 1
            var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
            var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = "Valid Value 1" };
            Session.Insert(fact1);
            Session.Insert(fact2);

            //Act - 1
            Session.Fire();

            //Assert - 1
            AssertFiredOnce();
            Assert.Equal("Valid Value 1|Valid Value 2", GetFiredFact<CalculatedFact3>(0).Value);

            //Arrange - 2
            fact2.TestProperty = "Valid Value 22";
            Session.Update(fact2);

            //Act - 2
            Session.Fire();

            //Assert - 2
            AssertFiredTwice();
            Assert.Equal("Valid Value 1|Valid Value 22", GetFiredFact<CalculatedFact3>(1).Value);
        }

        [Fact]
        public void Fire_OneMatchingFactOfEachKindSecondFactUpdatedToInvalidateBindingCondition_DoesNotFire()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
            var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = "Valid Value 1" };
            Session.Insert(fact1);
            Session.Insert(fact2);

            fact2.Counter = 1;
            Session.Update(fact2);

            //Act
            Session.Fire();

            //Assert
            AssertDidNotFire();
        }

        [Fact]
        public void Fire_OneMatchingFactOfEachKindSecondFactRetracted_DoesNotFire()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
            var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = "Valid Value 1" };
            Session.Insert(fact1);
            Session.Insert(fact2);

            Session.Retract(fact2);

            //Act
            Session.Fire();

            //Assert
            AssertDidNotFire();
        }

        [Fact]
        public void Fire_TwoMatchingSetsOfFacts_FiresTwiceCalculatesPerSet()
        {
            //Arrange
            var fact11 = new FactType1 { TestProperty = "Valid Value 11" };
            var fact21 = new FactType2 { TestProperty = "Valid Value 21", JoinProperty = "Valid Value 11" };
            var fact12 = new FactType1 { TestProperty = "Valid Value 12" };
            var fact22 = new FactType2 { TestProperty = "Valid Value 22", JoinProperty = "Valid Value 12" };
            Session.InsertAll(new []{fact11, fact12});
            Session.InsertAll(new []{fact21, fact22});

            //Act
            Session.Fire();

            //Assert
            AssertFiredTwice();
            Assert.Equal("Valid Value 11|Valid Value 21", GetFiredFact<CalculatedFact3>(0).Value);
            Assert.Equal("Valid Value 12|Valid Value 22", GetFiredFact<CalculatedFact3>(1).Value);
        }

        protected override void SetUpRules()
        {
            SetUpRule<TestRule>();
        }

        public class FactType1
        {
            public string TestProperty { get; set; }
            public int Counter { get; set; }
        }

        public class FactType2
        {
            public string TestProperty { get; set; }
            public int Counter { get; set; }
            public string JoinProperty { get; set; }
        }

        public class CalculatedFact3
        {
            public CalculatedFact3(FactType1 fact1, FactType2 fact2)
            {
                Value = $"{fact1.TestProperty}|{fact2.TestProperty}";
            }

            public string Value { get; }
        }

        public class CalculatedFact4
        {
            public CalculatedFact4(FactType1 fact1, FactType2 fact2)
            {
                Value = fact1.Counter + fact2.Counter;
            }

            public long Value { get; }
        }

        public class TestRule : Rule
        {
            public override void Define()
            {
                FactType1 fact1 = null;
                FactType2 fact2 = null;
                CalculatedFact3 fact3 = null;
                CalculatedFact4 fact4 = null;

                When()
                    .Match<FactType1>(() => fact1, f => f.TestProperty.StartsWith("Valid"))
                    .Match<FactType2>(() => fact2, f => f.TestProperty.StartsWith("Valid"), f => f.JoinProperty == fact1.TestProperty)
                    .Calculate(() => fact3, () => new CalculatedFact3(fact1, fact2))
                    .Calculate(() => fact4, () => new CalculatedFact4(fact1, fact2))
                    .Having(() => fact4.Value == 0);

                Then()
                    .Do(ctx => ctx.NoOp());
            }
        }
    }
}
