#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.DB2;
using FluentMigrator.Runner.Helpers;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.DB2
{
    public class Db2Processor : GenericProcessorBase
    {
        [Obsolete]
        public Db2Processor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
            Quoter = new Db2Quoter();
        }

        public Db2Processor(
            [NotNull] Db2DbFactory factory,
            [NotNull] IMigrationGenerator generator,
            [NotNull] IAnnouncer announcer,
            [NotNull] IOptions<ProcessorOptions> options)
            : base(factory.Factory, generator, announcer, options.Value)
        {
            Quoter = new Db2Quoter();
        }

        public override string DatabaseType => "DB2";

        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { "IBM DB2" };

        public IQuoter Quoter
        {
            get;
            set;
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            var conditions = new List<string>
            {
                BuildEqualityComparison("TABNAME", tableName),
                BuildEqualityComparison("COLNAME", columnName),
            };

            if (!string.IsNullOrEmpty(schemaName))
                conditions.Add(BuildEqualityComparison("TABSCHEMA", schemaName));

            var condition = string.Join(" AND ", conditions);

            var doesExist = Exists("SELECT COLNAME FROM SYSCAT.COLUMNS WHERE {0}", condition);
            return doesExist;
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            var conditions = new List<string>
            {
                BuildEqualityComparison("TABNAME", tableName),
                BuildEqualityComparison("CONSTNAME", constraintName),
            };

            if (!string.IsNullOrEmpty(schemaName))
                conditions.Add(BuildEqualityComparison("TABSCHEMA", schemaName));

            var condition = string.Join(" AND ", conditions);

            return Exists("SELECT CONSTNAME FROM SYSCAT.TABCONST WHERE {0}", condition);
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            var defaultValueAsString = string.Format("%{0}%", FormatHelper.FormatSqlEscape(defaultValue.ToString()));

            var conditions = new List<string>
            {
                BuildEqualityComparison("TABNAME", tableName),
                BuildEqualityComparison("COLNAME", columnName),
                $"\"DEFAULT\" LIKE '{defaultValueAsString}'",
            };

            if (!string.IsNullOrEmpty(schemaName))
                conditions.Add(BuildEqualityComparison("TABSCHEMA", schemaName));

            var condition = string.Join(" AND ", conditions);

            return Exists("SELECT \"DEFAULT\" FROM SYSCAT.COLUMNS WHERE {0}", condition);
        }

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read();
            }
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            var conditions = new List<string>
            {
                BuildEqualityComparison("TABNAME", tableName),
                BuildEqualityComparison("INDNAME", indexName),
            };

            if (!string.IsNullOrEmpty(schemaName))
                conditions.Add(BuildEqualityComparison("INDSCHEMA", schemaName));

            var condition = string.Join(" AND ", conditions);

            var doesExist = Exists("SELECT INDNAME FROM SYSCAT.INDEXES WHERE {0}", condition);

            return doesExist;
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
            {
                return;
            }

            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }

        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            using (var reader = command.ExecuteReader())
            {
                return reader.ReadDataSet();
            }
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM {0}", Quoter.QuoteTableName(tableName, schemaName));
        }

        public override bool SchemaExists(string schemaName)
        {
            var conditions = new List<string>
            {
                BuildEqualityComparison("SCHEMANAME", schemaName),
            };

            var condition = string.Join(" AND ", conditions);

            return Exists("SELECT SCHEMANAME FROM SYSCAT.SCHEMATA WHERE {0}", condition);
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            var conditions = new List<string>
            {
                BuildEqualityComparison("TABNAME", tableName),
            };

            if (!string.IsNullOrEmpty(schemaName))
                conditions.Add(BuildEqualityComparison("TABSCHEMA", schemaName));

            var condition = string.Join(" AND ", conditions);

            return Exists("SELECT TABNAME FROM SYSCAT.TABLES WHERE {0}", condition);
        }

        protected override void Process(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
            {
                return;
            }

            EnsureConnectionIsOpen();

            using (var command = CreateCommand(sql))
            {
                command.ExecuteNonQuery();
            }
        }

        private string BuildEqualityComparison(string columnName, string value)
        {
            if (Quoter.IsQuoted(value))
            {
                return $"{Quoter.QuoteColumnName(columnName)}='{FormatHelper.FormatSqlEscape(Quoter.UnQuote(value))}'";
            }

            return $"LCASE({Quoter.QuoteColumnName(columnName)})=LCASE('{FormatHelper.FormatSqlEscape(Quoter.UnQuote(value))}')";
        }
    }
}
