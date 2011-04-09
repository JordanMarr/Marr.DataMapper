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
            //MapRepository.Instance.EnableTraceLogging = true;
            //MapRepository.Instance.ReflectionStrategy = new Marr.Data.Reflection.SimpleReflectionStrategy();
        }

        /// <summary>
        /// Creates x number of lists of object graphs (2 levels deep).
        ///         -> Category (1-1)
        /// Product 
        ///         -> List of OptionType (1-M) 
        ///                 -> List of Option (1-M)
        /// </summary>
        [TestMethod]
        public void TimedTest()
        {
            var times = new List<long>();
            var profiler = new Stopwatch();

            using (var db = CreateAccessDB())
            {
                for (int i = 1; i <= 4; i++)
                {
                    profiler.Restart();

                    var products = db.Query<Product>().Table("V_Product").Graph().ToList();

                    profiler.Stop();

                    Trace.WriteLine(string.Format("iteration {0} : {1}ms", i, profiler.ElapsedMilliseconds));
                    times.Add(profiler.ElapsedMilliseconds);
                }
            }

            Trace.WriteLine("---------------");
            Trace.WriteLine(string.Format("Average time: {0}", times.Average()));
        }
    }
}
