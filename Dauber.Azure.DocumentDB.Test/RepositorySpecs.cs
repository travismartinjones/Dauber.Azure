using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Container;
using Dauber.Core.Contracts;
using FakeItEasy;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.DocumentDb.Test
{
    public class Job : ViewModel
    {
    }

    public class JobLoad : ViewModel
    {

    }

    public class TicketJobProjection
    {
        public Guid JobId { get; set; }
        public Guid OrderId { get; set; }
        public bool IsTicketPhotoRequired { get; set; }        
        public Guid? OriginationFleetId { get; set; }     
        public string CustomerName { get; set; }
        public string ProviderName { get; set; }
        public string ProviderCustomerName { get; set; }
        public string ProviderReferenceNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string OrderReferenceNumber { get; set; }
        public string MaterialDescription { get; set; }
        public int TransitToPickupSeconds { get; set; }
        public int TransitToDropOffSeconds { get; set; }
        public bool IsTicketInformationRequired { get; set; }
        public bool IsDeliverySignatureRequired { get; set; }
        public bool IsPickupSignatureRequired { get; set; }       
        public List<string> PickupSignators { get; set; }
        public List<string> DeliverySignators { get; set; }
        public string PickupNotes { get; set; }
        public string DropOffNotes { get; set; }
        public double DeliveredQuantity { get; set; }
        public string PurchaseOrderNumber { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public int HaulType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public int UnitType { get; set; }
        public int Status { get; set; }
        public double? TransitToDropOffMeters { get; set; }
        public List<Guid> WhitelistedFleets { get; set; }
    }

    public partial class RepositorySpecs
    {
        public class DatabaseSettings : IDocumentDbSettings
        {
            public string DocumentDbRepositoryEndpointUrl { get; set; }
            public string DocumentDbRepositoryAuthKey { get; set; }
            public string DocumentDbRepositoryDatabaseId { get; set; }
            public string DocumentDbRepositoryCollectionId { get; set; }
            public bool IsPartitioned { get; set; }
            public CosmosClientOptions CosmosClientOptions { get; } = new CosmosClientOptions();
        }
    }

    [TestFixture]
    public partial class RepositorySpecs
    {
        private static string AuthKey = "";
        private static string EntryPointUrl = "https://dauber-test.documents.azure.com:443/";

        [Test]
        public async Task NotFoundPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var driver = (await db.GetAsync<Driver>(Guid.NewGuid()).ConfigureAwait(false));
            driver.ShouldBeNull();

            driver = (await db.GetAsync<Driver>().ConfigureAwait(false)).AsEnumerable().FirstOrDefault(x => x.Id == Guid.NewGuid());
            driver.ShouldBeNull();

            await db.DeleteAsync<Driver>(new [] {Guid.NewGuid()}).ConfigureAwait(false);
        }

        [Test]
        public async Task SlowInFleet()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var jobProjections = (await db.GetAsync<Job, Job>($@"
SELECT j.id
FROM j
WHERE j.DocType = 'Truck' AND '45a0e505-b905-476c-8e44-2b08ec8fbc9a' IN (
j.FleetAssignments[0].FleetId,
j.FleetAssignments[1].FleetId,
j.FleetAssignments[2].FleetId,
j.FleetAssignments[3].FleetId,
j.FleetAssignments[4].FleetId,
j.FleetAssignments[5].FleetId,
j.FleetAssignments[6].FleetId,
j.FleetAssignments[7].FleetId,
j.FleetAssignments[8].FleetId,
j.FleetAssignments[9].FleetId)
").ConfigureAwait(false)).ToList();
            //var job = (await db.GetAsync<Job>(new Guid("e748aab1-2613-4baa-a468-c1e6f0573155")).ConfigureAwait(false));
        }
        
        [Test]
        public async Task SlowInSite()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-site-database",
                DocumentDbRepositoryCollectionId = "dauber-site-collection",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var jobProjections = (await db.GetAsync<Job, Job>($@"
SELECT * FROM c 
WHERE  c.DocType = 'SiteOperator' AND 'PMR' IN (c.NameMeta[0],c.NameMeta[1],c.NameMeta[2],c.NameMeta[3],c.NameMeta[4],c.NameMeta[5],c.NameMeta[6],c.NameMeta[7],c.NameMeta[8],c.NameMeta[9],c.NameMeta[10],c.NameMeta[11])
").ConfigureAwait(false)).ToList();
            //var job1 = (await db.GetAsync<Job>(new Guid("dd50d499-23a8-44b6-b3fd-7722476d9f1d")).ConfigureAwait(false));
            //var job2 = (await db.GetAsync<Job>(new Guid("00000000-0000-0000-0000-000000000000")).ConfigureAwait(false));
        }
        
        [Test]
        public async Task LinqQueryableTestPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var drivers = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == new Guid("ae4afe26-28d0-4117-a12a-1dc6c02867cb")).ToList();
            drivers.Count.ShouldBe(4);
        }
        
        [Test]
        [Ignore("Disabled")]
        public async Task LinqQueryableTestUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var drivers = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == new Guid("9653867a-a055-4ec2-a3f1-1bc6b8714537")).ToList();
            drivers.Count.ShouldBe(5);
        }


        [Test]        
        public async Task JobLoadIterationVersusContains()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "container",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/",
                CosmosClientOptions =
                {
                    ConnectionMode = ConnectionMode.Direct,
                    EnableTcpConnectionEndpointRediscovery = false,
                    GatewayModeMaxConnectionLimit = 50,
                    MaxTcpConnectionsPerEndpoint = null,
                    MaxRequestsPerTcpConnection = null,
                    PortReuseMode = PortReuseMode.ReuseUnicastPort,
                    IdleTcpConnectionTimeout = (TimeSpan?)null
                }
            }, factory, logger, telemetryLogger);

            var stopwatch = new Stopwatch();

            var ids = new List<Guid>
            {
                new Guid("e0e1c684-64b3-424c-8d46-f56e13980870"),
                new Guid("c1b8b653-613e-4778-b664-96e7c4fb2bd5"),
                new Guid("e06bd3f0-6bed-4946-934e-fe124668930a"),
                new Guid("de784287-8e48-4d55-981c-5973f9473d5f"),
                new Guid("7ca66433-55c5-489a-9316-da21cd2a9257"),
                new Guid("be9aca58-8d1d-464b-9410-b8830891769e"),
                new Guid("cc566ed6-a16d-4ad3-8933-c5a45770baf1"),
                new Guid("a3d24d55-00a0-45f0-a0c1-01dee1cc8425"),
                new Guid("f9b6e2de-ba1e-4fef-a773-de365c24dda3"),
                new Guid("8ea4a303-aa9f-4647-aae8-c2e3bf33c03e"),
                new Guid("8f6058e9-b9df-4537-8b6f-5d7472416368"),
                new Guid("7866ee96-1f41-46b7-a5f2-e65ba54590cd"),
                new Guid("4a3182ac-f698-422b-a427-d241b2fce3ea"),
                new Guid("f269b762-7e22-4c64-89f5-9f9c36e47da5"),
                new Guid("666ad3b7-079e-4bdb-9bbf-97a3e405aac2"),
                new Guid("08329e12-80c5-4bfe-812a-5b4e1f48c26b"),
                new Guid("4cf3fb31-26ec-4068-974b-a184d917c0b8"),
                new Guid("1f6d1279-7fec-47f6-a4e0-629c9f0f410c"),
                new Guid("16ce5edd-ab99-4bb9-ab39-b412ee085bdc"),
                new Guid("da61fee2-53ec-4f6b-82aa-311129799aa4"),
                new Guid("48e84062-1abc-4f21-be93-107653529c9d"),
                new Guid("7064a669-68f5-40b2-8b68-aade284dcd6c"),
                new Guid("0253b8ea-b735-4c25-8c14-5387ff567898"),
                new Guid("f7903f1b-bcbb-4a06-a687-6d40b2af5c31"),
                new Guid("a05118d3-031e-485a-a4ad-1d79c837584b"),
                new Guid("5c372023-05ef-4431-b722-a11278d532f9"),
                new Guid("651168f1-0ab4-4fd9-a8ec-b0a3c7cce722"),
                new Guid("3be82c69-557d-43ee-bead-b72bf1997d79"),
                new Guid("5bed48e5-1fe9-40be-8c14-48403330c67c"),
                new Guid("0cf41d22-c59e-446a-b772-b62d473f00ba"),
                new Guid("34cf41b9-8bf1-42b8-949c-d896ced91904"),
                new Guid("1def455e-ff9b-401f-bf71-e8db9941e19b"),
                new Guid("dacfa8da-0599-41a9-8e78-5cefb0b97c08"),
                new Guid("3f4e6458-8f36-493a-86f2-f1faa1903bc0"),
                new Guid("6b6bc029-2432-478b-8ac1-43adf405f8c9"),
                new Guid("c1361a04-0d7c-4a82-8cde-7237278c4e2e"),
                new Guid("f5f6244f-8f5c-4d5c-86a0-372e829d890b"),
                new Guid("4b4418f0-2c61-43c8-9059-c5319cde5d27"),
                new Guid("0d7d5bfa-ebe6-451a-8426-4d226cc6f72b")
            };

            var loadsIteration = new List<JobLoad>();

            stopwatch.Start();

            foreach (var id in ids)
            {
                var loads = await (await db.GetAsync<JobLoad>().ConfigureAwait(false)).Where(x => x.Id == id).QueryToList().ConfigureAwait(false);
                loadsIteration.AddRange(loads);
            }

            stopwatch.Stop();
            var iterationMs = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();

            var loadsContains = await (await db.GetAsync<JobLoad>().ConfigureAwait(false)).Where(x => ids.Contains(x.Id)).QueryToList().ConfigureAwait(false);
            stopwatch.Stop();
            var containsMs = stopwatch.ElapsedMilliseconds;

            containsMs.ShouldBeLessThan(iterationMs);
        }
        
        [Test]
        public async Task GetByPartitionKeyOnOldDocumentPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);
            
            var driver = (await db.GetAsync<Driver>(new Guid("748e572a-e0cf-4e93-9b1b-f2b11d3df0b9")).ConfigureAwait(false));
            driver.ShouldNotBeNull();
            driver.Name.ShouldBe("Aragorn His Bad Self");
        }
        
        
        [Test]
        public async Task GetBySqlPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl,
                IsPartitioned = true
            }, factory, logger, telemetryLogger);
            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var drivers = (await db.GetAsync<Driver>($"SELECT * FROM c WHERE c.DocType = '{nameof(Driver)}' AND c.FleetId = 'ae4afe26-28d0-4117-a12a-1dc6c02867cb'").ConfigureAwait(false)).ToList();
            drivers.Count.ShouldBe(4);
        }

        [Test]
        public async Task UpsertExistingDocumentByIdOnly()
        {   
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            var driverId = Guid.NewGuid();
            var fleetId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                DriversLicense = new DriversLicense {Id = "123456789", ExpirationDate = DateTime.UtcNow},
                Email = "test@test.com",
                FleetId = fleetId,
                ImageUrl = "test",
                Name = Guid.NewGuid().ToString(),
                ProfileImageUrl = "test",
                PhoneNumber = "+15555555555"
            };

            await db.InsertAsync(driver).ConfigureAwait(false);
            var result = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            result.ShouldNotBeNull();

            var driverReplacement = (await db.GetUpsertable<Driver>($"c.Name = '{driver.Name}'").ConfigureAwait(false));
            driverReplacement.DriversLicense = new DriversLicense {Id = "123456789", ExpirationDate = DateTime.UtcNow};
            driverReplacement.Email = "replaced@test.com";
            driverReplacement.FleetId = fleetId;
            driverReplacement.ImageUrl = "test";
            driverReplacement.Name = "Joe Test";
            driverReplacement.ProfileImageUrl = "test";
            driverReplacement.PhoneNumber = "+15555555555";
            await db.UpsertAsync(driverReplacement).ConfigureAwait(false);
            result = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            result.Email.ShouldBe(driverReplacement.Email);
            await db.DeleteAsync<Driver>(new[] {driverId}).ConfigureAwait(false);
            result = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            result.ShouldBeNull();
        }
        
        [Test]
        public async Task AllTheCrudPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);
            
            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var driverId = Guid.NewGuid();
            var fleetId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                DriversLicense = new DriversLicense {Id = "123456789", ExpirationDate = DateTime.UtcNow},
                Email = "test@test.com",
                FleetId = fleetId,
                ImageUrl = "test",
                Name = "Joe Test",
                ProfileImageUrl = "test",
                PhoneNumber = "+15555555555"
            };

            // insert the driver and verify the properties
            var response = await db.InsertAsync(driver).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            var get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // update the driver and verify the change
            get.Name = "Joe Test2";
            response = await db.UpdateAsync(get).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.Name.ShouldBe("Joe Test2");

            // upsert the driver and verify the change
            get.Name = "Joe Test3";
            get.MostRecentJobLoadId = Guid.Empty;
            response = await db.UpsertAsync(get).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.Name.ShouldBe("Joe Test3");

            get = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.MostRecentJobLoadId.HasValue).AsQueryable().ToList().FirstOrDefault();
            get.ShouldNotBeNull();

            // get the driver by a linq statement
            var where = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == fleetId).ToList();
            where.Count.ShouldBe(1);
            where[0].Name.ShouldBe("Joe Test3");


            // delete the driver and verify it is gone
            db.Delete<Driver>(new [] {driverId});
            get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.ShouldBeNull();

            // add the driver via upsert and verify it added
            response = await db.UpsertAsync(driver).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // cleanup
            response = db.Delete<Driver>(new [] {driverId});
            get = await db.GetAsync<Driver>(driverId, response.SessionToken).ConfigureAwait(false);
            get.ShouldBeNull();
        }
        
        [Test]
        [Ignore("Disabled")]
        public async Task AllTheCrudUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);                        
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = AuthKey,
                DocumentDbRepositoryEndpointUrl = EntryPointUrl
            }, factory, logger, telemetryLogger);

            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var driverId = Guid.NewGuid();
            var fleetId = Guid.NewGuid();
            var driver = new Driver
            {
                Id = driverId,
                DriversLicense = new DriversLicense {Id = "123456789", ExpirationDate = DateTime.UtcNow},
                Email = "test@test.com",
                FleetId = fleetId,
                ImageUrl = "test",
                Name = "Joe Test",
                ProfileImageUrl = "test",
                PhoneNumber = "+15555555555"
            };

            // insert the driver and verify the properties
            await db.InsertAsync(driver).ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            var get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // update the driver and verify the change
            get.Name = "Joe Test2";
            await db.UpdateAsync(get).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.Name.ShouldBe("Joe Test2");

            // upsert the driver and verify the change
            get.Name = "Joe Test3";
            await db.UpsertAsync(get).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.Name.ShouldBe("Joe Test3");

            // get the driver by a linq statement
            var where = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == fleetId).ToList();
            where.Count.ShouldBe(1);
            where[0].Name.ShouldBe("Joe Test3");

            // delete the driver and verify it is gone
            db.Delete<Driver>(new [] {driverId});
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();

            // add the driver via upsert and verify it added
            await db.UpsertAsync(driver).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // cleanup
            db.Delete<Driver>(new [] {driverId});
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();
        }


    }
}