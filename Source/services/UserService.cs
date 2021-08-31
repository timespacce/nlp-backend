using Microsoft.IdentityModel.Tokens;
using ML_Interpretation_Engine.Application;
using ML_Interpretation_Engine.Source.Requests;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ML_Interpretation_Engine.Source
{
    public interface IUserService
    {
        string Login(string userName, string password);
    }

    public class UserService : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private readonly List<User> users = new List<User>
        {
            new User { id = 1, username = "Adesso", password = "AdessoPassword1", role = "admin"},
            new User { id = 2, username = "Karamel", password = "KaramelPassword1", role = "guest"}
        };

        public string Login(string username, string password)
        {
            var user = users.SingleOrDefault(x => x.username == username && x.password == password);

            // return null if user not found
            if (user == null) return string.Empty;

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Startup.SECRET);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.username),
                    new Claim(ClaimTypes.Role, user.role)
                }),

                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.token = tokenHandler.WriteToken(token);

            return user.token;
        }
    }
}
