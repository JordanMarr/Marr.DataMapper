using System;
using System.Collections.Generic;
using Marr.Data.Mapping;
using Marr.Data.QGen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Marr.Data.IntegrationTests.DB_SqlServerCe
{
    [TestClass]
    public class TvSeries : TestBase
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
                    .Relationships.MapProperties<Episode>()
                        .For("_series")
                            .LazyLoad<Series>((db, e) => db.Query<Series>().Where(s => s.Id == e.SeriesId).ToList().FirstOrDefault());
        }

        [TestMethod]
        public void OneToManyChild_Should_LazyLoadParent()
        {
            using (var db = CreateSqlServerCeDB())
            {
                db.SqlMode = SqlModes.Text;

                List<Series> results1 = db.Query<Series>().Join<Series, Episode>(JoinType.Inner, s => s.Episodes, (s, e) => s.Id == e.SeriesId);
                List<Episode> results2 = db.Query<Episode>().Join<Episode, Series>(JoinType.Inner, e => e.Series, (e, s) => e.SeriesId == s.Id);

                Assert.IsNotNull(results1[0].Episodes[0].Series);
                Assert.IsNotNull(results2[0].Series);
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
        private dynamic _series;

        public int Id { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; }
        public Series Series
        {
            get
            {
                return _series;
            }
        }
    }

}
