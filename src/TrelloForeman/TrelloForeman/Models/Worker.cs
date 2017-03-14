namespace TrelloForeman.Models
{
    public class Worker
    {
        public Worker(string id, string cellphoneNumber)
        {
            this.Id = id;
            this.CellphoneNumber = cellphoneNumber;
        }

        public string CellphoneNumber { get; set; }

        public string Id { get; set; }
    }
}