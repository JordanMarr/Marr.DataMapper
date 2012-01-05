using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace Marr.Data.Tests
{
    [TestClass]
    public class UnitOfWorkTest
    {
        [TestMethod]
        public void One_UnitOfWork_ShouldCreateAndDisposeDataMapper_Once()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());
            
            using (uow)
            {
                db = uow.DB;
            }

            factory.AssertWasCalled(f => f.Create(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.Dispose(), o => o.Repeat.Once());
        }

        [TestMethod]
        public void Two_UnitOfWorks_ShouldCreateAndDisposeDataMapper_Twice()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow)
            {
                db = uow.DB;
            }

            using (uow)
            {
                db = uow.DB;
            }

            factory.AssertWasCalled(f => f.Create(), o => o.Repeat.Twice());
            db.AssertWasCalled(d => d.Dispose(), o => o.Repeat.Twice());
        }

        [TestMethod]
        public void UnitOfWork_ShouldOnlyCreateAndDisposeOneDataMapper()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow)
            {
                uow.DB.ExecuteNonQuery("query1");
                uow.DB.ExecuteNonQuery("query2");
            }

            factory.AssertWasCalled(f => f.Create(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.Dispose(), o => o.Repeat.Once());
        }

        [TestMethod]
        public void WhenOneSharedContextExists_ShouldControlDataMapperLifetime()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow.SharedContext)
            {
                uow.DB.ExecuteNonQuery("query1");

                using (uow)
                {
                    uow.DB.ExecuteNonQuery("query2");
                }

                using (uow)
                {
                    uow.DB.ExecuteNonQuery("query3");
                }

                uow.DB.ExecuteNonQuery("query4");
            }

            factory.AssertWasCalled(f => f.Create(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.ExecuteNonQuery(""), o => o.IgnoreArguments().Repeat.Times(4));
            db.AssertWasCalled(d => d.Dispose(), o => o.Repeat.Once());
        }

        [TestMethod]
        public void WhenMultipleSharedContextsExist_OuterSharedContext_ShouldControlDataMapperLifetime()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow.SharedContext)
            {
                uow.DB.ExecuteNonQuery("query1");

                using (uow.SharedContext)
                {
                    uow.DB.ExecuteNonQuery("query2");

                    using (uow)
                    {
                        uow.DB.ExecuteNonQuery("query3");
                    }
                }

                uow.DB.ExecuteNonQuery("query4");
            }

            factory.AssertWasCalled(f => f.Create(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.Dispose(), o => o.Repeat.Once());
        }
    }

    public interface ITestDbFactory
    {
        IDataMapper Create();
    }
}
