// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.TestModels;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore
{
    public class MonsterFixupSqliteTest : MonsterFixupTestBase
    {
        private static readonly HashSet<string> _createdDatabases = new HashSet<string>();

        private static readonly ConcurrentDictionary<string, object> _creationLocks
            = new ConcurrentDictionary<string, object>();

        protected override IServiceProvider CreateServiceProvider()
            => new ServiceCollection().AddEntityFrameworkSqlite().BuildServiceProvider(validateScopes: true);

        protected override DbContextOptions CreateOptions(string databaseName)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(CreateConnectionString(databaseName))
                .EnableSensitiveDataLogging();

            return optionsBuilder.Options;
        }

        private static string CreateConnectionString(string name)
            => new SqliteConnectionStringBuilder
            {
                DataSource = name + ".db"
            }.ConnectionString;

        protected override void CreateAndSeedDatabase(string databaseName, Func<MonsterContext> createContext, Action<MonsterContext> seed)
        {
            var creationLock = _creationLocks.GetOrAdd(databaseName, n => new object());
            lock (creationLock)
            {
                if (!_createdDatabases.Contains(databaseName))
                {
                    using (var context = createContext())
                    {
                        context.Database.EnsureClean();
                        seed(context);
                    }

                    _createdDatabases.Add(databaseName);
                }
            }
        }

        protected override void OnModelCreating<TMessage, TProductPhoto, TProductReview>(ModelBuilder builder)
        {
            base.OnModelCreating<TMessage, TProductPhoto, TProductReview>(builder);
            builder.Entity<TMessage>().HasKey(e => e.MessageId);
            builder.Entity<TProductPhoto>().HasKey(e => e.PhotoId);
            builder.Entity<TProductReview>().HasKey(e => e.ReviewId);
        }
    }
}
