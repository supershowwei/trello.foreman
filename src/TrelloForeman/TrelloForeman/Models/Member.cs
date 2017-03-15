namespace TrelloForeman.Models
{
    public class Member
    {
        public Member(string id, string cellphoneNumber)
        {
            this.Id = id;
            this.CellphoneNumber = cellphoneNumber;
        }

        public string CellphoneNumber { get; set; }

        public string Id { get; set; }
    }
}