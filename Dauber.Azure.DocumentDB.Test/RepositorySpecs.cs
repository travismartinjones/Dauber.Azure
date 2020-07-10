using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using Dauber.Core.Contracts;
using FakeItEasy;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.DocumentDb.Test
{
    public class Job : ViewModel
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
        }
    }

    [TestFixture]
    public partial class RepositorySpecs
    {
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
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
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
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
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
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
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
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
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
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger, telemetryLogger);

            var drivers = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == new Guid("9653867a-a055-4ec2-a3f1-1bc6b8714537")).ToList();
            drivers.Count.ShouldBe(5);
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
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
            }, factory, logger, telemetryLogger);
            
            var driver = (await db.GetAsync<Driver>(new Guid("748e572a-e0cf-4e93-9b1b-f2b11d3df0b9")).ConfigureAwait(false));
            driver.ShouldNotBeNull();
            driver.Name.ShouldBe("Aragorn His Bad Self");
        }
        
        [Test]
        [Ignore("Disabled")]
        public async Task GetByPartitionKeyOnOldDocumentUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
          
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger, telemetryLogger);
            
            var driver = (await db.GetAsync<Driver>(new Guid("fa01e57c-f80b-4f16-8752-6a77c432fe1c")).ConfigureAwait(false));
            driver.ShouldNotBeNull();
            driver.Name.ShouldBe("Rude Guy");
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
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/",
                IsPartitioned = true
            }, factory, logger, telemetryLogger);
            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var drivers = (await db.GetAsync<Driver>($"SELECT * FROM c WHERE c.DocType = '{nameof(Driver)}' AND c.FleetId = 'ae4afe26-28d0-4117-a12a-1dc6c02867cb'").ConfigureAwait(false)).ToList();
            drivers.Count.ShouldBe(4);
        }

        [Test]
        [Ignore("Disabled")]
        public async Task GetBySqlUnpartitioned()
        {   
            var logger = A.Fake<ILogger>();
            var telemetryLogger = A.Fake<ITelemetryLogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger, telemetryLogger);

            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var drivers = (await db.GetAsync<Driver>($"SELECT * FROM c WHERE c.DocType = '{nameof(Driver)}' AND c.FleetId = '9653867a-a055-4ec2-a3f1-1bc6b8714537'").ConfigureAwait(false)).ToList();
            drivers.Count.ShouldBe(5);
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
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
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
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
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