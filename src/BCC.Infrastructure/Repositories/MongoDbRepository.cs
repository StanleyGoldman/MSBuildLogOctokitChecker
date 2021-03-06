﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BCC.Infrastructure.Interfaces;
using MongoDB.Driver;

namespace BCC.Infrastructure.Repositories
{
    public class MongoDbRepository<T, TField> : IRepository<T, TField> where T : class, new()
    {
        private readonly Expression<Func<T, TField>> _idExpression;
        protected readonly IMongoCollection<T> Entities;

        public MongoDbRepository(IMongoCollection<T> entities, Expression<Func<T, TField>> idExpression)
        {
            Entities = entities;
            _idExpression = idExpression;
        }

        public async Task DeleteAsync(T item)
        {
            var getter = _idExpression.Compile();

            var value = getter(item);

            await DeleteAsync(value);
        }

        public async Task DeleteAsync(TField value)
        {
            var filter = Builders<T>.Filter.Eq(_idExpression, value);

            await Entities.DeleteOneAsync(filter);
        }

        public async Task<T> GetAsync(TField value)
        {
            var filter = Builders<T>.Filter.Eq(_idExpression, value);

            return await Entities.Find(filter).FirstAsync();
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return GetAllAsync(FilterDefinition<T>.Empty);
        }

        public Task<IEnumerable<T>> GetAllAsync(int skip, int take)
        {
            return GetAllAsync(FilterDefinition<T>.Empty, skip, take);
        }

        public async Task AddAsync(T item)
        {
            await Entities.InsertOneAsync(item);
        }

        public async Task AddAsync(IEnumerable<T> items)
        {
            await Entities.InsertManyAsync(items);
        }

        protected async Task DeleteAsync(FilterDefinition<T> filter)
        {
            await Entities.DeleteManyAsync(filter);
        }

        protected Task<IEnumerable<T>> GetAllAsync(FilterDefinition<T> filter)
        {
            var items = Entities.Find(filter).ToEnumerable();

            return Task.FromResult(items);
        }

        protected Task<IEnumerable<T>> GetAllAsync(FilterDefinition<T> filter, int skip, int take)
        {
            var items = Entities.Find(filter).Skip(skip).Limit(take).ToEnumerable();

            return Task.FromResult(items);
        }
    }
}