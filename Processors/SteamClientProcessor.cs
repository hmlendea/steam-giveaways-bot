using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using NuciExtensions;

using SteamKit2;
using SteamKit2.Internal;

using SteamGiveawaysBot.Entities;
using SteamGiveawaysBot.Utils;

namespace SteamGiveawaysBot.Processors
{
    public class SteamClientProcessor : IDisposable
    {
        readonly SteamClient steamClient;
        readonly CallbackManager manager;

        readonly SteamUser user;
        readonly AccountDetails account;

        public bool IsRunning { get; private set; }

        public bool IsLoggedIn { get; private set; }

        public bool IsConnected { get; private set; }

        public bool IsDisposed { get; private set; }

        public SteamClientProcessor(AccountDetails account)
        {
            steamClient = new SteamClient();
            manager = new CallbackManager(steamClient);

            user = steamClient.GetHandler<SteamUser>();

            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            this.account = account;
        }

        ~SteamClientProcessor()
        {
            Dispose();
        }

        public void LogIn()
        {
            IsRunning = true;
            steamClient.Connect();

            new Thread(() =>
            {
                while (IsRunning)
                {
                    manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
            }).Start();

            Thread.Sleep(1000);
        }

        public void LogOut()
        {
            user.LogOff();
        }

        public void PlayGames(IEnumerable<uint> gameIds)
        {
            PlayGames(gameIds, TimeSpan.FromSeconds(2));
        }

        public void PlayGames(IEnumerable<uint> gameIds, TimeSpan duration)
        {
            WaitUntilLoggedIn();

            IList<uint> randomisedAppIds = gameIds
                .Where(gameID => gameID != 0)
                .ToList()
                .Shuffle();

            ClientMsgProtobuf<CMsgClientGamesPlayed> request =
                new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            foreach (uint appId in randomisedAppIds)
            {
                CMsgClientGamesPlayed.GamePlayed gamePlayed = new CMsgClientGamesPlayed.GamePlayed
                {
                    game_id = new GameID(appId)
                };

				request.Body.games_played.Add(gamePlayed);
			}

			steamClient.Send(request);

            Thread.Sleep((int)duration.TotalMilliseconds);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed || !disposing)
            {
                return;
            }

            if (IsLoggedIn)
            {
                LogOut();
            }

            steamClient.Disconnect();

            IsDisposed = true;
        }

        void OnConnected(SteamClient.ConnectedCallback callback)
        {
            IsConnected = true;

            SteamUser.LogOnDetails loginDetails = new SteamUser.LogOnDetails
            {
                Username = account.Username,
                Password = account.Password
            };

            user.LogOn(loginDetails);
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            IsConnected = false;
            IsRunning = false;
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                IsRunning = false;

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    Console.WriteLine("err login");
                    throw new AuthenticationException("Unable to logon to Steam: This account is SteamGuard protected.");
                }

                throw new AuthenticationException($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
            }

            IsLoggedIn = true;
        }

        void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            IsLoggedIn = false;
        }

        void WaitUntilLoggedIn()
        {
            DateTime endTime = DateTime.Now.AddSeconds(5);

            while (!IsLoggedIn && DateTime.Now <= endTime)
            {
                Thread.Sleep(500);
            }

            if (!IsLoggedIn)
            {
                throw new AuthenticationException("Not logged in");
            }
        }

        void WaitUntilConnected()
        {
            DateTime endTime = DateTime.Now.AddSeconds(5);

            while (!IsConnected && steamClient.IsConnected && DateTime.Now <= endTime)
            {
                Thread.Sleep(1000);
            }

            if (!IsConnected || !steamClient.IsConnected)
            {
                throw new AuthenticationException("Steam is not connected");
            }
        }
    }
}
