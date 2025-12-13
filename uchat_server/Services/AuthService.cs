using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat_server.DAL;
using uchat_shared.Net;

namespace uchat_server.Services
{
    public  class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public User Register(string username, string password)
        {
            if (_userRepository.IsUsernameTaken(username))
            {
                return null;
            }

            string newUid = Guid.NewGuid().ToString();
            string passwordHash = HashPassword(password);

            _userRepository.AddUser(username, passwordHash, newUid);

            return new User
            {
                Username = username,
                UID = newUid,
                PasswordHash = passwordHash
            };
        }

        public User Login(string username, string password)
        {
            var user = _userRepository.GetUserByUsername(username);

            if (user == null)
            {
                return null;
            }

            if (VerifyPassword(password, user.PasswordHash))
            {
                string newUid = Guid.NewGuid().ToString();
                _userRepository.UpdateUserUID(username, newUid);
                user.UID = newUid;
                return user;
            }

            return null;
        }
    }
}
