using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using NuciAPI.Responses;
using NuciSecurity.HMAC;

using SteamGiveawaysBot.Configuration;

namespace SteamGiveawaysBot.Client
{
    public class ServerCommunicator<TRequest>(BotSettings botSettings) : IServerCommunicator<TRequest>
        where TRequest : class
    {
        readonly HttpClient httpClient = new();

        protected BotSettings botSettings = botSettings ?? throw new ArgumentNullException(nameof(botSettings));

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string endpoint, TRequest request)
        {
            if (!endpoint.Contains("&hmac="))
            {
                string hmacToken = HmacEncoder.GenerateToken(request, botSettings.SharedSecretKey);
                endpoint += $"&hmac={HttpUtility.UrlEncode(hmacToken)}";
            }

            HttpResponseMessage httpResponse = await SendHttpRequest(method, endpoint);
            await ValdiateHttpRespone(httpResponse);

            return httpResponse;
        }

        async Task<HttpResponseMessage> SendHttpRequest(HttpMethod method, string endpoint)
        {
            if (method.Equals(HttpMethod.Get))
            {
                return await httpClient.GetAsync(endpoint);
            }

            if (method.Equals(HttpMethod.Post))
            {
                return await httpClient.PostAsync(endpoint, null);
            }

            throw new ArgumentException("Unsupported HTTP method");
        }

        static async Task<ErrorResponse> DeserialiseErrorResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ErrorResponse>(responseString);
        }

        static async Task ValdiateHttpRespone(HttpResponseMessage httpResponse)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                return;
            }

            ErrorResponse errorResponse = await DeserialiseErrorResponse(httpResponse);

            throw new Exception(errorResponse.Message);
        }
    }

    public class ServerCommunicator<TRequest, TResponse>(BotSettings botSettings) : ServerCommunicator<TRequest>(botSettings), IServerCommunicator<TRequest, TResponse>
        where TRequest : class
        where TResponse : Response
    {
        // TODO: I don't like this 'new'
        public new async Task<TResponse> SendRequest(HttpMethod method, string endpoint, TRequest request)
        {
            HttpResponseMessage httpResponse = await base.SendRequest(method, endpoint, request);
            TResponse response = await DeserialiseSuccessResponse(httpResponse);

            ValdiateResponse(response, botSettings.SharedSecretKey);

            return response;
        }

        static async Task<TResponse> DeserialiseSuccessResponse(HttpResponseMessage httpResponse)
        {
            string responseString = await httpResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(responseString);
        }

        static void ValdiateResponse(TResponse response, string sharedSecretKey)
        {
            if (!HmacEncoder.IsTokenValid(response.HmacToken, response, sharedSecretKey))
            {
                throw new AuthenticationException("The response HMAC token is not valid");
            }
        }
    }
}
