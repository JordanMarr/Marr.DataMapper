/*  Copyright (C) 2008 - 2011 Jordan Marr

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>. */

using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using Marr.Data.Parameters;
using System.Linq.Expressions;
using Marr.Data.QGen;
using System.Reflection;

namespace Marr.Data
{
    public interface IDataMapper : IDisposable
    {
        string ProviderString { get; }
        ParameterChainMethods AddParameter(string name, object value);
        IDbDataParameter AddParameter(IDbDataParameter parameter);
        int Update<T>(T entity, Expression<Func<T, bool>> filter);
        int Update<T>(string tableName, T entity, Expression<Func<T, bool>> filter);
        int Update<T>(T entity, string sql);
        int Insert<T>(T entity);
        int Insert<T>(string tableName, T entity);
        int Insert<T>(T entity, string sql);
        int Delete<T>(Expression<Func<T, bool>> filter);
        int Delete<T>(string tableName, Expression<Func<T, bool>> filter);
        void BeginTransaction();
        DbCommand Command { get; }
        void Commit();
        int DeleteDataTable(DataTable dt, string deleteSP);
        int ExecuteNonQuery(string sql);
        object ExecuteScalar(string sql);
        DataSet GetDataSet(string sql);
        DataSet GetDataSet(string sql, DataSet ds, string tableName);
        DataTable GetDataTable(string sql, DataTable dt, string tableName);
        DataTable GetDataTable(string sql);
        int InsertDataTable(DataTable table, string insertSP);
        int InsertDataTable(DataTable table, string insertSP, UpdateRowSource updateRowSource);
        DbParameterCollection Parameters { get; }
        void Find(string sql);
        T Find<T>(string sql);
        T Find<T>(string sql, T ent);
        QueryBuilder<T> Query<T>();
        List<T> Query<T>(string sql);
        List<T> QueryToGraph<T>(string sql);
        ICollection<T> QueryToGraph<T>(string sql, ICollection<T> entityList);
        void Query(string sql);
        ICollection<T> Query<T>(string sql, ICollection<T> entityList);
        void RollBack();
        SqlModes SqlMode { get; set; }        
        int UpdateDataSet(DataSet ds, string updateSP);
        event EventHandler<LoadEntityEventArgs> LoadEntity;
        event EventHandler OpeningConnection;
    }
}
