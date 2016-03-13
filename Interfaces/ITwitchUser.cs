using RestSharp;

namespace TwitchChatBot.Interfaces
{
    interface ITwitchUser
    {
        RestRequest Request(string url, Method method);
    }
}
