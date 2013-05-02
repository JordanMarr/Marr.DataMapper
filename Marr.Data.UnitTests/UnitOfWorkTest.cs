using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using System.Data.Common;

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

        [TestMethod]
        public void OneUnitOfWork_ShouldBeginAndCommitOneTransaction()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            var cmd = MockRepository.GenerateStub<DbCommand>();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow)
            {
                uow.BeginTransaction();

                uow.DB.ExecuteNonQuery("query1");
                uow.DB.ExecuteNonQuery("query2");

                uow.Commit();
            }

            db.Expect(d => d.BeginTransaction()).Repeat.Once();
            db.Expect(d => d.Commit()).Repeat.Once();
        }

        [TestMethod]
        public void OneUnitOfWork_ShouldBeginAndRollBackOneTransaction()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            var cmd = MockRepository.GenerateStub<DbCommand>();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow)
            {
                uow.BeginTransaction();

                uow.DB.ExecuteNonQuery("query1");
                uow.DB.ExecuteNonQuery("query2");

                uow.RollBack();
            }

            db.Expect(d => d.BeginTransaction()).Repeat.Once();
            db.Expect(d => d.RollBack()).Repeat.Once();
        }

        [TestMethod]
        public void UnitOfWork_WithNestedTransactions_ShouldBeginAndCommitOneTransaction()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            var cmd = MockRepository.GenerateStub<DbCommand>();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow.SharedContext)
            {
                uow.BeginTransaction();

                uow.DB.ExecuteNonQuery("query1");

                using (uow)
                {
                    uow.BeginTransaction();

                    uow.DB.ExecuteNonQuery("query2");
                    uow.DB.ExecuteNonQuery("query3");

                    uow.Commit();
                }

                uow.Commit();
            }

            db.AssertWasCalled(d => d.BeginTransaction(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.Commit());
        }

        [TestMethod]
        public void UnitOfWork_WithNestedTransactions_InnerCommitShouldBeIgnored()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            var cmd = MockRepository.GenerateStub<DbCommand>();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            using (uow.SharedContext)
            {
                uow.BeginTransaction();

                uow.DB.ExecuteNonQuery("query1");

                using (uow)
                {
                    uow.BeginTransaction();

                    uow.DB.ExecuteNonQuery("query2");
                    uow.DB.ExecuteNonQuery("query3");

                    uow.Commit();
                }

                uow.RollBack();
            }

            db.AssertWasCalled(d => d.BeginTransaction(), o => o.Repeat.Once());
            db.AssertWasNotCalled(d => d.Commit());
            db.AssertWasCalled(d => d.RollBack());
        }

        [TestMethod]
        public void UnitOfWork_WithNestedTransactions_InnerRollBackShouldBeCalled()
        {
            var db = MockRepository.GenerateStub<IDataMapper>();
            var cmd = MockRepository.GenerateStub<DbCommand>();
            db.Expect(d => d.Command).Return(cmd).Repeat.Any();
            ITestDbFactory factory = MockRepository.GenerateMock<ITestDbFactory>();
            factory.Expect(f => f.Create()).Return(db).Repeat.Any();
            UnitOfWork uow = new UnitOfWork(() => factory.Create());

            bool exceptionThrown = false;

            using (uow.SharedContext)
            {
                uow.BeginTransaction();

                uow.DB.ExecuteNonQuery("query1");

                try
                {
                    using (uow)
                    {
                        uow.BeginTransaction();

                        uow.DB.ExecuteNonQuery("query2");
                        uow.DB.ExecuteNonQuery("query3");

                        uow.RollBack();
                    }

                    uow.Commit();
                }
                catch (NestedSharedContextRollBackException ex)
                {
                    exceptionThrown = true;
                }                
            }

            db.AssertWasCalled(d => d.BeginTransaction(), o => o.Repeat.Once());
            db.AssertWasCalled(d => d.RollBack());
            db.AssertWasNotCalled(d => d.Commit());
            Assert.IsTrue(exceptionThrown);
        }
    }

    public interface ITestDbFactory
    {
        IDataMapper Create();
    }
}
