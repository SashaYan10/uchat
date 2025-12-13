using uchat_shared.Net;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.Services.AuthService
{
    public class Auth(NetworkClass networkClass)
    {
        private readonly NetworkClass _network = networkClass;

        private const string UsernameKey = "username_key";
        private const string PasswordKey = "password_key";

        public static async Task<bool> IsLoggedAsync()
        {
            string? username = await SecureStorage.Default.GetAsync(UsernameKey);
            string? password = await SecureStorage.Default.GetAsync(PasswordKey);

            return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }

        public static async Task<(string username, string password)> GetSavedUserDataAsync()
        {
            string? username = await SecureStorage.Default.GetAsync(UsernameKey);
            string? password = await SecureStorage.Default.GetAsync(PasswordKey);

            if (string.IsNullOrEmpty(password))
            {
                return (username ?? string.Empty, string.Empty);
            }

            string decryptedPassword = AesCryptography.DecryptPassword(password);

            return (username, decryptedPassword);
        }

        public static async Task SaveUserDataAsync(string username, string password)
        {
            string encryptedPassword = AesCryptography.EncryptPassword(password);

            await SecureStorage.Default.SetAsync(UsernameKey, username);
            await SecureStorage.Default.SetAsync(PasswordKey, encryptedPassword);
        }

        private async Task<(bool isSuccess, string message)> SendAuthPacketAsync(PacketType type, string username, string password)
        {
            var tcs = new TaskCompletionSource<(bool, string)>();

            void Handler(PacketModel packetModel)
            {
                if (packetModel.Type == PacketType.AuthSuccess)
                {
                    tcs.TrySetResult((true, packetModel.UID));
                }
                if (packetModel.Type == PacketType.AuthFailure)
                {
                    tcs.TrySetResult((false, packetModel.Text));
                }
            }

            _network.OnPacketReceived += Handler;

            try
            {
                await _network.SendPacketAsync(new PacketModel
                {
                    Type = type,
                    Username = username,
                    Password = password
                });

                var result = tcs.Task;
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(result, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    return (false, "Server is not responding. Try again later.");
                }

                return await result;
            }
            finally
            {
                _network.OnPacketReceived -= Handler;
            }
        }

        public async Task<(bool isSuccess, string error)> RegisterUserAsync(string username, string password)
        {
            var result = await SendAuthPacketAsync(PacketType.Register, username, password);

            if (result.isSuccess)
            {
                await SaveUserDataAsync(username, password);
            }

            return result;
        }

        public async Task<(bool isSuccess, string error)> LoginUserAsync(string username, string password)
        {
            var result = await SendAuthPacketAsync(PacketType.Login, username, password);

            if (result.isSuccess)
            {
                await SaveUserDataAsync(username, password);
            }

            return result;
        }

        public static void Logout()
        {
            SecureStorage.Default.RemoveAll();
        }
    }
}
