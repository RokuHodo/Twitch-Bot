using System;

using RestSharp;

using TwitchChatBot.Interfaces;
using TwitchChatBot.TwitchModels;

namespace TwitchChatBot.Clients
{
    class TwitchUserAuthenticated : TwitchUser, ITwitchUser
    {
        private string client_id,
                       user_token;

        public string name,
                      display_name;
                
        public TwitchUserAuthenticated(string client_id, string user_token) : base()
        {
            this.client_id = client_id;
            this.user_token = user_token;

            name = GetAuthenticatedUser().name;
            display_name = GetAuthenticatedUser().display_name;
        }

        /// <summary>
        /// Gets a user object of the specified channel by using the authentication token and a client id.
        /// </summary>
        public User GetAuthenticatedUser()
        {
            var request = Request("user", Method.GET);

            var response = client.Execute<User>(request);

            return response.Data;            
        }

        /// <summary>
        /// Sets the title of a channel.
        /// Requires the "channel_editor" scope.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="status">Title to set the stream to.</param>
        /// <returns></returns>
        public Channel SetTitle(string channel, string status)
        {
            RestRequest request = Request($"channels/{channel}", Method.PUT);

            request.AddUrlSegment("channel", channel);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { channel = new { status } });

            IRestResponse<Channel> response = client.Execute<Channel>(request);

            return response.Data;
        }

        /// <summary>
        /// Sets the game of a channel.
        /// Requires the "channel_editor" scope.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="game">Game to set the stream to.</param>
        /// <returns></returns>
        public Channel SetGame(string channel, string game)
        {
            RestRequest request = Request($"channels/{channel}", Method.PUT);

            request.AddUrlSegment("channel", channel);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { channel = new { game } });

            IRestResponse<Channel> response = client.Execute<Channel>(request);

            return response.Data;
        }

        /// <summary>
        /// Sets the stream delay of a channel.
        /// Requires the "channel_editor" scope.
        /// Note: The channel needs to be partnered to set a custom dtream delay.
        /// </summary>
        /// <param name="channel">Channel name.<./param>
        /// <param name="delay">Stream delay of the stream.</param>
        /// <returns></returns>
        public Channel SetDelay(string channel, string delay)
        {
            RestRequest request = Request($"channels/{channel}", Method.PUT);

            request.AddUrlSegment("channel", channel);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { channel = new { delay } });

            IRestResponse<Channel> response = client.Execute<Channel>(request);

            return response.Data;
        }

        /// <summary>
        /// Send the request to the api.
        /// Requires the authentication token of the broadcaster and the client id of the application from Twitch.
        /// </summary>
        /// <param name="url">Twitch api url.</param>
        /// <param name="method">Operation that is being performed.</param>
        /// <returns></returns>
        public new RestRequest Request(string url, Method method)
        {
            RestRequest request = new RestRequest(url, method);

            request.AddHeader("Client-ID", client_id);
            request.AddHeader("Authorization", String.Format("OAuth {0}", user_token));

            return request;
        }
    }
}
