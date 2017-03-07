using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Xml.Linq;

namespace TrelloForeman
{
    public class TrelloForemanConfig
    {
        public static readonly TrelloForemanConfig Instance = new TrelloForemanConfig();
        private static readonly string SecretDocumentKey = "Secret";
        private readonly ObjectCache objectCache = MemoryCache.Default;
        private readonly ConcurrentQueue<string> workerIdQueue = new ConcurrentQueue<string>();

        private TrelloForemanConfig()
        {
        }

        public string ApplicationKey => this.SecretDocument.Root.Element("ApplicationKey").Value;

        public string UserToken => this.SecretDocument.Root.Element("UserToken").Value;

        public string ToDoListId => this.SecretDocument.Root.Element("ToDoList").Attribute("Id").Value;

        public string DingtalkWebhookUrl => this.SecretDocument.Root.Element("Dingtalk").Element("WebhookUrl").Value;

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

        private string[] WorkerIdList
            => this.SecretDocument.Root.Element("Workers").Elements().Select(w => w.Attribute("Id").Value).ToArray();

        public string FetchOneWorker()
        {
            if (this.workerIdQueue.Count == 0)
            {
                var workerIdList = this.WorkerIdList;

                var procession = Enumerable.Range(0, workerIdList.Length).ToList();

                var random = new Random(Guid.NewGuid().GetHashCode());
                while (procession.Count > 0)
                {
                    var index = random.Next(procession.Count);

                    this.workerIdQueue.Enqueue(workerIdList[procession[index]]);

                    procession.RemoveAt(index);
                }
            }

            string workerId;
            while (!this.workerIdQueue.TryDequeue(out workerId))
            {
                // 一定要取到值
            }

            return workerId;
        }

        private void LoadSecretDocument()
        {
            var file = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data"), "secret.xml");

            var cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(new[] { file }));

            this.objectCache.Set(SecretDocumentKey, XDocument.Load(file), cacheItemPolicy);
        }
    }
}