using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomex.Common;
using Atomex.Services;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using OxyPlot;
using OxyPlot.Avalonia;
using OxyPlot.Series;
using PieSeries = OxyPlot.Series.PieSeries;


namespace Atomex.Client.Desktop.ViewModels
{
    public class PortfolioViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }

        public PlotModel PlotModel { get; set; }
        public IList<CurrencyViewModel> AllCurrencies { get; set; }
        private Color NoTokensColor { get; } = Color.FromArgb(50, 0, 0, 0);

        private decimal _portfolioValue;

        public decimal PortfolioValue
        {
            get => _portfolioValue;
            set
            {
                _portfolioValue = value;
                this.RaisePropertyChanged(nameof(PortfolioValue));
            }
        }

        public PortfolioViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public PortfolioViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            AllCurrencies = e.AtomexClient?.Account?.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c);
                    vm.AmountUpdated += OnAmountUpdatedEventHandler;
                    return vm;
                })
                .ToList() ?? new List<CurrencyViewModel>();

            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }

        private void OnAmountUpdatedEventHandler(object sender, EventArgs args)
        {
            // update total portfolio value
            PortfolioValue = AllCurrencies.Sum(c => c.TotalAmountInBase);

            // update currency portfolio percent
            AllCurrencies.ForEachDo(c => c.PortfolioPercent = PortfolioValue != 0
                ? c.TotalAmountInBase / PortfolioValue
                : 0);
            
            this.RaisePropertyChanged(nameof(AllCurrencies));

            UpdatePlotModel();
        }

        private void UpdatePlotModel()
        {
            var series = new PieSeries
            {
                StrokeThickness = 0,
                StartAngle = 0,
                AngleSpan = 360,
                TickHorizontalLength = 0,
                TickRadialLength = 0,
                OutsideLabelFormat = string.Empty,
                InsideLabelFormat = string.Empty,
                TrackerFormatString = "{1}: ${2:0.00} ({3:P2})"
            };

            foreach (var currency in AllCurrencies)
            {
                series.Slices.Add(
                    new PieSlice(currency.Currency.Name, (double) currency.TotalAmountInBase)
                    {
                        Fill = currency.AccentColor.ToOxyColor()
                    });
            }

            if (PortfolioValue == 0)
            {
                series.Slices.Add(new PieSlice(Properties.Resources.PwNoTokens, 1) {Fill = NoTokensColor.ToOxyColor()});
                series.TrackerFormatString = "{1}";
            }

            PlotModel = new PlotModel {Culture = CultureInfo.InvariantCulture};
            PlotModel.Series.Add(series);

            this.RaisePropertyChanged(nameof(PlotModel));
        }

        private IController _actualController;
        public IController ActualController
        {
            get => _actualController;
            set
            {
                _actualController = value;
            }
        }

        private void DesignerMode()
        {
            var random = new Random();

            PortfolioValue = 423432.23m;
            
            AllCurrencies = DesignTime.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false);
                    vm.TotalAmountInBase = random.Next(100);
                    return vm;
                })
                .ToList();
            
            OnAmountUpdatedEventHandler(this, EventArgs.Empty);
        }
    }
}