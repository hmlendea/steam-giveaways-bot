using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using NuciSecurity.HMAC;

using SteamGiveawaysBot.Configuration;
using SteamGiveawaysBot.Client.Models;

namespace SteamGiveawaysBot.Client
{
    public class ServerCommunicator<TRequest> : IServerCommunicator<TRequest>
        where TRequest : class
    {
        readonly HttpClient httpClient;

        readonly IHmacEncoder<TRequest> requestHmacEncoder;

        readonly BotSettings botSettings;

        public ServerCommunicator(
            IHmacEncoder<TRequest> requestHmacEncoder,
            BotSettings botSettings)
        {
            httpClient = new HttpClient();

            this.requestHmacEncoder = requestHmacEncoder;
            this.botSettings = botSettings;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string endpoint, TRequest request)
        {
            if (!endpoint.Contains("&hmac="))
            {
                string hmacToken = requestHmacEncoder.GenerateToken(request, botSettings.SharedSecretKey);
                endpoint += $"&hmac={HttpUtility.UrlEncode(hmacToken)}";
            }

            HttpResponseMessage httpResponse = await SendHttpRequest(method, endpoint);
            await ValdiateHttpRespone(httpResponse);
            
            return httpResponse;
        }

        async Task<HttpResponseMessage> SendHttpRequest(HttpMethod method, string endpoint)
        {
            if (method == HttpMethod.Get)
            {
                return await httpClient.GetAsync(endpoint);
            }
            else if (method == HttpMethod.Post)
            {
                return await httpClient.PostAsync(endpoint, null);
            }
            else
            {
                throw new ArgumentException("Unsupported HTTP method");
            }
        }

        async Task<ErrorResponse> DeserialiseErrorResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();
            ErrorResponse response = JsonConvert.DeserializeObject<ErrorResponse>(responseString);

            return response;
        }

        async Task ValdiateHttpRespone(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                ErrorResponse errorResponse = await DeserialiseErrorResponse(httpResponse);
                throw new Exception(errorResponse.ErrorMessage);
            }
        }
    }
        
    public class ServerCommunicator<TRequest, TResponse> : ServerCommunicator<TRequest>, IServerCommunicator<TRequest, TResponse>
        where TRequest : class
        where TResponse : Response
    {
        readonly HttpClient httpClient;

        readonly IHmacEncoder<TRequest> requestHmacEncoder;

        readonly IHmacEncoder<TResponse> responseHmacEncoder;

        readonly BotSettings botSettings;

        public ServerCommunicator(
            IHmacEncoder<TRequest> requestHmacEncoder,
            IHmacEncoder<TResponse> responseHmacEncoder,
            BotSettings botSettings)
            : base(requestHmacEncoder, botSettings)
        {
            httpClient = new HttpClient();

            this.requestHmacEncoder = requestHmacEncoder;
            this.responseHmacEncoder = responseHmacEncoder;
            this.botSettings = botSettings;
        }

        // TODO: I don't like this 'new'
        public new async Task<TResponse> SendRequest(HttpMethod method, string endpoint, TRequest request)
        {
            HttpResponseMessage httpResponse = await base.SendRequest(method, endpoint, request);
            TResponse response = await DeserialiseSuccessResponse(httpResponse);

            ValdiateResponse(response, botSettings.SharedSecretKey);
            
            return response;
        }

        async Task<TResponse> DeserialiseSuccessResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();
            TResponse response = JsonConvert.DeserializeObject<TResponse>(responseString);

            return response;
        }

        void ValdiateResponse(TResponse response, string sharedSecretKey)
        {
            if (!responseHmacEncoder.IsTokenValid(response.HmacToken, response, sharedSecretKey))
            {
                throw new AuthenticationException("The response HMAC token is not valid");
            }
        }
    }
}
