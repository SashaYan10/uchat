using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using AuthClass = uchat.Services.AuthService.Auth;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _message;

    private readonly AuthClass _auth;
    private readonly NetworkClass _network;

    public ICommand GoToLoginCommand { get; }
    public ICommand RegisterCommand { get; }

    public RegistrationViewModel(NetworkClass network, AuthClass auth)
    {
        _network = network;
        _auth = auth;

        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        GoToLoginCommand = new AsyncRelayCommand(GoToLoginAsync);

        //_network.ConnectionStatusChanged += OnConnectionStatusChanged;

        //_ = SilentConnectAsync();
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

    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Message = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Please fill in all required fields!";
                IsBusy = false;
                return;
            }

            if (Password.Length < 6)
            {
                Message = "The password must be at least 6 characters long!";
                IsBusy = false;
                return;
            }

            bool isConnected = await _network.ConnectToServerAsync("127.0.0.1", 7891);

            if (!isConnected)
            {
                Message = "Unable to connect to the server. Please try again later.";
                IsBusy = false;
                return;
            }

            (bool isSuccess, string error) = await _auth.RegisterUserAsync(Username, Password);
            if (isSuccess)
            {
                //_network.ConnectionStatusChanged -= OnConnectionStatusChanged;
                await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
            }
            else
            {
                Message = $"Error: {error}";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}