using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web.Hosting;
using System.Xml.Linq;

namespace TrelloForeman
{
    public class TrelloForemanConfig
    {
        public static readonly TrelloForemanConfig Instance = new TrelloForemanConfig();
        private static readonly string SecretDocumentKey = "Secret";
        private readonly object lockedObject = new object();
        private readonly ObjectCache objectCache = MemoryCache.Default;
        private List<string> workers = new List<string>();

        private TrelloForemanConfig()
        {
        }

        public string ApplicationKey => this.SecretDocument.Root.Element("ApplicationKey").Value;

        public string UserToken => this.SecretDocument.Root.Element("UserToken").Value;

        public string ToDoListId => this.SecretDocument.Root.Element("ToDoList").Attribute("Id").Value;

        public string DingtalkWebhookUrl => this.SecretDocument.Root.Element("Dingtalk").Element("WebhookUrl").Value;

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

        public string FetchOneWorker()
        {
            string workerId;
            lock (this.lockedObject)
            {
                if (this.workers.Count == 0)
                {
                    this.CreateWorkers();
                }

                workerId = this.workers.First();

                this.workers.RemoveAt(0);

                File.WriteAllText(this.RemainingWorkersFilePath, string.Join("\r\n", this.workers));
            }

            return workerId;
        }

        private void CreateWorkers()
        {
            this.workers = File.ReadAllLines(this.RemainingWorkersFilePath).ToList();

            if (this.workers.Count == 0)
            {
                var workerIdList =
                    this.SecretDocument.Root.Element("Workers")
                        .Elements()
                        .Select(w => w.Attribute("Id").Value)
                        .ToArray();

                var numbers = Enumerable.Range(0, workerIdList.Length).ToList();

                var random = new Random(Guid.NewGuid().GetHashCode());
                while (numbers.Count > 0)
                {
                    var index = random.Next(numbers.Count);

                    this.workers.Add(workerIdList[numbers[index]]);

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