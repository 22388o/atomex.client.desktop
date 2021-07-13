﻿using System;
using Atomex.Blockchain.Abstract;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionDescriptionDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (!(data is TransactionViewModel tx))
                return null;

            switch (tx.Currency)
            {
                case BitcoinBasedCurrency:
                    return App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.BtcBasedDescriptionTemplate);
                case Tezos:
                    return App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.XtzAdditionalDescriptionTemplate);
                case Ethereum:
                    return App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.EthAdditionalDescriptionTemplate);
                default:
                    return App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.BtcBasedDescriptionTemplate);
            }
        }

        public bool Match(object data)
        {
            return data is TransactionViewModel;
        }
    }
}