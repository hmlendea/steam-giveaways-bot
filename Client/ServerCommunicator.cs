using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using NuciAPI.Requests;
using NuciAPI.Responses;
using NuciSecurity.HMAC;

using SteamGiveawaysBot.Configuration;

namespace SteamGiveawaysBot.Client
{
    public class ServerCommunicator<TRequest>(BotSettings botSettings) : IServerCommunicator<TRequest>
        where TRequest : Request
    {
        readonly HttpClient httpClient = new();

        protected BotSettings botSettings = botSettings ?? throw new ArgumentNullException(nameof(botSettings));

        public async Task<HttpResponseMessage> SendRequest(HttpMethod method, string endpoint, TRequest request)
        {
            request.HmacToken = HmacEncoder.GenerateToken(request, botSettings.SharedSecretKey);

            using HttpRequestMessage httpRequest = new(method, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest);
            await ValdiateHttpRespone(httpResponse);

            return httpResponse;
        }

        static async Task<ErrorResponse> DeserialiseErrorResponse(HttpResponseMessage httpResponse)
            => JsonSerializer.Deserialize<ErrorResponse>(await httpResponse.Content.ReadAsStringAsync());

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
        where TRequest : Request
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
            => JsonSerializer.Deserialize<TResponse>(await httpResponse.Content.ReadAsStringAsync());

        static void ValdiateResponse(TResponse response, string sharedSecretKey)
        {
            if (!HmacEncoder.IsTokenValid(response.HmacToken, response, sharedSecretKey))
            {
                throw new AuthenticationException("The response HMAC token is not valid");
            }
        }
    }
}
