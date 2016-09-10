using System;

using RestSharp;

using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Extensions;
using TwitchBot.Chat;
using TwitchBot.Interfaces;
using TwitchBot.Models.TwitchAPI;
using TwitchBot.Models.Bot.Chat;

namespace TwitchBot.Clients
{
    class TwitchClientOAuth : TwitchClient, ITwitchClient
    {
        private string client_id,
                       oauth_token;

        public string name,
                      display_name;

        public Connection connection;
                
        public TwitchClientOAuth(string _client_id, string _user_token) : base()
        {
            client_id = _client_id;
            oauth_token = _user_token;

            name = GetAuthenticatedUser().name;
            display_name = GetAuthenticatedUser().display_name;

            connection = new Connection(ConnectionType.Chat, name, oauth_token);
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
            RestRequest request = Request("channels/{channel}", Method.PUT);

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
            RestRequest request = Request("channels/{channel}", Method.PUT);

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
            RestRequest request = Request("channels/{channel}", Method.PUT);

            request.AddUrlSegment("channel", channel);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { channel = new { delay } });

            IRestResponse<Channel> response = client.Execute<Channel>(request);

            return response.Data;
        }

        #region Chat commands

        /// <summary>
        /// Purges a user for 1 second.
        /// </summary>
        /// <param name="channel">User to purge.</param>
        /// <param name="reason">Optional reason for the purge.</param>
        public void Purge(string channel, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, ".timeout " + channel.ToLower() + " 1" + " " + reason);
        }

        /// <summary>
        /// Times out a user for a specified amount of time.
        /// </summary>
        /// <param name="channel">User to timeout.</param>
        /// <param name="seconds">How long to time the user out for.</param>
        /// <param name="reason">Optional reason for the timeout.</param>
        public void Timeout(string channel, int seconds, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, ".timeout " + channel.ToLower() + " " + seconds.ToString() + " " + reason);
        }

        /// <summary>
        /// Bans a user.
        /// </summary>
        /// <param name="channel">User to be banned.</param>
        /// <param name="reason">Optional reason for the ban.</param>
        public void Ban(string channel, string reason = "")
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/ban " + channel.ToLower() + " " + reason);
        }

        /// <summary>
        /// Unbans a user.
        /// </summary>
        /// <param name="channel">User to unban.</param>
        public void Unban(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/unban " + channel.ToLower());
        }

        /// <summary>
        /// Mods a user.
        /// </summary>
        /// <param name="channel">User to mod.</param>
        public void Mod(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/mod " + channel.ToLower());
        }

        /// <summary>
        /// Unmods a user.
        /// </summary>
        /// <param name="channel">User to unmod.</param>
        public void Unmod(string channel)
        {
            connection.writer.WriteLine("PRIVMSG #{0} :{1}", name, "/unmod " + channel.ToLower());
        }

        #endregion

        #region Send messages or whispers

        /// <summary>
        /// Sends a message to the current chat room.
        /// </summary>
        /// <param name="room">Room to send the message to.</param>
        /// <param name="message">Message to send.</param>
        public void SendMessage(string room, string message)
        {
            if (!room.CheckString() || !message.CheckString() || !connection.isConnected())
            {
                return;
            }

            connection.writer.WriteLine(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :{2}", name.ToLower(), room.ToLower(), message);
            connection.writer.Flush();
        }

        /// <summary>
        /// Sends a whisper to a specified user.
        /// </summary>
        /// <param name="recipient">Who to send the whisper to.</param>
        /// <param name="whisper">The message to send to the user.</param>
        public void SendWhisper(string recipient, string whisper)
        {
            if (!whisper.CheckString() || !connection.isConnected())
            {
                return;
            }

            connection.writer.WriteLine("PRIVMSG #jtv :/w {0} {1}", recipient.ToLower(), whisper);
            connection.writer.Flush();
        }

        /// <summary>
        /// Wrapper to either send a chat message or a whisper.
        /// </summary>
        /// <param name="message_type">The type of message to be sent.</param>
        /// <param name="message">MEssage thaty contains the information in order to send the chat message or whisper</param>
        /// <param name="message_or_whisper">The message to be sent as a whisper or a chat message.</param>
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

        #region  Command wrappers

        /// <summary>
        /// Gets the uptime of the OAuth User.
        /// Called from Twitch by using <code>!uptime</code>.
        /// </summary>
        /// <returns></returns>
        public string GetUpTime()
        {
            if (!isLive(name))
            {
                return display_name + " is currently offline";
            }

            string total_time, prefix;

            TimeSpan time = GetUpTime(display_name);

            string hours = time.Hours.GetTimeString("hour"),
                   minutes = time.Minutes.GetTimeString("minute"),
                   seconds = time.Seconds.GetTimeString("second");

            prefix = display_name + " has been streaming for ";

            //hours does not have a value
            if (!hours.CheckString())
            {
                //(0, 0, 0)
                if (!minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = "currently offline";
                }
                //(0, 0, 1)
                else if (!minutes.CheckString() && seconds.CheckString())
                {
                    total_time = seconds;
                }
                //(0, 1, 0)
                else if (minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = minutes;
                }
                //(0, 1, 1)
                else
                {
                    total_time = minutes + " and " + seconds;
                }
            }
            //hours has a value
            else
            {
                //(1, 0, 0)
                if (!minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = hours;
                }
                //(1, 0, 1)
                else if (!minutes.CheckString() && seconds.CheckString())
                {
                    total_time = hours + " and " + seconds;
                }
                //(1, 1, 0)
                else if (minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = hours + " and " + minutes;
                }
                //(1, 1, 1)
                else
                {
                    total_time = hours + ", " + minutes + ", and " + seconds;
                }
            }

            return prefix + total_time;
        }

        /// <summary>
        /// Updates the broadcaster's game, title, or stream delay.
        /// Note: The broadcaster must be a partner to set a delay.
        /// Requires the "channel_editor" scope.
        /// </summary>
        /// <param name="stream_setting">Stream setting to update.</param>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/>.</param>
        /// <param name="message">The message to be parsed for the new stream setting.</param>
        public void UpdateStream(StreamSetting stream_setting, Commands commands, Message message)
        {
            string value = commands.ParseAfterCommand(message);

            if (!value.CheckString())
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "stream setting", stream_setting.ToString(), DebugError.NORMAL_NULL);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(stream_setting), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(stream_setting), stream_setting.ToString());
                DebugBot.PrintLine(nameof(value), "null");

                return;
            }

            if (stream_setting == StreamSetting.Delay)
            {
                if (!value.CanCovertTo<double>())
                {
                    Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "stream setting", stream_setting.ToString(), DebugError.NORMAL_CONVERT);

                    DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(stream_setting), DebugError.NORMAL_CONVERT);
                    DebugBot.PrintLine(nameof(value), value);
                    DebugBot.PrintLine(nameof(value) + " type", value.GetType().Name.ToLower());
                    DebugBot.PrintLine("supported type", typeof(double).Name);

                    return;
                }

                if (!isPartner(name))
                {
                    Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "stream setting", stream_setting.ToString(), display_name + " is not a partner");

                    DebugBot.PrintLine(DebugMessageType.ERROR, "Failed to update the " + stream_setting.ToString() + ": " + display_name + " is not a partner");

                    return;
                }
            }

            try
            {
                switch (stream_setting)
                {
                    case StreamSetting.Delay:
                        SetDelay(display_name.ToLower(), value);

                        value = GetChannel(display_name).delay.ToString();
                        break;
                    case StreamSetting.Game:
                        SetGame(display_name.ToLower(), value);

                        value = GetChannel(display_name).game;
                        break;
                    case StreamSetting.Title:
                        SetTitle(display_name.ToLower(), value);

                        value = GetChannel(display_name).status;
                        break;
                    default:
                        break;
                }

                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.UPDATE, message, "stream setting", stream_setting.ToString());

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(stream_setting));
                DebugBot.PrintLine(nameof(stream_setting), stream_setting.ToString());                
                DebugBot.PrintLine(nameof(value), value);
            }
            catch(Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "stream setting", stream_setting.ToString(), DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(stream_setting), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(stream_setting), stream_setting.ToString());
                DebugBot.PrintLine(nameof(exception), exception.Message);
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
            request.AddHeader("Authorization", string.Format("OAuth {0}", oauth_token));
            request.AddQueryParameter("noCache", DateTime.Now.Ticks.ToString());

            return request;
        }
    }
}
