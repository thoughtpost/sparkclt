using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Thoughtpost.Azure.Query
{
    public class HistoricalTableQuery<T> where T : ITableEntity, new ()
    {
        public HistoricalTableQuery(CloudTable table)
        {
            this.Table = table;
        }

        public CloudTable Table { get; set; }

        public List<T> Get(object key)
        {
            return Get(GetQuery(key));
        }

        public T Get(string pk, string rk)
        {
            List<T> list = Get(GetQuery(pk, rk), 1);
            if (list == null) return default(T);
            return list.FirstOrDefault();
        }

        public List<T> Get(object key, DateTime start, DateTime end)
        {
            return Get(GetQuery(key, start, end));
        }

        public List<T> Get(object key, string start, string end)
        {
            return Get(GetQuery(key, start, end));
        }

        public List<T> Get(T readStart, T readEnd)
        {
            return Get(GetQuery(readStart, readEnd));
        }

        public List<T> Get(TableQuery<T> rangeQuery)
        {
            return Get(rangeQuery, -1);
        }

        public List<T> Get(TableQuery<T> rangeQuery, int limit )
        {
            int count = 0;
            List<T> reads = new List<T>();

            if (limit > 0)
            {
                rangeQuery = rangeQuery.Take(limit);
            }

            // Loop through the results, displaying information about the entity.
            foreach (T entity in this.Table.ExecuteQuery(rangeQuery))
            {
                reads.Add(entity);
                
                count++;
                if (limit > 0 && count >= limit) break;
            }

            return reads;
        }

        protected TableQuery<T> GetQuery(T readStart, T readEnd)
        {
            string pk = "";
            string rk1 = DateTimeUtility.MinTimeKey;
            string rk2 = DateTimeUtility.MaxTimeKey;
            if (readStart == null && readEnd == null)
            {
                throw new InvalidOperationException("Both objects were null");
            }
            if (readStart != null)
            {
                if ( readStart is ITableEntity)
                {
                    ITableEntity e = readStart as ITableEntity;
                    pk = e.PartitionKey;
                    rk1 = e.RowKey;
                }
            }
            if (readEnd != null)
            {
                if (readEnd is ITableEntity)
                {
                    ITableEntity e = readEnd as ITableEntity;
                    pk = e.PartitionKey;
                    rk2 = e.RowKey;
                }
            }

            return GetQuery(pk, rk1, rk2);
        }

        protected TableQuery<T> GetQuery(object key, DateTime start, DateTime end)
        {
            string sd1 = DateTimeUtility.InverseTimeKey(start);
            string sd2 = DateTimeUtility.InverseTimeKey(end);

            return GetQuery(key, sd1, sd2);
        }

        protected TableQuery<T> GetQuery(object key, string start, DateTime end)
        {
            string sd2 = DateTimeUtility.InverseTimeKey(end);

            return GetQuery(key, start, sd2);
        }

        protected TableQuery<T> GetQuery(object key, string start, string end)
        {
            // Create the table query.
            TableQuery<T> rangeQuery = new TableQuery<T>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key.ToString()),
                    TableOperators.And,
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, end),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, start)
                    )
                )
            );

            return rangeQuery;
        }


        protected TableQuery<T> GetQuery(string pk, string rk)
        {
            // Create the table query.
            TableQuery<T> rangeQuery = new TableQuery<T>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk)
                )
            );

            return rangeQuery;
        }

        protected TableQuery<T> GetQuery(object key)
        {
            // Create the table query.
            TableQuery<T> rangeQuery = new TableQuery<T>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key.ToString())
            );

            return rangeQuery;
        }


        public List<T> ParallelGet(IList<object> keys)
        {
            return ParallelGet(keys, DateTime.MinValue, DateTime.MaxValue);
        }

        public List<T> ParallelGet(IList<object> keys, DateTime start, DateTime end)
        {
            List<Task<List<T>>> tasklist = new List<Task<List<T>>>();

            foreach (object key in keys)
            {
                Task<List<T>> t = Task.Factory.StartNew<List<T>>(() => Get(key.ToString(), start, end));

                tasklist.Add(t);
            }

            Task.WaitAll(tasklist.ToArray());

            List<T> results = new List<T>();

            foreach (Task<List<T>> t in tasklist)
            {
                if (t.Result != null && t.Result.Count != 0)
                {
                }

                results.AddRange(t.Result);
            }

            return results;
        }


    }

}
