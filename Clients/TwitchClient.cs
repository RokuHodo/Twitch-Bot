using System;

using RestSharp;

using TwitchChatBot.Interfaces;
using TwitchChatBot.Json;
using TwitchChatBot.Models.TwitchAPI;

namespace TwitchChatBot.Clients
{
    class TwitchClient : ITwitchClient
    {
        private readonly string twitch_api_url = "https://api.twitch.tv/kraken",
                                twitch_accept_header = "application/vnd.twitchtv.v3+json";

        public readonly RestClient client;

        public TwitchClient()
        {
            client = new RestClient(twitch_api_url);
            client.AddHandler("application/json", new CustomJsonDeserializer());
            client.AddDefaultHeader("Accept", twitch_accept_header);            
        }

        /// <summary>
        /// Gets a channel object of the specified channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public Channel GetChannel(string channel)
        {
            RestRequest request = Request($"channels/{channel}", Method.GET);
            request.AddUrlSegment("channel", channel);

            IRestResponse<Channel> response = client.Execute<Channel>(request);

            return response.Data;
        }

        /// <summary>
        /// Gets a stream object of the specified channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public StreamResult GetStream(string channel)
        {
            RestRequest request = Request($"streams/{channel}", Method.GET);
            request.AddUrlSegment("channel", channel);

            IRestResponse<StreamResult> response = client.Execute<StreamResult>(request);

            return response.Data;
        }

        /// <summary>
        /// Checks to see if a channel is live.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public bool isLive(string channel)
        {
            return GetStream(channel).stream != null;
        }

        /// <summary>
        /// Checks to see if a channel is partnered.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public bool isPartner(string channel)
        {
            return GetChannel(channel).partner;
        }

        /// <summary>
        /// Gets the uptime of a channel in <see cref="DateTime"/> format.
        /// Returns <see cref="TimeSpan.Zero"/> if the channel is offline.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public TimeSpan GetUpTime(string channel)
        {
            TimeSpan up_time = TimeSpan.Zero;

            if (isLive(channel))
            {
                DateTime stream_start = GetStream(channel).stream.created_at;

                up_time = DateTime.Now.Subtract(stream_start.ToLocalTime());
            }

            return up_time;
        }        

        /// <summary>
        /// Send the request to the api
        /// </summary>
        /// <param name="url">Twitch api url.</param>
        /// <param name="method">Operation that is being performed.</param>
        /// <returns></returns>
        public RestRequest Request(string url, Method method)
        {
            return new RestRequest(url, method);
        }
    }
}
