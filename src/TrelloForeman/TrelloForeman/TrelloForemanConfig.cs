using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web.Hosting;
using System.Xml.Linq;
using TrelloForeman.Models;

namespace TrelloForeman
{
    public class TrelloForemanConfig
    {
        public static readonly TrelloForemanConfig Instance = new TrelloForemanConfig();

        private static readonly string SecretDocumentKey = "Secret";

        private readonly object lockedObject = new object();

        private readonly ObjectCache objectCache = MemoryCache.Default;

        private List<Member> workers = new List<Member>();

        private TrelloForemanConfig()
        {
        }

        public string ApplicationKey => this.SecretDocument.Root.Element("ApplicationKey").Value;

        public string DingtalkWebhookUrl => this.SecretDocument.Root.Element("Dingtalk").Element("WebhookUrl").Value;

        public string ListenerUrl => this.SecretDocument.Root.Element("ListenerUrl").Value;

        public string UserToken => this.SecretDocument.Root.Element("UserToken").Value;

        public string BacklogListId => this.SecretDocument.Root.Element("BacklogList").Attribute("Id").Value;

        public string VacationListId => this.SecretDocument.Root.Element("VacationList").Attribute("Id").Value;

        public string ToVerifyListId => this.SecretDocument.Root.Element("ToVerifyList").Attribute("Id").Value;

        private string RemainingWorkersFilePath
            => Path.Combine(HostingEnvironment.MapPath("~/App_Data"), "remaining_workers");

        private XDocument SecretDocument
        {
            get
            {
                if (this.objectCache[SecretDocumentKey] == null)
                {
                    this.LoadSecretDocument();
                }

                return this.objectCache[SecretDocumentKey] as XDocument;
            }
        }

        public string FetchMemberCellphoneNumber(string memberId)
        {
            return
                this.SecretDocument.Root.Element("Members")
                    .Elements()
                    .Single(m => m.Attribute("Id").Value.Equals(memberId))
                    .Attribute("CellphoneNumber")
                    .Value;
        }

        public Member FetchOneWorker()
        {
            Member worker;
            lock (this.lockedObject)
            {
                if (this.workers.Count == 0)
                {
                    this.CreateWorkers();
                }

                worker = this.workers.First();

                this.workers.RemoveAt(0);

                File.WriteAllText(
                    this.RemainingWorkersFilePath,
                    string.Join("\r\n", this.workers.Select(w => $"{w.Id},{w.CellphoneNumber}")));
            }

            return worker;
        }

        private void CreateWorkers()
        {
            this.workers = File.ReadAllLines(this.RemainingWorkersFilePath).Select(
                line =>
                    {
                        var workerRawArray = line.Split(',');

                        return new Member(workerRawArray[0], workerRawArray[1]);
                    }).ToList();

            if (this.workers.Count == 0)
            {
                var workerRoster =
                    this.SecretDocument.Root.Element("Members")
                        .Elements("Worker")
                        .Select(w => new Member(w.Attribute("Id").Value, w.Attribute("CellphoneNumber").Value))
                        .ToArray();

                var numbers = Enumerable.Range(0, workerRoster.Length).ToList();

                var random = new Random(Guid.NewGuid().GetHashCode());
                while (numbers.Count > 0)
                {
                    var index = random.Next(numbers.Count);

                    this.workers.Add(workerRoster[numbers[index]]);

                    numbers.RemoveAt(index);
                }
            }
        }

        private void LoadSecretDocument()
        {
            var file = Path.Combine(HostingEnvironment.MapPath("~/App_Data"), "secret.xml");

            var cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(new[] { file }));

            this.objectCache.Set(SecretDocumentKey, XDocument.Load(file), cacheItemPolicy);
        }
    }
}