using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;

namespace VKatcherShared.Services
{
    public class AuthenticationService
    {
        #region VK
        public static async Task<bool> VKLogin()
        {
            var q = new QueryString
            {
                {"client_id",  "3697615"},
                {"client_secret",  "AlVXZFMUqyrnABp8ncuU"},
                {"scope",  "271390"},
                {"redirect_uri",  ""},
                {"display",  "popup"},
                {"v",  "5.62"},
                {"response_type",  "token"},
            };
            string startURI = "https://oauth.vk.com/authorize?" + q;
            string endURI = "https://oauth.vk.com/blank.html";
            var result = await WebAuthenticationBroker.AuthenticateAsync
                (WebAuthenticationOptions.None,
                new Uri(startURI),
                new Uri(endURI));
            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var res = result.ResponseData.ToString();
                var split = res.Split('=', '&');
                StoreToken("VK", "test", split[1]);
                return true;
            }
            else
                return false;
        }

        public static async Task<string> GetVKAccessToken()
        {
            var vault = new PasswordVault();
            var tok = vault.Retrieve("VK", "test");
            if (tok.Password == null)
            {
                var success = await AuthenticationService.VKLogin();
                if (success)
                    return vault.Retrieve("VK", "test").Password;
                else
                    return null;
            }
            else
                return tok.Password;
        } 
        #endregion

        public static void StoreToken(string ServiceName, string Username, string Token)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(ServiceName, Username, Token));
        }

        public static bool CheckLoggedIn(string ServiceName, string Username)
        {
            var vault = new PasswordVault();
            var tok = vault.Retrieve(ServiceName, Username);
            if (tok == null)
            {
                return false;
            }
            else
                return true;
        }
    }
}
