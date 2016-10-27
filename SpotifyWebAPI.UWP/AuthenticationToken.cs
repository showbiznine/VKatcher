using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpotifyWebAPI
{
    public class AuthenticationToken
    {
        private string accessToken;

        /// <summary>
        /// An access token that can be provided in subsequent calls, for example to Spotify Web API services. 
        /// 
        /// refreshes the token automatically if it has expired
        /// </summary>
        public string AccessToken
        {
            get
            {
                if (HasExpired)
                    Refresh();

                return accessToken;
            }
            set
            {
                accessToken = value;
            }
        }

        /// <summary>
        /// How the access token may be used: always "Bearer". 
        /// </summary>
        public string TokenType { get; set; }
        
        /// <summary>
        /// The date/time that this token will become invalid
        /// </summary>
        public DateTime ExpiresOn { get; set; }

        /// <summary>
        /// A token that can be sent to the Spotify Accounts service in place of an authorization code. 
        /// (When the access code expires, send a POST request to the Accounts service /api/token endpoint, but 
        /// use this code in place of an authorization code. A new access token and a new refresh token will be returned.) 
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Determines if this token has expired
        /// </summary>
        public bool HasExpired { get { return DateTime.Now > ExpiresOn; } }

        /// <summary>
        /// Updates this token if it has expired
        /// </summary>
        public async void Refresh()
        {
            var token = await Authentication.GetAccessToken(this.RefreshToken);
            this.accessToken = token.accessToken;
            this.ExpiresOn = token.ExpiresOn;
            this.RefreshToken = token.RefreshToken;
            this.TokenType = this.TokenType;
        }
    }
}
