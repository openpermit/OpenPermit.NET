using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenPermit.Accela
{
    [DataContract]
    public class AccessToken
    {
        [DataMember(Name = "access_token")]
        public string Token { get; set; }

        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "expires_in")]
        public int ExpiresInSeconds { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }
    }

    [DataContract]
    public class User
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "loginName")]
        public string LoginName { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "firstName")]
        public string FirstName { get; set; }

        [DataMember(Name = "lastName")]
        public string LastName { get; set; }

        [DataMember(Name = "countryCode")]
        public string CountryCode { get; set; }

        [DataMember(Name = "streetAddress")]
        public string StreetAddress { get; set; }

        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "postalCode")]
        public string PostalCode { get; set; }

        [DataMember(Name = "phoneCountryCode")]
        public string PhoneCountryCode { get; set; }

        [DataMember(Name = "phoneAreaCode")]
        public string PhoneAreaCode { get; set; }

        [DataMember(Name = "phoneNumber")]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "avatarUrl")]
        public string AvatarUrl { get; set; }
    }

    public class CivicIDOAuthClient : OAuth2Client
    {
        private const int ApiVersion = 3;
        private const string AuthorizationEndPoint = "https://auth.accela.com/oauth2/authorize"; // e.g. "https://auth.accela.com/oauth2/authorize"
        private const string TokenEndPoint = "https://apis.accela.com/oauth2/token"; // e.g. "https://apis.accela.com/oauth2/token"
        private const string UserProfileEndPoint = "https://apis.accela.com/v3/users/me"; // e.g. "https://apis.accela.com/v3/users/me"

        private readonly string appId;
        private readonly string appSecret;
        private readonly string scope;

        private string profileScope = string.Empty;
        private string agencyName;
        private string environment;

        public CivicIDOAuthClient()
            : base("Civic ID")
        {
        }

        public CivicIDOAuthClient(string appId, string appSecret)
            : this(appId, appSecret, (ApiVersion == 3) ? "get_user_profile" : "get_my_profile", "TEST")
        {   
        }

        // Initializes provider with custom name
        public CivicIDOAuthClient(string appId, string appSecret, string scope, string environment)
            : base("Civic ID")
        {
            if (appId == null)
            {
                throw new ArgumentNullException("Application id is required");
            }

            if (appSecret == null)
            {
                throw new ArgumentNullException("Application secret is required");
            }

            // Requires.NotNullOrEmpty(appId, "appId");
            // Requires.NotNullOrEmpty(appSecret, "appSecret");
            // Requires.NotNullOrEmpty(scope, "scope");
            this.appId = appId;
            this.appSecret = appSecret;
            this.environment = environment;
            this.scope = scope;
        }

        public string Environment
        {
            get { return this.environment; }
            set { this.environment = value; }
        }

        public string AgencyName
        {
            get { return this.agencyName; }
            set { this.agencyName = value; }
        }

        public AccessToken QueryAccessToken(string userName, string password)
        {
            var queryString = new StringBuilder();

            queryString.Append("grant_type=password");
            queryString.Append("&client_id=" + this.appId);
            queryString.Append("&client_secret=" + this.appSecret);
            queryString.Append("&username=" + userName);
            queryString.Append("&password=" + password);
            queryString.Append("&scope=" + this.scope);

            if (!string.IsNullOrEmpty(this.agencyName))
            {
                queryString.Append("&environment=" + this.environment);
                queryString.Append("&agency_name=" + this.agencyName);
            }

            var query = queryString.ToString();

            var tokenRequest = WebRequest.Create(TokenEndPoint);
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.ContentLength = query.Length;
            tokenRequest.Method = "POST";
            
            // set headers
            tokenRequest.Headers.Add("x-accela-appid", this.appId);

            var response = this.SendPOSTRequest<AccessToken>(tokenRequest, query);
            return response;
        }

        public AuthenticationResult AuthenticateWithPassword(string userName, string password)
        {
            var accessToken = this.QueryAccessToken(userName, password);

            IDictionary<string, string> userData = this.GetUserData(accessToken.Token);
            if (userData == null)
            {
                return AuthenticationResult.Failed;
            }

            string id = userData["id"];
            string name;

            // Some oAuth providers do not return value for the 'username' attribute. 
            // In that case, try the 'name' attribute. If it's still unavailable, fall back to 'id'
            if (!userData.TryGetValue("username", out name) && !userData.TryGetValue("name", out name))
            {
                name = id;
            }

            // add the access token to the user data dictionary just in case page developers want to use it
            userData["accesstoken"] = accessToken.Token;

            return new AuthenticationResult(
                                             isSuccessful: true, 
                                             provider: this.ProviderName, 
                                             providerUserId: id, 
                                             userName: userName, 
                                             extraData: userData);
        }

        /// <summary>
        /// Generates service URL the user should be directed to
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var uriBuilder = new UriBuilder(AuthorizationEndPoint);
            var queryString = new StringBuilder();
           
            queryString.Append("response_type=code");
            queryString.Append("&client_id=" + this.appId);
            queryString.Append("&redirect_uri=" + HttpUtility.UrlEncode(returnUrl.AbsoluteUri));
            queryString.Append("&environment=" + this.environment);
            queryString.Append("&scope=" + this.scope);

            if (!string.IsNullOrEmpty(this.agencyName))
            {
                queryString.Append("&environment=" + this.environment);
                queryString.Append("&agency_name=" + this.agencyName);
            }

            uriBuilder.Query = queryString.ToString();
            
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Sends request to get user's CivicID profile
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var civicUser = new User();

            using (var client = new WebClient())
            {
                // set headers
                client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=utf-8;";
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = accessToken;
                client.Headers.Add("x-accela-appid", this.appId);

                var responseBody = client.DownloadString(UserProfileEndPoint);
                var json = JObject.Parse(responseBody);

                if (json["result"] != null) 
                {
                    // v4
                    civicUser = json["result"].ToObject<User>();
                }
                else 
                {
                    // v3
                    civicUser = JsonConvert.DeserializeObject<User>(responseBody);
                }
            }

            var userData = new Dictionary<string, string>();

            // Fill the Dictionary with user's data. This will be available through ExtraData Dictionary of the DotNetOpenAuth.AspNet.AuthenticationResult instance
            if (civicUser != null)
            {
                var login = !string.IsNullOrWhiteSpace(civicUser.LoginName) ? civicUser.LoginName : civicUser.Id.ToLower();

                userData.Add("id", login);
                userData.Add("civicId", civicUser.Id);
                userData.Add("email", civicUser.Email);
                userData.Add("login", login);
                userData.Add("firstName", civicUser.FirstName);
                userData.Add("lastName", civicUser.LastName);

                userData.Add("agencyName", this.agencyName);

                userData.Add("countryCode", civicUser.CountryCode);
                userData.Add("city", civicUser.City);
                userData.Add("streetAddress", civicUser.StreetAddress);
                userData.Add("state", civicUser.State);
                userData.Add("postalCode", civicUser.PostalCode);

                userData.Add("phoneCountryCode", civicUser.PhoneCountryCode);
                userData.Add("phoneAreaCode", civicUser.PhoneAreaCode);
                userData.Add("phoneNumber", civicUser.PhoneNumber);

                userData.Add("avatarUrl", civicUser.AvatarUrl);
            }

            return userData;
        }

        /// <summary>
        /// Exchanges authorization code for the access token
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var queryString = new StringBuilder();

            queryString.Append("client_id=" + this.appId);
            queryString.Append("&client_secret=" + this.appSecret);
            queryString.Append("&grant_type=authorization_code");
            queryString.Append("&code=" + authorizationCode);
            queryString.Append("&redirect_uri=" + HttpUtility.UrlEncode(returnUrl.AbsoluteUri));

            var query = queryString.ToString();

            var tokenRequest = WebRequest.Create(TokenEndPoint);
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.ContentLength = query.Length;
            tokenRequest.Method = "POST";
            
            // set headers
            tokenRequest.Headers.Add("x-accela-appid", this.appId);

            var response = this.SendPOSTRequest<AccessToken>(tokenRequest, query);

            if (response != null)
            {
                return response.Token;
            }

            return null;
        }

        private T SendPOSTRequest<T>(WebRequest request, string query)
        {
            using (Stream requestStream = request.GetRequestStream())
            {
                var writer = new StreamWriter(requestStream);
                writer.Write(query);
                writer.Flush();
            }

            var response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream);

                    return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                }
            }

            return default(T);
        }
    }
}