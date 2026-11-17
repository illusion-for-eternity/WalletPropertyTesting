using FluentAssertions;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletPropertyTesting.Domain;

namespace WalletPropertyTesting.Tests.Properties
{

    public class RepositoryProperties
    {
        private InMemoryWalletRepository CreateRepository()
        {
            return new InMemoryWalletRepository();
        }

        [Property]
        public void Invalid_id_when_getting_wallet(Guid guid)
        {
            InMemoryWalletRepository inMemoryWalletRepository = CreateRepository();

            Action action = () => inMemoryWalletRepository.Get(guid);

            action.Should().Throw<KeyNotFoundException>();
        }
    }
}
