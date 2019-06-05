using System;
using System.Net.Http;
using System.Threading.Tasks;

using SteamGiveawaysBot.Service.Models;

namespace SteamGiveawaysBot.Service
{
    public interface IServerCommunicator<TRequest>
        where TRequest : class
    {
        Task<HttpResponseMessage> SendRequest(HttpMethod method, string endpoint, TRequest request);
    }
    
    public interface IServerCommunicator<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        Task<TResponse> SendRequest(HttpMethod method, string endpoint, TRequest request);
    }
}
