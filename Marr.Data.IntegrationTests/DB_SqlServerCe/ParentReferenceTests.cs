using System;
using System.Collections.Generic;
using Marr.Data.Mapping;
using Marr.Data.QGen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Marr.Data.IntegrationTests.DB_SqlServerCe
{
    [TestClass]
    public class ParentReferenceTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            MapRepository.Instance.EnableTraceLogging = true;

            FluentMappings mappings = new FluentMappings();
            mappings
                .Entity<Series>()
                    .Columns.AutoMapSimpleTypeProperties()
                        .For(s => s.Id)
                            .SetPrimaryKey()
                    .Relationships.AutoMapICollectionOrComplexProperties()

                .Entity<Episode>()
                    .Columns.AutoMapSimpleTypeProperties()
                        .For(e => e.Id)
                            .SetPrimaryKey()
                            .SetAltName("EpisodeId")
                        .For(e => e.Title)
                            .SetAltName("EpisodeTitle")
                        .Relationships.AutoMapICollectionOrComplexProperties();
        }

        [TestMethod]
        public void OneToMany_ChildrenShouldGetReferenceToParent()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;

                List<Series> series = db.Query<Series>().Join<Series, Episode>(JoinType.Inner, s => s.Episodes, (s, e) => s.Id == e.SeriesId);

                Assert.IsNotNull(series[0].Episodes[0].Series);
                foreach (var s in series)
                {
                    foreach (var e in s.Episodes)
                    {
                        Assert.AreEqual(s.Id, e.Series.Id);
                        Assert.IsTrue(e.Series.Episodes.Count > 0);
                    }
                }
            }
        }

        [TestMethod]
        public void ManyChildren_Should_LoadParent_AsOneToOne()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;

                List<Episode> episodes = db.Query<Episode>().Join<Episode, Series>(JoinType.Inner, e => e.Series, (e, s) => e.SeriesId == s.Id);

                Assert.IsNotNull(episodes[0].Series);
                foreach (var e in episodes)
                {
                    Assert.AreEqual(e.SeriesId, e.Series.Id);
                }
            }
        }
    }

    public class Series
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<Episode> Episodes { get; set; }
    }

    public class Episode
    {
        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; }

        // This relationship references back to the parent
        public Series Series { get; set; }
    }

}
