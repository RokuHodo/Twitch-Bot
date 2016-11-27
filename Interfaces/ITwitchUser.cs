using RestSharp;

namespace TwitchBot.Interfaces
{
    interface ITwitchClient
    {
        RestRequest Request(string url, Method method);
    }
}
