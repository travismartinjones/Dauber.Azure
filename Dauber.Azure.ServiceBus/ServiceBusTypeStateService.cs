using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HighIronRanch.Azure.ServiceBus;
using LiteDB;

namespace Dauber.Azure.ServiceBus
{
    public interface IServiceBusStateDirectoryBuilder
    {
        string Build();
    }

    public class ServiceBusTypeStateService : IServiceBusTypeStateService
    {
        private readonly IServiceBusStateDirectoryBuilder _serviceBusStateDirectoryBuilder;
        private string _serviceBusFolder;        
        private string _dbFilename;
        private LiteDatabase _db;

        public ServiceBusTypeStateService(IServiceBusStateDirectoryBuilder serviceBusStateDirectoryBuilder)
        {
            _serviceBusStateDirectoryBuilder = serviceBusStateDirectoryBuilder;
            _serviceBusFolder = _serviceBusStateDirectoryBuilder.Build();
            Directory.CreateDirectory(_serviceBusFolder);
            _dbFilename = Path.Combine(_serviceBusFolder, "data.db");
            _db = new LiteDatabase(_dbFilename);
            var queues = _db.GetCollection<Queue>("queues");
            queues.EnsureIndex(x => x.Name);
            var topics = _db.GetCollection<Queue>("topics");
            topics.EnsureIndex(x => x.Name);
        }

        ~ServiceBusTypeStateService()
        {
            _db.Dispose();
        }

        public async Task<bool> GetIsQueueCreated(string name)
        {
            var collection = _db.GetCollection<Queue>("queues");
            return collection.Find(x => x.Name == name).Any();
        }

        public async Task OnQueueCreated(string name)
        {
            if (await GetIsQueueCreated(name)) return;
            var collection = _db.GetCollection<Queue>("queues");            
            collection.Insert(new Queue { Name = name });
        }

        public async Task<bool> GetIsTopicCreated(string name)
        {
            var collection = _db.GetCollection<Topic>("topics");
            return collection.Find(x => x.Name == name).Any();
        }

        public async Task OnTopicCreated(string name)
        {
            if (await GetIsTopicCreated(name)) return;
            var collection = _db.GetCollection<Topic>("topics");
            collection.Insert(new Topic { Name = name });
        }
    }
}
