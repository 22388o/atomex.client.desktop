﻿using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class KusdCurrencyViewModel : CurrencyViewModel
    {
        public KusdCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/kusd_90x90.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/kusd_mask.png"));
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("kusd.png");
            LargeIconPath       = PathToImage("kusd_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}