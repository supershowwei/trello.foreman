using TrelloForeman.Contract;

namespace TrelloForeman.Logic
{
    public class NullTrelloEventHandler : ITrelloEventHandler
    {
        public void Process(dynamic @event)
        {
        }
    }
}