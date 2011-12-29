using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Diagnostics;
using Marr.Data.IntegrationTests.DB_Access.Entities;

namespace Marr.Data.IntegrationTests.DB_Access
{
    [TestClass]
    public class AccessTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            MapRepository.Instance.EnableTraceLogging = true;
            //MapRepository.Instance.ReflectionStrategy = new Marr.Data.Reflection.SimpleReflectionStrategy();
        }

        [TestMethod]
        public void CrudTest()
        {
            using (var db = CreateAccessDB())
            {
                try
                {
                    db.BeginTransaction();

                    // Query for an entity that does not exist
                    Product p1 = db.Query<Product>().From("V_Product").Graph()
                        .Where(p => p.Name == "P1").FirstOrDefault();
                    Assert.IsNull(p1, "P1 should not exist yet");

                    // Insert a new entity
                    Product newProduct = new Product { Name = "P1", NewItem = true, Price = 5.55M, Description = "My product", CategoryID = 2 };
                    db.Insert<Product>(newProduct);

                    // Query newly inserted entity
                    p1 = db.Query<Product>().From("V_Product").Graph()
                        .Where(p => p.Name == "P1").FirstOrDefault();
                    Assert.IsNotNull(p1, "P1 should have been added to the database.");

                    Assert.AreEqual("Matted Prints", p1.Category.Name, "The category child object should have been loaded");
                    Assert.IsTrue(p1.OptionTypes.Count == 1, "There should be 1 associated option type");
                    Assert.IsTrue(p1.OptionTypes.First().Options.Count == 2, "There should be 2 associated options");

                    // Update entity
                    p1.Price = 444.44M;
                    db.Update<Product>(p1, p => p.Name == "P1");

                    var updatedP1 = db.Query<Product>().From("V_Product").Graph()
                        .Where(p => p.Name == "P1").FirstOrDefault();
                    Assert.AreEqual(444.44M, updatedP1.Price);

                    // Delete entity
                    db.Delete<Product>(p => p.Name == "P1");

                    var deletedP1 = db.Query<Product>().From("V_Product").Graph()
                        .Where(p => p.Name == "P1").FirstOrDefault();
                    Assert.IsNull(deletedP1, "P1 should have been deleted.");

                    db.Commit();
                }
                catch
                {
                    db.RollBack();
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates x number of lists of object graphs (2 levels deep).
        ///         -> Category (1-1)
        /// Product 
        ///         -> List of OptionType (1-M) 
        ///                 -> List of Option (1-M)
        /// </summary>
        [TestMethod]
        public void TimedTest_AccessQuery()
        {
            var times = new List<long>();
            var profiler = new Stopwatch();

            using (var db = CreateAccessDB())
            {
                for (int i = 1; i <= 4; i++)
                {
                    profiler.Restart();

                    var products = db.Query<Product>().From("V_Product").Graph().ToList();

                    profiler.Stop();

                    if (products.Count == 0)
                        throw new Exception("Result set was empty");

                    Trace.WriteLine(string.Format("iteration {0} : {1}ms", i, profiler.ElapsedMilliseconds));
                    times.Add(profiler.ElapsedMilliseconds);
                }
            }

            Trace.WriteLine("---------------");
            times.RemoveAt(0);
            Trace.WriteLine(string.Format("Average time after inital load: {0}", times.Average()));
        }

        [TestMethod]
        public void TimedTest_PassingInQueryText()
        {
            var times = new List<long>();
            var profiler = new Stopwatch();

            using (var db = CreateAccessDB())
            {
                db.SqlMode = SqlModes.Text;

                for (int i = 1; i <= 4; i++)
                {
                    profiler.Restart();

                    string sql = @"SELECT Product.ID, Product.Name, Product.Description, Product.Price, Product.CategoryID, Product.ImageFileName, Product.NewItem, Product.IsSplash, Category.Name AS CategoryName, Option.OptionTypeID, OptionType.Type, OptionType.MultiPick, Option.ID AS OptionID, Option.Description AS OptionDescription, Option.Price AS OptionPrice
                                   FROM ((Category LEFT JOIN OptionType ON Category.ID = OptionType.CategoryID) RIGHT JOIN Product ON Category.ID = Product.CategoryID) LEFT JOIN [Option] ON OptionType.ID = Option.OptionTypeID;";
                    var products = db.Query<Product>().QueryText(sql).Graph().ToList();

                    profiler.Stop();

                    if (products.Count == 0)
                        throw new Exception("Result set was empty");

                    Trace.WriteLine(string.Format("iteration {0} : {1}ms", i, profiler.ElapsedMilliseconds));
                    times.Add(profiler.ElapsedMilliseconds);
                }
            }

            Trace.WriteLine("---------------");
            times.RemoveAt(0);
            Trace.WriteLine(string.Format("Average time after inital load: {0}", times.Average()));
        }

    }
}
