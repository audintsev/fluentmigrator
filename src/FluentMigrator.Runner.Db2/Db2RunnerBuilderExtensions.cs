﻿#region License
// Copyright (c) 2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using FluentMigrator.Runner.Generators.DB2;
using FluentMigrator.Runner.Generators.DB2.iSeries;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.DB2;
using FluentMigrator.Runner.Processors.DB2.iSeries;

using Microsoft.Extensions.DependencyInjection;

namespace FluentMigrator.Runner
{
    /// <summary>
    /// Extension methods for <see cref="IMigrationRunnerBuilder"/>
    /// </summary>
    public static class Db2RunnerBuilderExtensions
    {
        /// <summary>
        /// Adds DB2 support (Linux and Windows server)
        /// </summary>
        /// <param name="builder">The builder to add the DB2-specific services to</param>
        /// <returns>The migration runner builder</returns>
        public static IMigrationRunnerBuilder UseDb2(this IMigrationRunnerBuilder builder)
        {
            builder.Services
                .AddScoped<Db2DbFactory>()
                .AddScoped<IMigrationProcessor, Db2Processor>()
                .AddScoped<IMigrationGenerator, Db2Generator>();
            return builder;
        }

        /// <summary>
        /// Adds DB2 iSeries support
        /// </summary>
        /// <param name="builder">The builder to add the DB2 iSeries-specific services to</param>
        /// <returns>The migration runner builder</returns>
        public static IMigrationRunnerBuilder UseDb2ISeries(this IMigrationRunnerBuilder builder)
        {
            builder.Services
                .AddScoped<Db2ISeriesDbFactory>()
                .AddScoped<IMigrationProcessor, Db2ISeriesProcessor>()
                .AddScoped<IMigrationGenerator, Db2ISeriesGenerator>();
            return builder;
        }
    }
}
