using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using ReactiveUI;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Services.Abstract;
using Avalonia.Threading;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletMainViewModel : ViewModelBase
    {
        public const int DelayBeforeSwitchingSwapDetailsMs = 250;

        public WalletMainViewModel()
        {
        }

        public WalletMainViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));

            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is ViewModels.PortfolioViewModel or ViewModels.WalletsViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsPortfolioSectionActive);

            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is ConversionViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsConversionSectionActive);

            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is SettingsViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsSettingsSectionActive);

            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is WertViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsWertSectionActive);

            this.WhenAnyValue(vm => vm.RightPopupContent)
                .WhereNotNull()
                .SubscribeInMainThread(_ => RightPopupOpened = true);

            this.WhenAnyValue(vm => vm.Content)
                .WhereNotNull()
                .Where(_ => RightPopupOpened)
                .SubscribeInMainThread(_ => ShowRightPopupContent(null));

            this.WhenAnyValue(vm => vm.RightPopupContent)
                .Select(content => content != null)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .ToPropertyExInMainThread(this, vm => vm.RightPopupHasContent);

            PortfolioViewModel = new PortfolioViewModel(AtomexApp)
            {
                SetDexTab = SelectConversion,
                SetWalletCurrency = SelectCurrencyWallet,
                SetWertCurrency = SelectWert
            };
            WalletsViewModel = new WalletsViewModel(AtomexApp)
            {
                SetConversionTab = SelectConversion,
                SetWertCurrency = SelectWert,
                BackAction = SelectPortfolio,
                ShowRightPopupContent = ShowRightPopupContent
            };
            ConversionViewModel = new ConversionViewModel(AtomexApp)
            {
                ShowRightPopupContent = ShowRightPopupContent
            };

            SettingsViewModel = new SettingsViewModel(AtomexApp);
            WertViewModel = new WertViewModel(AtomexApp);

            SelectPortfolio();
            SubscribeToServices();

            InstalledVersion = GetAssemblyFileVersion();
        }

        private IAtomexApp AtomexApp { get; set; }
        private PortfolioViewModel PortfolioViewModel { get; set; }
        private WalletsViewModel WalletsViewModel { get; set; }
        private ConversionViewModel ConversionViewModel { get; set; }
        private SettingsViewModel SettingsViewModel { get; set; }
        private WertViewModel WertViewModel { get; set; }

        [Reactive] public ViewModelBase? RightPopupContent { get; set; }
        [Reactive] public bool RightPopupOpened { get; set; }
        [Reactive] public ViewModelBase Content { get; set; }
        [Reactive] public string InstalledVersion { get; set; }
        [Reactive] public bool IsExchangeConnected { get; set; }
        [Reactive] public bool IsMarketDataConnected { get; set; }
        [Reactive] public bool IsQuotesProviderAvailable { get; set; }
        [ObservableAsProperty] public bool IsPortfolioSectionActive { get; }
        [ObservableAsProperty] public bool IsConversionSectionActive { get; }
        [ObservableAsProperty] public bool IsSettingsSectionActive { get; }
        [ObservableAsProperty] public bool IsWertSectionActive { get; }
        [ObservableAsProperty] public bool RightPopupHasContent { get; }


        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args.AtomexClient;
            if (atomexClient?.Account == null)
            {
                SelectPortfolio();
                ShowRightPopupContent(null);
                return;
            }

            atomexClient.ServiceConnected += OnAtomexClientServiceStateChangedEventHandler;
            atomexClient.ServiceDisconnected += OnAtomexClientServiceStateChangedEventHandler;
        }

        private void OnAtomexClientServiceStateChangedEventHandler(object sender, AtomexClientServiceEventArgs args)
        {
            if (sender is not IAtomexClient atomexClient)
                return;

            IsExchangeConnected = atomexClient.IsServiceConnected(AtomexClientService.Exchange);
            IsMarketDataConnected = atomexClient.IsServiceConnected(AtomexClientService.MarketData);

            // subscribe to symbols updates
            if (args.Service != AtomexClientService.MarketData || !IsMarketDataConnected) return;

            atomexClient.SubscribeToMarketData(SubscriptionType.TopOfBook);
            atomexClient.SubscribeToMarketData(SubscriptionType.DepthTwenty);
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            IsQuotesProviderAvailable = provider.IsAvailable;
        }

        public void ShowRightPopupContent(ViewModelBase? popupContent)
        {
            switch (popupContent)
            {
                case null:
                    RightPopupOpened = false;
                    ConversionViewModel.DGSelectedIndex = -1;

                    _ = Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(DelayBeforeSwitchingSwapDetailsMs + 100));
                        if (!RightPopupOpened)
                        {
                            RightPopupContent = null;
                            WalletsViewModel.Selected.SelectedTransaction = null;
                        }
                    });
                    return;

                // allow showing SwapDetails only when Conversion page is active
                case SwapDetailsViewModel when Content is not ViewModels.ConversionViewModel:
                    return;

                // allow showing TransactionDetails only when WalletsViewModel page is active
                case TransactionViewModelBase when Content is not ViewModels.WalletsViewModel:
                    return;

                default:
                    RightPopupContent = null;
                    RightPopupContent = popupContent;
                    break;
            }
        }


        public void SelectPortfolio()
        {
            Content = PortfolioViewModel;
        }

        private void SelectCurrencyWallet(string? currencyDescription = null)
        {
            if (currencyDescription != null)
            {
                WalletsViewModel.Selected = WalletsViewModel
                    .Wallets
                    .First(wallet => wallet.Header == currencyDescription);
            }

            Content = WalletsViewModel;
        }

        public void SelectConversion(CurrencyConfig? fromCurrency = null)
        {
            Content = ConversionViewModel;
            if (fromCurrency != null)
            {
                ConversionViewModel.SetFromCurrency(fromCurrency);
            }
        }

        public void SelectSettings()
        {
            Content = SettingsViewModel;
        }

        public void SelectWert(string? currencyDescription = null)
        {
            if (currencyDescription != null)
            {
                WertViewModel.Selected = WertViewModel
                    .Wallets
                    .First(wallet => wallet.Header == currencyDescription);
            }

            Content = WertViewModel;
        }

        private static string GetAssemblyFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersion.FileVersion;
        }
    }
}