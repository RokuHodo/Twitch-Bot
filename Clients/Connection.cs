using System;
using System.IO;
using System.Net.Sockets;

using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Chat;

namespace TwitchChatBot.Clients
{
    class Connection
    {
        private const int port = 6667;

        private string ip_address, user_name, user_token;

        private TcpClient tcp_Client;

        public StreamReader reader;
        public StreamWriter writer;

        ConnectionType connection;

        public Connection(ConnectionType connection, string user_name, string user_token)
        {
            this.user_name = user_name;
            this.user_token = user_token;
            this.connection = connection;

            ip_address = GetIP(connection);

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
            Console.WriteLine();
            Debug.Notify($"Connecting to the {connection.ToString()} server..." + Environment.NewLine);

            tcp_Client = new TcpClient(ip_address, port);

            //create the reader/wrtier to communicate with the irc
            reader = new StreamReader(tcp_Client.GetStream());
            writer = new StreamWriter(tcp_Client.GetStream());

            writer.AutoFlush = true;

            //log into the irc
            writer.WriteLine("PASS oauth:" + user_token);
            writer.WriteLine("NICK " + user_name);
            writer.WriteLine("USER " + user_name + " 8 * :" + user_name);

            //request to recieve notices and user information through the irc
            writer.WriteLine("CAP REQ :twitch.tv/tags");
            writer.WriteLine("CAP REQ :twitch.tv/membership");
            writer.WriteLine("CAP REQ :twitch.tv/commands");

            writer.Flush();
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
