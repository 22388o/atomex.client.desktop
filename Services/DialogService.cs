using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.ViewModels;


namespace Atomex.Client.Desktop.Services
{
    public sealed class DialogService
    {
        private static string MainDialogHostIdentifier => "MainDialogHost";
        private bool _isDialogOpened;
        private readonly DialogServiceViewModel _dialogServiceViewModel;

        public DialogService()
        {
            _dialogServiceViewModel = new DialogServiceViewModel();
        }

        public bool Close()
        {
            var result = _isDialogOpened;
            if (!_isDialogOpened) return result;
            DialogHost.DialogHost.GetDialogSession(MainDialogHostIdentifier)?.Close();
            _isDialogOpened = false;

            return result;
        }

        public bool IsCurrentlyShowing(ViewModelBase vm)
        {
            return _dialogServiceViewModel.Content.GetType() == vm.GetType();
        }

        public void ShowPrevious()
        {
            if (_isDialogOpened) return;
            _ = DialogHost.DialogHost.Show(_dialogServiceViewModel, MainDialogHostIdentifier);
            _isDialogOpened = true;
        }


        public void Show(ViewModelBase viewModel)
        {
            _dialogServiceViewModel.Content = viewModel;
            ShowPrevious();
        }
    }
}