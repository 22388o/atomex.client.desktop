﻿using System;
using System.Windows.Input;

using ReactiveUI;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TransactionViewModel : ViewModelBase, ITransactionViewModel
    {
        public event EventHandler<TransactionEventArgs> UpdateClicked;
        public event EventHandler<TransactionEventArgs> RemoveClicked;

        public IBlockchainTransaction Transaction { get; }
        public string Id { get; set; }
        public CurrencyConfig Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public bool CanBeRemoved { get; set; }
        public string Direction { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public TransactionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TransactionViewModel(
            IBlockchainTransaction tx,
            CurrencyConfig currencyConfig,
            decimal amount,
            decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id          = Transaction.Id;
            Currency    = currencyConfig;
            State       = Transaction.State;
            Type        = Transaction.Type;
            Amount      = amount;

            var netAmount = amount + fee;

            var currencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencyConfig, false);

            AmountFormat = currencyViewModel.CurrencyFormat;
            CurrencyCode = currencyViewModel.CurrencyCode;
            Time         = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Unknown ||
                           tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending ||
                           tx.State == BlockchainTransactionState.Unconfirmed;

            Description = GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: netAmount,
                amountDigits: currencyConfig.Digits,
                currencyCode: currencyConfig.Name);
            
            if (Amount <= 0)
            {
                Direction = "To: ";
            }

            if (Amount > 0)
            {
                Direction = "From: ";
            }
        }

        private ICommand _openTxInExplorerCommand;
        public ICommand OpenTxInExplorerCommand => _openTxInExplorerCommand ??= ReactiveCommand.Create<string>((id) =>
        {
            if (Uri.TryCreate($"{Currency.TxExplorerUri}{id}", UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for transaction explorer");
        });

        private ICommand _openAddressInExplorerCommand;
        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??= ReactiveCommand.Create<string>((address) =>
        {
            if (Uri.TryCreate($"{Currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for address explorer");
        });

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                App.Clipboard.SetTextAsync(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= ReactiveCommand.Create(() =>
        {
            UpdateClicked?.Invoke(this, new TransactionEventArgs(Transaction));
        });

        private ICommand _removeCommand;
        public ICommand RemoveCommand => _removeCommand ??= ReactiveCommand.Create(() =>
        {
            RemoveClicked?.Invoke(this, new TransactionEventArgs(Transaction));
        });

        private void DesignerMode()
        {
            Id = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }

        public static string GetDescription(
            BlockchainTransactionType type,
            decimal amount,
            decimal netAmount,
            int amountDigits,
            string currencyCode)
        {
            if (type.HasFlag(BlockchainTransactionType.SwapPayment))
            {
                return $"Swap payment {Math.Abs(amount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapRefund))
            {
                return $"Swap refund {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapRedeem))
            {
                return $"Swap redeem {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.TokenApprove))
            {
                return $"Token approve";
            }
            else if (type.HasFlag(BlockchainTransactionType.TokenCall))
            {
                return $"Token call";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapCall))
            {
                return $"Token swap call";
            }
            else if (amount <= 0)
            {
                return $"Sent {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (amount > 0)
            {
                return $"Received {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }

            return "Unknown transaction";
        }
    }
}