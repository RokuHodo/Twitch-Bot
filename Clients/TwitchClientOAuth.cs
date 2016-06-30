using System.IO;

using RestSharp;

using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Extensions;
using TwitchChatBot.Chat;
using TwitchChatBot.Interfaces;
using TwitchChatBot.Models.TwitchAPI;

namespace TwitchChatBot.Clients
{
    class TwitchClientOAuth : TwitchClient, ITwitchClient
    {
        private string client_id,
                       user_token;

        public string name,
                      display_name;

        public Connection connection;
                
        public TwitchClientOAuth(string _client_id, string _user_token) : base()
        {
            client_id = _client_id;
            user_token = _user_token;

            name = GetAuthenticatedUser().name;
            display_name = GetAuthenticatedUser().display_name;

            connection = new Connection(ConnectionType.Chat, name, _user_token);
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

        #region Chat commands

        public void Purge(string channel, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, ".timeout " + channel.ToLower() + " 1" + " " + reason);
        }

        public void Timeout(string channel, int seconds, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, ".timeout " + channel.ToLower() + " " + seconds.ToString() + " " + reason);
        }

        public void Ban(string channel, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/ban " + channel.ToLower() + " " + reason);
        }

        public void Unban(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/unban " + channel.ToLower());
        }

        public void Mod(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/mod " + channel.ToLower());
        }

        public void Unmod(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/unmod " + channel.ToLower());
        }

        #endregion

        #region Send messages or whispers

        public void SendMessage(string room, string message)
        {
            if (!room.CheckString() || !message.CheckString() || !connection.isConnected())
            {
                return;
            }

            connection.writer.WriteLine(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :{2}", name.ToLower(), room.ToLower(), message);
            connection.writer.Flush();
        }

        public void SendWhisper(string recipient, string whisper)
        {
            if (!whisper.CheckString() || !connection.isConnected())
            {
                return;
            }

            connection.writer.WriteLine("PRIVMSG #jtv :/w {0} {1}", recipient.ToLower(), whisper);
            connection.writer.Flush();
        }

        public void SendResponse(MessageType message_type, Message message, string message_or_whisper)
        {
            if(message_type == MessageType.Chat)
            {
                SendMessage(message.room, message_or_whisper);
            }
            else
            {
                SendWhisper(message.sender.name, message_or_whisper);
            }
        }

        #endregion

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
            request.AddHeader("Authorization", string.Format("OAuth {0}", user_token));

            return request;
        }
    }
}
