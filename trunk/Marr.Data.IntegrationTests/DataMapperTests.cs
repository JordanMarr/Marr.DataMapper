using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marr.Data.IntegrationTests.Entities;

namespace Marr.Data.IntegrationTests
{
    /// <summary>
    /// Contains integration tests for the data mapper.
    /// </summary>
    [TestClass]
    public class DataMapperTests
    {
        //[TestMethod]
        public void TestLinq()
        {
            var db = new DataMapper(System.Data.SqlClient.SqlClientFactory.Instance, "Data Source=a;Initial Catalog=a;User Id=a;Password=a;");
            var results = from o in db.Query<Order>()
                          where o.ID > 5
                          orderby o.ID, o.OrderName descending
                          select o;

            foreach (var result in results)
            {
                string orderName = result.OrderName;
                int orderID = result.ID;
            }
        }
    }
}
