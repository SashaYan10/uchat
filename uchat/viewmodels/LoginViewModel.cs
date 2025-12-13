using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using AuthClass = uchat.Services.AuthService.Auth;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isCheckingSession;

    private readonly AuthClass _auth;
    private readonly NetworkClass _network;

    private static bool _hasAlreadyCheckedSession = false;

    public ICommand GoToRegisterCommand { get; }
    public ICommand LoginCommand { get; }

    public LoginViewModel(NetworkClass network, AuthClass auth) {
        _network = network;
        _auth = auth;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        GoToRegisterCommand = new AsyncRelayCommand(GoToRegisterAsync);

        //_network.ConnectionStatusChanged += OnConnectionStatusChanged;

        if (!_hasAlreadyCheckedSession)
        {
            _hasAlreadyCheckedSession = true;
            _isCheckingSession = true;
            _ = CheckCompletedLoginAsync();
        }
        else
        {
            _isCheckingSession = false;
            //_ = SilentConnectAsync();
        }
    }

    //private void OnConnectionStatusChanged(bool isConnected)
    //{
    //    if (isConnected)
    //    {
    //        Message = string.Empty;
    //    }
    //}

    //private async Task SilentConnectAsync()
    //{
    //    if (!_network.IsConnected)
    //    {
    //        await _network.ConnectToServerAsync("127.0.0.1", 7891);
    //    }
    //}

    private async Task CheckCompletedLoginAsync()
    {
        await Task.Delay(500);

        //await SilentConnectAsync();

        if (!await _network.ConnectToServerAsync("127.0.0.1", 7891))
        {
            _isCheckingSession = false;
            return;
        }

        if (await AuthClass.IsLoggedAsync())
        {
            var userData = await AuthClass.GetSavedUserDataAsync();
            Username = userData.username;
            Password = userData.password;

            await LoginAsync();
        } 
        else
        {
            IsCheckingSession = false;
        }
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true; //////
            Message = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Please fill in all required fields!";
                IsBusy = false;
                IsCheckingSession = false;
                return;
            }

            IsBusy = true;
            Message = "Connecting...";

            bool isConnected = await _network.ConnectToServerAsync("127.0.0.1", 7891);

            if (!isConnected)
            {
                Message = "Unable to connect to the server. Please try again later.";
                IsBusy = false;
                IsCheckingSession = false;
                return;
            }

            (bool isSuccess, string error) = await _auth.LoginUserAsync(Username, Password);
            if (isSuccess)
            {
                //.ConnectionStatusChanged -= OnConnectionStatusChanged;
                await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                IsCheckingSession = false;
            }
            else
            {
                Message = $"Error: {error}";
                IsCheckingSession = false;
            }
        } 
        catch (Exception ex)
        {
            Message = $"Error: {ex}";
        }
        finally
        {
            IsCheckingSession = false;
            IsBusy = false;
        }
    }

    private static async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(RegistrationPage)}");
    }
}