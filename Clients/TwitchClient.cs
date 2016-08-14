using System;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;

using RestSharp;

using TwitchChatBot.Extensions;
using TwitchChatBot.Helpers;
using TwitchChatBot.Interfaces;
using TwitchChatBot.Json;
using TwitchChatBot.Models.TwitchAPI;

namespace TwitchChatBot.Clients
{
    class TwitchClient : ITwitchClient
    {
        readonly string twitch_api_url = "https://api.twitch.tv/kraken",
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
            RestRequest request = Request("channels/{channel}", Method.GET);
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
            RestRequest request = Request("streams/{channel}", Method.GET);
            request.AddUrlSegment("channel", channel);

            IRestResponse<StreamResult> response = client.Execute<StreamResult>(request);

            return response.Data;
        }

        /// <summary>
        /// Gets a paged result of follwers for a specific channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public FollowerResult GetFollowers_Page(string channel, Paging paging = default(Paging))
        {
            RestRequest request = Request("channels/{channel}/follows", Method.GET);
            request.AddUrlSegment("channel", channel);
            request = paging.AddPaging(request);

            IRestResponse<FollowerResult> response = client.Execute<FollowerResult>(request);

            return response.Data;
        }

        /// <summary>
        /// Returns a complete list of all followers of a stream.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        /// <returns></returns>
        public IEnumerable<Follower> GetFollowers_All(string channel)
        {
            List<Follower> followers = new List<Follower>();

            Paging paging = new Paging();
            paging.limit = 100;

            FollowerResult follower_page = GetFollowers_Page(channel, paging);

            do
            {
                foreach(Follower follower in follower_page.follows)
                {
                    followers.Add(follower);
                }                

                if (follower_page._cursor.CheckString())
                {
                    paging._cursor = follower_page._cursor;

                    follower_page = GetFollowers_Page(channel, paging);
                }                
            }
            while (follower_page._cursor.CheckString());

            //only return a list of distinct followers in case some ass hat decides to unfollow and follow during this time
            return followers.Distinct();
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

        /*
        /// <summary>
        /// Returns a list of users who have followed when compared against an older list of followers
        /// </summary>
        /// <param name="channel">Name of the channel to get the list of followers for.</param>
        /// <param name="follower_list">The old list of followers to cmopare against.</param>
        /// <param name="followers">The new list of followers that is requested and referenced.</param>
        /// <returns></returns>
        public IEnumerable<string> GetNewFollowers(string channel, List<Follower> follower_list, out Follower[] followers)
        {
            followers = GetFollowers_All(channel).ToArray();

            string[] display_names_old = new string[follower_list.Count],
                     display_names_current = new string[followers.Length];

            for (int index = 0; index < follower_list.Count; index++)
            {
                display_names_old[index] = follower_list[index].user.display_name;
            }

            for (int index = 0; index < display_names_current.Length; index++)
            {
                display_names_current[index] = followers[index].user.display_name;
            }

            return display_names_current.Except(display_names_old);
        }
        */

        public IEnumerable<string> GetNewFollowers(string channel, List<string> followers_at_launch, ref List<string> followers_added)
        {            
            List<string> comparison_page_list = new List<string>();
            List<string> requested_pages_all_list = new List<string>();                        

            bool searching = true;

            string[] overlap,
                     comparison_page_new_array,
                     comparison_page_old_array = followers_added.Count > 0 ? followers_added.ToArray() : followers_at_launch.ToArray();

            Paging paging = new Paging();
            paging.limit = 100;

            FollowerResult requested_page = GetFollowers_Page(channel, paging);

            do
            {             
                //store the first follower page in a list
                foreach (Follower follower in requested_page.follows)
                {
                    comparison_page_list.Add(follower.user.display_name);
                    requested_pages_all_list.Add(follower.user.display_name);                    
                }

                //see if there are any users that overlap between the old and new arrays
                comparison_page_new_array = comparison_page_list.ToArray();                
                overlap = comparison_page_new_array.Intersect(comparison_page_old_array).ToArray();

                //an old follower was found on both arrays
                //anyting below the old user is also following, all new users have been found, stop searching
                if (overlap.Length > 0)
                {
                    searching = false;
                }
                //no old followers were are shared between the old and new arrays
                //search the next page
                else
                {
                    //we only want to compare against MAX(100) followers at a time to keep things efficient instead of starting at the begining every time
                    comparison_page_list.Clear();

                    paging._cursor = requested_page._cursor;

                    requested_page = GetFollowers_Page(channel, paging);
                }
            }
            while (searching);
                        
            //this will work fine for now, but will probably blow up if the arrays get huge...
            followers_added.AddRange(requested_pages_all_list);
            followers_added = followers_added.Distinct().ToList();

            return requested_pages_all_list.ToArray().Except(comparison_page_old_array).Distinct();
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
