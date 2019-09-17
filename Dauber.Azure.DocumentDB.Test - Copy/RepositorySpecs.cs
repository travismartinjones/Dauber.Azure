using System;
using System.Linq;
using System.Threading.Tasks;
using Dauber.Core;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.DocumentDb.Test
{
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
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
            }, factory, logger);

            var driver = (await db.GetAsync<Driver>(Guid.NewGuid()).ConfigureAwait(false));
            driver.ShouldBeNull();

            driver = (await db.GetAsync<Driver>().ConfigureAwait(false)).AsEnumerable().FirstOrDefault(x => x.Id == Guid.NewGuid());
            driver.ShouldBeNull();

            await db.DeleteAsync<Driver>(Guid.NewGuid()).ConfigureAwait(false);
        }
        
        [Test]
        public async Task LinqQueryableTestPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
            }, factory, logger);

            var drivers = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == new Guid("ae4afe26-28d0-4117-a12a-1dc6c02867cb")).ToList();
            drivers.Count.ShouldBe(4);
        }
        
        [Test]
        public async Task LinqQueryableTestUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger);

            var drivers = (await db.GetAsync<Driver>().ConfigureAwait(false)).Where(x => x.FleetId == new Guid("9653867a-a055-4ec2-a3f1-1bc6b8714537")).ToList();
            drivers.Count.ShouldBe(5);
        }
        
        [Test]
        public async Task GetByPartitionKeyOnOldDocumentPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
            }, factory, logger);
            
            var driver = (await db.GetAsync<Driver>(new Guid("748e572a-e0cf-4e93-9b1b-f2b11d3df0b9")).ConfigureAwait(false));
            driver.ShouldNotBeNull();
            driver.Name.ShouldBe("Aragorn His Bad Self");
        }
        
        [Test]
        public async Task GetByPartitionKeyOnOldDocumentUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);            
          
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger);
            
            var driver = (await db.GetAsync<Driver>(new Guid("fa01e57c-f80b-4f16-8752-6a77c432fe1c")).ConfigureAwait(false));
            driver.ShouldNotBeNull();
            driver.Name.ShouldBe("Rude Guy");
        }
        
        [Test]
        public async Task GetBySqlPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbReadModelRepository(new DatabaseSettings
            {
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/",
                IsPartitioned = true
            }, factory, logger);
            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var drivers = (await db.GetAsync<Driver>($"SELECT * FROM c WHERE c.DocType = '{nameof(Driver)}' AND c.FleetId = 'ae4afe26-28d0-4117-a12a-1dc6c02867cb'").ConfigureAwait(false)).ToList();
            drivers.Count.ShouldBe(4);
        }

        [Test]
        public async Task GetBySqlUnpartitioned()
        {   
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger);

            // 748e572a-e0cf-4e93-9b1b-f2b11d3df0b9
            var drivers = (await db.GetAsync<Driver>($"SELECT * FROM c WHERE c.DocType = '{nameof(Driver)}' AND c.FleetId = '9653867a-a055-4ec2-a3f1-1bc6b8714537'").ConfigureAwait(false)).ToList();
            drivers.Count.ShouldBe(5);
        }
        
        [Test]
        public async Task AllTheCrudPartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = true,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-container",
                DocumentDbRepositoryAuthKey = "fLMGnhzz8F7zjtajHOxUcUVN6sEcpYiEcJBJ73nd7WvdwCpJNiH89Loonu4hc0t2qhGv2HIVnvEHdu31d7kYjQ==",
                DocumentDbRepositoryEndpointUrl = "https://dauber-test.documents.azure.com:443/"
            }, factory, logger);
            
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
            db.Delete<Driver>(driverId);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();

            // add the driver via upsert and verify it added
            await db.UpsertAsync(driver).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // cleanup
            db.Delete<Driver>(driverId);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();
        }
        
        [Test]
        public async Task AllTheCrudUnpartitioned()
        {            
            var logger = A.Fake<ILogger>();
            var factory = new ReliableReadWriteDocumentClientFactory(logger);                        
            var db = new DocumentDbWritableReadModelRepository(new DatabaseSettings
            {
                IsPartitioned = false,
                DocumentDbRepositoryDatabaseId = "dauber-fleet-database",
                DocumentDbRepositoryCollectionId = "dauber-fleet-collection",
                DocumentDbRepositoryAuthKey = "ewhSCsOUv2QzHeu3aCl1X6uAxQPMu11r1G4TlruWTg3wuaUf0pVybv4PGx0ZS7RMVS4vDBxE5TlQwWFYYOUTkA==",
                DocumentDbRepositoryEndpointUrl = "https://dauber.documents.azure.com:443/"
            }, factory, logger);

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
            db.Delete<Driver>(driverId);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();

            // add the driver via upsert and verify it added
            await db.UpsertAsync(driver).ConfigureAwait(false);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldNotBeNull();
            get.Name.ShouldBe("Joe Test");

            // cleanup
            db.Delete<Driver>(driverId);
            get = await db.GetAsync<Driver>(driverId).ConfigureAwait(false);
            get.ShouldBeNull();
        }


    }
}