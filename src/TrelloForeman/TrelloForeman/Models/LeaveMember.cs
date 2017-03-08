using System;

namespace TrelloForeman.Models
{
    public class LeaveMember
    {
        public LeaveMember(string id, DateTime? dueDate)
        {
            this.Id = id;
            this.DueDate = dueDate ?? DateTime.MinValue;
        }

        public string Id { get; set; }

        public DateTime DueDate { get; set; }
    }
}