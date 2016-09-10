using RestSharp;

namespace TwitchBot.Helpers
{   
    class Paging
    {
        public int limit = 25,
                   offset = 0;

        public string _cursor = string.Empty,
                      direction = "DESC";

        public Paging()
        {

        }

        public Paging(int _limit, int _offset, string __cursor, string _direction)
        {
            limit = _limit;
            offset = _offset;

            _cursor = __cursor;
            direction = _direction;
        }

        public RestRequest AddPaging(RestRequest request)
        {
            request.AddParameter("limit", limit);
            request.AddParameter("offset", offset);
            request.AddParameter("cursor", _cursor);
            request.AddParameter("direction", direction);

            return request;
        }
    }
}
