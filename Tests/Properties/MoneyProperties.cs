using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletPropertyTesting.Domain;

namespace WalletPropertyTesting.Tests.Properties
{

    public class MoneyProperties
    {
        [Property]
        public void negative_money_amount_throws(NegativeInt negativeInt)
        {
            decimal negativeDecimal = (decimal)negativeInt;
            Action act = () => new Money(negativeDecimal);

            act.Should().Throw<ArgumentException>();
        }
    }

}
