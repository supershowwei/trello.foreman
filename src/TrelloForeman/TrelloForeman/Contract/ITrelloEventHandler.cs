namespace TrelloForeman.Contract
{
    public interface ITrelloEventHandler
    {
        void Process(dynamic @event);
    }
}