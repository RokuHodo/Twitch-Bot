﻿using System;
using System.Collections.Generic;
using System.Linq;

using RestSharp;

using TwitchBot.Extensions;
using TwitchBot.Helpers;
using TwitchBot.Interfaces;
using TwitchBot.Json;
using TwitchBot.Models.TwitchAPI;

namespace TwitchBot.Clients
{
    class TwitchClient : ITwitchClient
    {
        readonly string twitch_api_url = "https://api.twitch.tv/kraken",
                        twitch_accept_header = "application/vnd.twitchtv.v3+json",
                        client_id;

        public readonly RestClient client;

        public TwitchClient(string _client_id)
        {
            client_id = _client_id;

            client = new RestClient(twitch_api_url);
            client.AddHandler("application/json", new CustomJsonDeserializer());
            client.AddDefaultHeader("Accept", twitch_accept_header);            
        }

        /// <summary>
        /// Gets a channel object of the specified channel.
        /// </summary>
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
        public StreamResult GetStream(string channel)
        {
            RestRequest request = Request("streams/{channel}", Method.GET);
            request.AddUrlSegment("channel", channel);

            IRestResponse<StreamResult> response = client.Execute<StreamResult>(request);

            return response.Data;
        }

        /// <summary>
        /// Gets the meta data between tweo channels if the user is following a channel.
        /// </summary>
        public FollowerRelationship GetFollowerRelationship(string user, string channel)
        {
            RestRequest request = Request("users/{user}/follows/channels/{channel}", Method.GET);
            request.AddUrlSegment("user", user);
            request.AddUrlSegment("channel", channel);

            IRestResponse<FollowerRelationship> response = client.Execute<FollowerRelationship>(request);

            return response.Data;
        }

        /// <summary>
        /// Gets how long a user has been following a channel in <see cref="DateTime"/> format.
        /// </summary>
        public DateTime GetHowlong(string user, string channel)
        {
            return GetFollowerRelationship(user, channel).created_at;
        }

        /// <summary>
        /// Gets how long a user has been following a channel and formats it into a displayable string,
        /// </summary>
        public string GetHowLong_String(string user, string channel)
        {
            string how_long_response = string.Empty;

            if (user.ToLower() == channel.ToLower())
            {
                how_long_response = "You cannot follow yourself " + channel + " FailFish";
            }
            else if (!isFollowing(user, channel))
            {
                how_long_response = user + " is not following " + channel;
            }            
            else
            {
                string follow_date = GetHowlong(user, channel).ToLocalTime().ToLongDateString();

                how_long_response = user + " has been following " + channel + " since " + follow_date + " PogChamp";
            }          

            return how_long_response;
        }

        /// <summary>
        /// Gets a paged result of follwers for a specific channel.
        /// </summary>
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

            return followers.Distinct();
        }

        /// <summary>
        /// Checks to see if a channel is live.
        /// </summary>
        public bool isLive(string channel)
        {
            return GetStream(channel).stream != null;
        }

        /// <summary>
        /// Checks to see if a channel is partnered.
        /// </summary>
        public bool isPartner(string channel)
        {
            return GetChannel(channel).partner;
        }

        /// <summary>
        /// Determines if a user is following a channel.
        /// </summary>
        public bool isFollowing(string user, string channel)
        {
            return GetFollowerRelationship(user, channel)._status != 404;
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
        /// Gets all of the new followers up until a certain <see cref="DateTime"/>.
        /// </summary>
        public IEnumerable<string> GetNewFollowers(string channel, ref DateTime newest_follower_updated_at, ref Trie followers_at_launch_trie)
        {                              
            bool searching = true;

            List<string> new_followers_list_string = new List<string>();
            List<Follower> new_followers_list_follower = new List<Follower>();

            Paging paging = new Paging();
            paging.limit = 100;            

            FollowerResult requested_page = GetFollowers_Page(channel, paging);
                        
            do
            {             
                //store the first follower page in a list
                foreach (Follower follower in requested_page.follows)
                {
                    //check to see if the follower date is earlier than the last follower date
                    if(DateTime.Compare(follower.created_at, newest_follower_updated_at) <= 0)
                    {
                        searching = false;

                        newest_follower_updated_at = follower.created_at;

                        //DebugBot.PrintLine(nameof(follower), follower.user.display_name);
                        //DebugBot.PrintLine(nameof(follower.user.updated_at), follower.user.updated_at.ToLocalTime().ToString());

                        break;
                    }

                    //try and add the user to the trie
                    if (followers_at_launch_trie.Insert(follower.user.display_name))
                    {
                        new_followers_list_string.Add(follower.user.display_name);
                        new_followers_list_follower.Add(follower);
                    }
                }

                if (searching)
                {
                    paging._cursor = requested_page._cursor;

                    requested_page = GetFollowers_Page(channel, paging);
                }                
            }
            while (searching);

            //make sure to set the "newest_follower_updated_at" to the first person that is added since it is requested in descending order
            if(new_followers_list_follower.Count > 0)
            {
                newest_follower_updated_at = new_followers_list_follower[0].created_at;
            }            
            
            return new_followers_list_string;
        }

        /// <summary>
        /// Send the request to the api
        /// </summary>
        public RestRequest Request(string url, Method method)
        {
            RestRequest request = new RestRequest(url, method);
            request.AddHeader("Client-ID", client_id);
            request.AddQueryParameter("noCache", DateTime.Now.Ticks.ToString());

            return request;
        }
    }
}
