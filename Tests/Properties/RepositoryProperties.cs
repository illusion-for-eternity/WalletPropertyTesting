using FluentAssertions;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletPropertyTesting.Domain;
using WalletPropertyTesting.Tests.Arbitraries;

namespace WalletPropertyTesting.Tests.Properties
{

    public class RepositoryProperties
    {
        private InMemoryWalletRepository CreateRepository()
        {
            return new InMemoryWalletRepository();
        }

        [Property]
        public void Invalid_id_when_getting_wallet_throws(Guid guid)
        {
            InMemoryWalletRepository inMemoryWalletRepository = CreateRepository();

            Action action = () => inMemoryWalletRepository.Get(guid);

            action.Should().Throw<KeyNotFoundException>();
        }

        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void Repository_returns_all_wallets_with_correct_properties(Money money)
        {
            Wallet wallet = new Wallet();
            wallet.Deposit(money);
            InMemoryWalletRepository inMemoryWalletRepository = CreateRepository();
            inMemoryWalletRepository.Save(wallet);

            var walletListFromAll = inMemoryWalletRepository.All();

            walletListFromAll.Should().ContainSingle().Which.Should().BeEquivalentTo(wallet);
        }

    }
}
