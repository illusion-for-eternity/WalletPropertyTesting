using FluentAssertions;
using FsCheck.Fluent;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WalletPropertyTesting.Domain;
using WalletPropertyTesting.Tests.Arbitraries;
using Xunit.Sdk;
using System.Reflection;

namespace WalletPropertyTesting.Tests.Properties
{

    public class WalletProperties
    {
        private readonly Mock<IWalletRepository> _walletRepo = new();

        private WalletService CreateService()
        {
            return new WalletService(_walletRepo.Object);
        }

        // P2 · НЕЗМІННЕ (ІНВАРІАНТ)
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void Invariant_deposit_keep_increasing_balance_and_transactions_count(Money money)
        {
            Wallet wallet = new Wallet();
            var startBalance = wallet.Balance.Amount;
            var startHistoryCount = wallet.History.Count;

            wallet.Deposit(money);
            var actual = wallet.Balance.Amount >= startBalance && wallet.History.Count > startHistoryCount;

            actual.Should().BeTrue();
        }

        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public Property Withdraw_more_than_curent_balance_throws(Money money)
        {
            Wallet wallet = new Wallet();
            var startBalance = wallet.Balance;
            var startHistoryCount = wallet.History.Count;
            bool positiveMoney = money.Amount > 0;
            bool ok = true;

            if (positiveMoney)
            {
                Action act = () => wallet.Withdraw(money);
                act.Should().Throw<InvalidOperationException>();
                wallet.Balance.Should().BeEquivalentTo(startBalance);
                wallet.History.Count.Should().Be(startHistoryCount);
            }

            return ok.When(positiveMoney);
        }

        // P1 · ТУДИ Й НАЗАД
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void RoundTrip_deposit_that_withdraws(Money money)
        {
            Wallet wallet = new Wallet();
            var startBalance = wallet.Balance;

            wallet.Deposit(money);
            wallet.Withdraw(money);

            wallet.Balance.Should().BeEquivalentTo(startBalance);
        }

        // P4 · РІЗНІ ШЛЯХИ — ОДИН РЕЗУЛЬТАТ
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void DifferentPath_two_deposits_equal_one(Money a, Money b)
        {
            Wallet walletToCopy = new Wallet();
            Wallet walletOne = walletToCopy.Copy();
            Wallet walletTwo = walletToCopy.Copy();

            walletOne.Deposit(a);
            walletOne.Deposit(b);
            walletTwo.Deposit(a + b);

            walletOne.Balance.Should().BeEquivalentTo(walletTwo.Balance);
        }

        // P5 · ПОРІВНЯННЯ З ЕТАЛОНОМ (ТЕСТОВИЙ ОРАКУЛ)
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void Oracle_walletService_depostit_equals_wallet_deposit(Money money)
        {
            Wallet walletToCopy = new Wallet();
            Wallet wallet = walletToCopy.Copy();
            Wallet walletFromRepo = walletToCopy.Copy();
            WalletService walletService = CreateService();
            _walletRepo.Setup(g => g.Get(It.Is<Guid>(id => id == walletFromRepo.Id))).Returns(walletFromRepo);

            wallet.Deposit(money);
            walletService.Deposit(walletFromRepo.Id, money.Amount);
            Money walletBalanceFromService = walletService.GetBalance(walletFromRepo.Id);

            wallet.Balance.Should().BeEquivalentTo(walletBalanceFromService);
        }

        [Property(Arbitrary = new[] { typeof(TransactionGenerator) })]
        public Property Given_transaction_should_be_saved_with_same_amount_and_type(Transaction transaction)
        {
            bool depositType = transaction.Type == TransactionType.Deposit;
            bool ok = true;
            Wallet wallet = new Wallet();

            if (depositType)
            {
                wallet.Deposit(transaction.Amount);
                ok = wallet.History.Any(t => t.Amount.Amount == transaction.Amount.Amount && t.Type == transaction.Type);
            }

            return ok.When(depositType);
        }

        // P7 · ЛЕГКО ПЕРЕВІРИТИ
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public Property EasyToVerify_balance_recovered_from_result(Money depositMoney, Money withdrawMoney)
        {
            Wallet wallet = new Wallet();
            bool canWithdraw = depositMoney.Amount >= withdrawMoney.Amount;
            bool ok = true;

            if (canWithdraw)
            {
                wallet.Deposit(depositMoney);
                wallet.Withdraw(withdrawMoney);
                decimal sumOfDeposits = wallet.History.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount.Amount);
                decimal sumOfWithdrawals = wallet.History.Where(t => t.Type == TransactionType.Withdraw).Sum(t => t.Amount.Amount);
                ok = wallet.Balance.Amount == (sumOfDeposits - sumOfWithdrawals);
            }

            return ok.When(canWithdraw);
        }

        // P8 · ПРОСТО НЕ ПАДАЄ
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public void NoException_deposit_nonnegative_never_throws(Money money)
        {
            Wallet wallet = new Wallet();

            Action act = () => wallet.Deposit(money);

            act.Should().NotThrow();
        }

        // P6 · ПЕРЕДБАЧУВАНА ЗМІНА (МЕТАМОРФНА ВЛАСТИВІСТЬ)
        [Property(Arbitrary = new[] { typeof(MoneyGenerator) })]
        public Property Metamorphic_double_withdraw_from_wallet_service_has_double_balance_decrease(Money depositMoney, Money withdrawMoney)
        {
            bool canWithdraw = depositMoney.Amount >= withdrawMoney.Amount * 2;

            Wallet walletToCopy = new Wallet();
            walletToCopy.Deposit(depositMoney);
            var singleFromRepo = walletToCopy.Copy();
            var twiseFromRepo = walletToCopy.Copy();

            WalletService walletService = CreateService();
            _walletRepo.Setup(g => g.Get(It.Is<Guid>(id => id == singleFromRepo.Id))).Returns(singleFromRepo);
            _walletRepo.Setup(g => g.Get(It.Is<Guid>(id => id == twiseFromRepo.Id))).Returns(twiseFromRepo);

            var startBalanceAmount = walletToCopy.Balance.Amount;
            bool ok = true;

            if (canWithdraw)
            {
                walletService.Withdraw(singleFromRepo.Id, withdrawMoney.Amount);
                walletService.Withdraw(twiseFromRepo.Id, withdrawMoney.Amount * 2);

                var singleFromGet = walletService.GetBalance(singleFromRepo.Id);
                var twiseFromGet = walletService.GetBalance(twiseFromRepo.Id);

                var withdrawedSingle = startBalanceAmount - singleFromGet.Amount;
                var withdrawedTwice = startBalanceAmount - twiseFromGet.Amount;
                ok = withdrawedTwice == 2 * withdrawedSingle && withdrawedSingle == withdrawMoney.Amount;
            }

            return ok.When(canWithdraw);
        }
    }

    public static class WalletExtensions
    {
        public static Wallet Copy(this Wallet original)
        {
            Wallet copy = new Wallet();
            typeof(Wallet).GetProperty(nameof(Wallet.Balance))!.SetValue(copy, original.Balance);

            foreach (Transaction transaction in original.History)
            {
                Transaction transactionCopy = transaction.Type == TransactionType.Deposit
                        ? Transaction.Deposit(transaction.Amount)
                        : Transaction.Withdraw(transaction.Amount);

                copy.History.Add(transactionCopy);
            }

            return copy;
        }
    }
}
