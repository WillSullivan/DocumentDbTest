using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;

namespace DocumentDBTest
{
    class Program
    {
        // the following two files contain the url and api keys for the DocumentDb instance to use in this application
        // both files are included in .gitignore, so they won't appear in github.  Create them yourself.
        private static string endpointUrl = System.IO.File.ReadAllText("endpoint.url");
        private static string authorizationKey = System.IO.File.ReadAllText("api.key");

        static void Main(string[] args)
        {
            var client = new DocumentClient(new Uri(endpointUrl), authorizationKey);

            var database = client.GetOrCreateDatabaseAsync("TestDatabase").Result;
            var documentCollection = client.GetOrCreateDocumentCollectionAsync(database, "testCollection").Result;

            var ssp = client.CreateDocumentAsync(documentCollection, new Location { Name = "State Street Pub", Lat = 123, Long = 456 }).Result;
            var home = client.CreateDocumentAsync(documentCollection, new Location { Name = "Home", Lat = 111, Long = 222 }).Result;
            var work = client.CreateDocumentAsync(documentCollection, new Location { Name = "work", Lat = 333, Long = 444 }).Result;
            Console.WriteLine("done!");
            Console.Read();
        }

        private static async Task<Document> Create(DocumentClient client, string collectionName, string name, double lat, double lng)
        {
            return await client.CreateDocumentAsync(collectionName, new Location
            {
                Name = name,
                Lat = lat,
                Long = lng,
            });
        }
    }

    public sealed class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
    }

    public static class DocumentClientExtensions
    {
        public static async Task<Database> GetOrCreateDatabaseAsync(this DocumentClient client, string id)
        {
            if(client == null)
                throw new ArgumentNullException("client");
            if(string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null, empty, or consist of whitespace.");
            // query must evaluate to an enumerable... meh.
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == id).ToArray().FirstOrDefault();
            if(database == null)
            {
                database = await client.CreateDatabaseAsync(new Database { Id = id });
            }

            return database;
        }
        public static Task<DocumentCollection> GetOrCreateDocumentCollectionAsync(this DocumentClient client, Database database, string id)
        {
            if(database == null)
                throw new ArgumentNullException("database");
            return GetOrCreateDocumentCollectionAsync(client, database.SelfLink, id);
        }
        public static async Task<DocumentCollection> GetOrCreateDocumentCollectionAsync(this DocumentClient client, string databaseSelfLink, string id)
        {
            if(client == null)
                throw new ArgumentNullException("client");
            if(string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null, empty, or consist of whitespace.");
            if(string.IsNullOrWhiteSpace(databaseSelfLink))
                throw new ArgumentException("databaseSelfLink cannot be null, empty, or consist of whitespace.");

            var retval = client.CreateDocumentCollectionQuery(databaseSelfLink).Where(d => d.Id == id).ToArray().FirstOrDefault();
            if(retval == null)
            {
                retval = await client.CreateDocumentCollectionAsync(
                databaseSelfLink,
                new DocumentCollection
                {
                    Id = id
                });
            }

            return retval;
        }
        public static async Task<Document> CreateDocumentAsync(this DocumentClient client, DocumentCollection collection, object document)
        {
            if(client == null)
                throw new ArgumentNullException("client");
            if(collection == null)
                throw new ArgumentNullException("collectoin");
            if(document == null)
                throw new ArgumentNullException("document");
            return await client.CreateDocumentAsync(collection.SelfLink, document);
        }
    }
}
