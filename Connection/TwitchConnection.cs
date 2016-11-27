using System.IO;
using System.Net.Sockets;

using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;

namespace TwitchBot.Connection
{
    class TwitchConnection
    {
        private const int port = 6667;

        private string ip_address, user_name, oauth_token;

        private TcpClient tcp_Client;

        public StreamReader reader;
        public StreamWriter writer;

        ConnectionType connection_type;

        public TwitchConnection(ConnectionType _connection_type, string _user_name, string _oauth_token)
        {
            user_name = _user_name;
            oauth_token = _oauth_token;
            connection_type = _connection_type;

            ip_address = GetIP(connection_type);

            Connect();
        }

        /// <summary>
        /// Gets the IP address based on the connection type
        /// </summary>
        /// <param name="connection">The type of connection being established</param>
        /// <returns></returns>
        private string GetIP(ConnectionType connection)
        {
            string ip;

            switch (connection)
            {
                case ConnectionType.Chat:
                    ip = "irc.chat.twitch.tv";
                    break;
                case ConnectionType.Whisper:
                    ip = "group.tmi.twitch.tv";
                    break;
                default:
                    ip = "irc.chat.twitch.tv";
                    break;
            }

            return ip;
        }

        /// <summary>
        /// Connect to the IRC server
        /// </summary>
        public void Connect()
        {
            DebugBot.BlankLine();
            DebugBot.Notify("Connecting to the " + connection_type.ToString() + " server for \"" + user_name + "\"...");

            tcp_Client = new TcpClient(ip_address, port);

            //create the reader/wrtier to communicate with the irc
            reader = new StreamReader(tcp_Client.GetStream());
            writer = new StreamWriter(tcp_Client.GetStream());

            writer.AutoFlush = true;

            //log into the irc
            writer.WriteLine("PASS oauth:" + oauth_token);
            writer.WriteLine("NICK " + user_name);
            writer.WriteLine("USER " + user_name + " 8 * :" + user_name);

            //request to recieve notices and user information through the irc
            writer.WriteLine("CAP REQ :twitch.tv/tags");
            writer.WriteLine("CAP REQ :twitch.tv/membership");
            writer.WriteLine("CAP REQ :twitch.tv/commands");

            writer.Flush();
        }

        public void Disconnect()
        {
            DebugBot.Notify("Disconnecting from the " + connection_type.ToString() + " server for \"" + user_name + "\"...");

            tcp_Client.Close();

            reader.DiscardBufferedData();
            reader.Dispose();
            reader.Close();

            writer.Flush();
            reader.DiscardBufferedData();
            writer.Dispose();
            writer.Close();
        }

        public void Reconnect()
        {
            DebugBot.Notify("Reconnecting to the " + connection_type.ToString() + " server for \"" + user_name + "\"...");

            Disconnect();
            Connect();
        }

        /// <summary>
        /// Determines if the <see cref="TcpClient"/> is connected to the server
        /// </summary>
        /// <returns></returns>
        public bool isConnected()
        {
            return tcp_Client.Connected;
        }
    }
}
