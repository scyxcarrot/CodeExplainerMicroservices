using IDS.Core.V2.Databases;
using LiteDB;
using System;
using System.Linq;

namespace LiteDbPlayground
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string[] Phones { get; set; }
        public bool IsActive { get; set; }
    }

    class Program
    {
        static void TryLiteDB()
        {
            using (var db = new LiteDatabase(".\\lite.db"))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<Customer>("customers");

                // Create your new customer instance
                var customer = new Customer
                {
                    Name = "John Doe",
                    Phones = new string[] { "8000-0000", "9000-0000" },
                    IsActive = true
                };

                // Insert new customer document (Id will be auto-incremented)
                col.Insert(customer);

                // Update a document inside a collection
                customer.Name = "Jane Doe";

                col.Update(customer);

                // Index document using document Name property
                col.EnsureIndex(x => x.Name);

                // Use LINQ to query documents (filter, sort, transform)
                var results = col.Query()
                    .Where(x => x.Name.StartsWith("J"))
                    .OrderBy(x => x.Name)
                    .Select(x => new { x.Name, NameUpper = x.Name.ToUpper() })
                    .Limit(10)
                    .ToList();

                // Let's create an index in phone numbers (using expression). It's a multikey index
                col.EnsureIndex(x => x.Phones);

                // and now we can query phones
                var r = col.FindOne(x => x.Phones.Contains("8888-5555"));

                Console.WriteLine(r == null? "8888-5555 not found" : "found 8888-5555");
            }
        }

        static void TryLiteDBDatabase()
        {
            using (var db = new LiteDbDatabase(".\\liteDbDatabase.db", AppDomain.CurrentDomain.GetAssemblies()))
            {

            }
        }

        static void Main(string[] args)
        {
            // This project is setup for other SEs learn the LiteDB behaviour.
            // Any change in this project shouldn't be commit.
            // TODO: Remove this project when the database been implemented
            TryLiteDB();
            TryLiteDBDatabase();
            Console.ReadLine();
        }
    }
}
