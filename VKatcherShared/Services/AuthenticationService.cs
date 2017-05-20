using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VKatcherShared.Models;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;

namespace VKatcherShared.Services
{
    public class AuthenticationService
    {
        public static string _clientID = "00000000481A8D3A";
        private const string _authHost = "https://oauth.vk.com/";
        private const string _VKclientID = "2274003";
        private const string _VKclientSecret = "hHbZxrka2uZ6jB1inYsH";

        #region VK

        public static async Task<bool> VKLogin()
        {
            HttpClient client = new HttpClient();
            var q = new QueryString
            {
                {"client_id",  _VKclientID},
                {"client_secret", _VKclientSecret },
                {"grant_type", "password" },
                {"username", "showbiznine@hotmail.com" },
                {"password", "hgssucks1" },
                {"scope", "all" },
                {"libverify_support", "1" }
            };
            var uri = new Uri(_authHost + "token?" + q);

            try
            {
                var res = await client.GetAsync(uri);
                var s = await res.Content.ReadAsStringAsync();

                var r = JsonConvert.DeserializeObject<VKToken>(s);
                StoreToken("VK", "test", r.access_token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static async Task<bool> VKLogin2()
        {
            var q = new QueryString
            {
                {"client_id",  "2685278"},
                {"scope",  "271390"},
                {"redirect_uri",  ""},
                {"display",  "touch"},
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
                var t = QueryString.Parse(res.Split('#')[1]);
                var tok = t["access_token"];
                StoreToken("VK", "test", tok);
                return true;
            }
            else
                return false;
        }

        public static async Task<string> GetVKAccessToken()
        {
            var vault = new PasswordVault();
            try
            {
                var tok = vault.Retrieve("VK", "test");
                return tok.Password;
            }
            catch (Exception)
            {
                var success = await VKLogin();
                if (success)
                    return vault.Retrieve("VK", "test").Password;
                else
                    return null;
            }   
        }

        public static async Task VKLogout()
        {
            var vault = new PasswordVault();
            try
            {
                var tok = vault.Retrieve("VK", "test");
                vault.Remove(tok);
            }
            catch (Exception)
            {
                
            }
        }
        #endregion

        #region OneDrive
        public static async Task<bool> OneDriveLogin()
        {
            var q = new QueryString
            {
                {"client_id",  _clientID},
                {"scope",  "onedrive.readwrite offline_access"},
                {"redirect_uri",  "https://login.live.com/oauth20_desktop.srf"},
                {"response_type",  "code"},
            };
            string startURI = "https://login.live.com/oauth20_authorize.srf?" + q;
            string endURI = "https://login.live.com/oauth20_desktop.srf";
            var result = await WebAuthenticationBroker.AuthenticateAsync
                (WebAuthenticationOptions.None,
                new Uri(startURI),
                new Uri(endURI));
            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var res = result.ResponseData.ToString();
                var split = res.Split('=', '&');
                var tok = await RedeemOneDriveCode(split[1], split[3]);
                StoreToken("OneDrive", "test", tok);
                return true;
            }
            else
                return false;
        }

        public static async Task<string> RedeemOneDriveCode(string code, string lc)
        {
            HttpClient client = new HttpClient();
            var uri = new Uri("https://login.live.com/oauth20_token.srf");
            var q = new QueryString
            {
                {"client_id", _clientID },
                {"redirect_uri", "https://login.live.com/oauth20_desktop.srf" },
                {"client_secret", "fb4qhzmqarEb3axIG6pjUWOLbgarjie6" },
                {"code", code },
                {"grant_type", "authorization_code" }
            };
            var res = await client.PostAsync(uri, new StringContent(q.ToString(), 
                Encoding.UTF8,
                "application/x-www-form-urlencoded"));
            var s = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<OneDriveToken>(s);
            return r.access_token;
        }

        public static async Task<string> GetOneDriveAccessToken()
        {
            var vault = new PasswordVault();
            var tok = vault.Retrieve("OneDrive", "test");
            if (tok.Password == null)
            {
                var success = await VKLogin();
                if (success)
                    return vault.Retrieve("OneDrive", "test").Password;
                else
                    return null;
            }
            else
                return tok.Password;
        }
        #endregion

        #region Spotify

        #endregion

        #region General
        public static void StoreToken(string ServiceName, string Username, string Token)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(ServiceName, Username, Token));
        }

        public static bool CheckLoggedIn(string ServiceName, string Username)
        {
            var vault = new PasswordVault();
            try
            {
                var tok = vault.Retrieve(ServiceName, Username);
                if (tok == null)
                {
                    return false;
                }
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }            
        } 
        #endregion
    }
}
