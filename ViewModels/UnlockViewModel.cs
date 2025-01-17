using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels
{
    public class UnlockViewModel : ViewModelBase
    {
        public Action? Unlocked;
        public event EventHandler<ErrorEventArgs> Error;
        public PasswordControlViewModel PasswordVM { get; set; }
        public string WalletName { get; set; }

        [Reactive] public bool InProgress { get; set; }
        [Reactive] public bool InvalidPassword { get; set; }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= (_unlockCommand = ReactiveCommand.Create(OnUnlockClick));

        private readonly Action<SecureString> _unlockAction;
        private readonly Action _goBackAction;

        public UnlockViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public UnlockViewModel(
            string walletName,
            Action<SecureString> unlockAction,
            Action goBack,
            Action? onUnlock = null)
        {
            WalletName = $"\"{walletName}\"";
            _unlockAction = unlockAction;
            _goBackAction += goBack;
            Unlocked += onUnlock;

            PasswordVM = new PasswordControlViewModel(
                onPasswordChanged: () => { InvalidPassword = false; },
                placeholder: "Password...",
                isSmall: true)
            {
                IsFocused = true
            };
        }

        private async void OnUnlockClick()
        {
            InvalidPassword = false;
            InProgress = true;

            try
            {
                await Task.Run(() => { _unlockAction(PasswordVM.SecurePass); });
            }
            catch (CryptographicException e)
            {
                Log.Error("Invalid password error");

                InvalidPassword = true;
                InProgress = false;

                Error?.Invoke(this, new ErrorEventArgs(e));
                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unlocking error");

                InProgress = false;

                Error?.Invoke(this, new ErrorEventArgs(e));
                return;
            }
            finally
            {
                InProgress = false;
            }

            PasswordVM.StringPass = string.Empty;

            Unlocked?.Invoke();
        }

        public void GoBackCommand()
        {
            _goBackAction?.Invoke();
        }

        private void DesignerMode()
        {
            InProgress = true;
        }
    }
}