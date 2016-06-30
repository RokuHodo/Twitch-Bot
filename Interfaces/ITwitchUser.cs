using RestSharp;

namespace TwitchChatBot.Interfaces
{
    interface ITwitchClient
    {
        RestRequest Request(string url, Method method);
    }
}
