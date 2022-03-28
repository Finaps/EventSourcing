using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventSourcing.EF.Tests.Postgres.Migrations
{
    public partial class AddInitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccountEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Iban = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    DebtorAccount = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditorAccount = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BankAccountEvents_Discriminator", "\"Type\" IN ('Event<BankAccount>', 'BankAccountCreatedEvent', 'BankAccountFundsDepositedEvent', 'BankAccountFundsTransferredEvent', 'BankAccountFundsWithdrawnEvent')");
                    table.CheckConstraint("CK_BankAccountEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_BankAccountCreatedEvent_NotNull", "NOT \"Type\" = 'BankAccountCreatedEvent' OR (\"Name\" IS NOT NULL AND \"Iban\" IS NOT NULL)");
                    table.CheckConstraint("CK_BankAccountFundsDepositedEvent_NotNull", "NOT \"Type\" = 'BankAccountFundsDepositedEvent' OR (\"Amount\" IS NOT NULL)");
                    table.CheckConstraint("CK_BankAccountFundsTransferredEvent_NotNull", "NOT \"Type\" = 'BankAccountFundsTransferredEvent' OR (\"DebtorAccount\" IS NOT NULL AND \"CreditorAccount\" IS NOT NULL AND \"Amount\" IS NOT NULL)");
                    table.CheckConstraint("CK_BankAccountFundsWithdrawnEvent_NotNull", "NOT \"Type\" = 'BankAccountFundsWithdrawnEvent' OR (\"Amount\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BankAccountEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "BankAccountEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccountProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Iban = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(23)", maxLength: 23, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_EmptyAggregateEvents_Discriminator", "\"Type\" IN ('Event<EmptyAggregate>', 'EmptyEvent')");
                    table.CheckConstraint("CK_EmptyAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_EmptyAggregateEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "EmptyAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmptyProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(23)", maxLength: 23, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockString = table.Column<string>(type: "text", nullable: true),
                    MockNullableString = table.Column<string>(type: "text", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockNullableDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockEnum = table.Column<int>(type: "integer", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: true),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_MockAggregateEvents_Discriminator", "\"Type\" IN ('Event<MockAggregate>', 'MockEvent')");
                    table.CheckConstraint("CK_MockAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_MockAggregateEvents_MockEnum_Enum", "\"MockEnum\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_MockEvent_NotNull", "NOT \"Type\" = 'MockEvent' OR (\"MockBoolean\" IS NOT NULL AND \"MockString\" IS NOT NULL AND \"MockDecimal\" IS NOT NULL AND \"MockDouble\" IS NOT NULL AND \"MockEnum\" IS NOT NULL AND \"MockFlagEnum\" IS NOT NULL AND \"MockFloatList\" IS NOT NULL AND \"MockStringSet\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MockAggregateEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "MockAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockString = table.Column<string>(type: "text", nullable: true),
                    MockNullableString = table.Column<string>(type: "text", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockDouble = table.Column<double>(type: "double precision", nullable: false),
                    MockNullableDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockEnum = table.Column<int>(type: "integer", nullable: false),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: false),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: false),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: false),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: false),
                    Type = table.Column<string>(type: "character varying(23)", maxLength: 23, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection", x => new { x.PartitionId, x.AggregateId });
                    table.CheckConstraint("CK_MockAggregateProjection_MockEnum_Enum", "\"MockEnum\" IN (0, 1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SimpleAggregateEvents_Discriminator", "\"Type\" IN ('Event<SimpleAggregate>', 'SimpleEvent')");
                    table.CheckConstraint("CK_SimpleAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_SimpleAggregateEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "SimpleAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SnapshotAggregateEvents_Discriminator", "\"Type\" IN ('Event<SnapshotAggregate>', 'SnapshotEvent')");
                    table.CheckConstraint("CK_SnapshotAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_SnapshotAggregateEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "SnapshotAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccountSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Iban = table.Column<string>(type: "text", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: true),
                    Type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BankAccountSnapshots_Discriminator", "\"Type\" IN ('Snapshot<BankAccount>', 'BankAccountSnapshot')");
                    table.CheckConstraint("CK_BankAccountSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_BankAccountSnapshot_NotNull", "NOT \"Type\" = 'BankAccountSnapshot' OR (\"Name\" IS NOT NULL AND \"Iban\" IS NOT NULL AND \"Balance\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BankAccountSnapshots_BankAccountEvents_PartitionId_Aggregat~",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "BankAccountEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_EmptyAggregateSnapshots_Discriminator", "\"Type\" IN ('Snapshot<EmptyAggregate>', 'EmptySnapshot')");
                    table.CheckConstraint("CK_EmptyAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_EmptyAggregateSnapshots_EmptyAggregateEvents_PartitionId_Ag~",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "EmptyAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateEvents_MockNestedRecordList",
                columns: table => new
                {
                    MockEventPartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockEventAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockEventIndex = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockString = table.Column<string>(type: "text", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockDouble = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateEvents_MockNestedRecordList", x => new { x.MockEventPartitionId, x.MockEventAggregateId, x.MockEventIndex, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateEvents_MockNestedRecordList_MockAggregateEvent~",
                        columns: x => new { x.MockEventPartitionId, x.MockEventAggregateId, x.MockEventIndex },
                        principalTable: "MockAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockString = table.Column<string>(type: "text", nullable: true),
                    MockNullableString = table.Column<string>(type: "text", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockNullableDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockEnum = table.Column<int>(type: "integer", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: true),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: true),
                    Type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_MockAggregateSnapshots_Discriminator", "\"Type\" IN ('Snapshot<MockAggregate>', 'MockSnapshot')");
                    table.CheckConstraint("CK_MockAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_MockAggregateSnapshots_MockEnum_Enum", "\"MockEnum\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_MockSnapshot_NotNull", "NOT \"Type\" = 'MockSnapshot' OR (\"MockBoolean\" IS NOT NULL AND \"MockString\" IS NOT NULL AND \"MockDecimal\" IS NOT NULL AND \"MockDouble\" IS NOT NULL AND \"MockEnum\" IS NOT NULL AND \"MockFlagEnum\" IS NOT NULL AND \"MockFloatList\" IS NOT NULL AND \"MockStringSet\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MockAggregateSnapshots_MockAggregateEvents_PartitionId_Aggr~",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "MockAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateProjection_MockNestedRecordList",
                columns: table => new
                {
                    MockAggregateProjectionPartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockAggregateProjectionAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockString = table.Column<string>(type: "text", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockDouble = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection_MockNestedRecordList", x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateProjection_MockNestedRecordList_MockAggregateP~",
                        columns: x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId },
                        principalTable: "MockAggregateProjection",
                        principalColumns: new[] { "PartitionId", "AggregateId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SimpleAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_SimpleAggregateSnapshots_SimpleAggregateEvents_PartitionId_~",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "SimpleAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SnapshotAggregateSnapshots_Discriminator", "\"Type\" IN ('Snapshot<SnapshotAggregate>', 'SnapshotSnapshot')");
                    table.CheckConstraint("CK_SnapshotAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_SnapshotSnapshot_NotNull", "NOT \"Type\" = 'SnapshotSnapshot' OR (\"Counter\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SnapshotAggregateSnapshots_SnapshotAggregateEvents_Partitio~",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "SnapshotAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateSnapshots_MockNestedRecordList",
                columns: table => new
                {
                    MockSnapshotPartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockSnapshotAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockSnapshotIndex = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockString = table.Column<string>(type: "text", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockDouble = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateSnapshots_MockNestedRecordList", x => new { x.MockSnapshotPartitionId, x.MockSnapshotAggregateId, x.MockSnapshotIndex, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateSnapshots_MockNestedRecordList_MockAggregateSn~",
                        columns: x => new { x.MockSnapshotPartitionId, x.MockSnapshotAggregateId, x.MockSnapshotIndex },
                        principalTable: "MockAggregateSnapshots",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountEvents_AggregateType",
                table: "BankAccountEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountEvents_PartitionId_AggregateId_PreviousIndex",
                table: "BankAccountEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountEvents_Timestamp",
                table: "BankAccountEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountEvents_Type",
                table: "BankAccountEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_AggregateType",
                table: "BankAccountProjection",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_Hash",
                table: "BankAccountProjection",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_Timestamp",
                table: "BankAccountProjection",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_Type",
                table: "BankAccountProjection",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountSnapshots_AggregateType",
                table: "BankAccountSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountSnapshots_Timestamp",
                table: "BankAccountSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountSnapshots_Type",
                table: "BankAccountSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_AggregateType",
                table: "EmptyAggregateEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_PartitionId_AggregateId_PreviousIndex",
                table: "EmptyAggregateEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_Timestamp",
                table: "EmptyAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_Type",
                table: "EmptyAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateSnapshots_AggregateType",
                table: "EmptyAggregateSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateSnapshots_Timestamp",
                table: "EmptyAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateSnapshots_Type",
                table: "EmptyAggregateSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_AggregateType",
                table: "EmptyProjection",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_Hash",
                table: "EmptyProjection",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_Timestamp",
                table: "EmptyProjection",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_Type",
                table: "EmptyProjection",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateEvents_AggregateType",
                table: "MockAggregateEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateEvents_PartitionId_AggregateId_PreviousIndex",
                table: "MockAggregateEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateEvents_Timestamp",
                table: "MockAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateEvents_Type",
                table: "MockAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateProjection_AggregateType",
                table: "MockAggregateProjection",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateProjection_Hash",
                table: "MockAggregateProjection",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateProjection_Timestamp",
                table: "MockAggregateProjection",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateProjection_Type",
                table: "MockAggregateProjection",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateSnapshots_AggregateType",
                table: "MockAggregateSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateSnapshots_Timestamp",
                table: "MockAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateSnapshots_Type",
                table: "MockAggregateSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_AggregateType",
                table: "SimpleAggregateEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_PartitionId_AggregateId_PreviousIndex",
                table: "SimpleAggregateEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_Timestamp",
                table: "SimpleAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_Type",
                table: "SimpleAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateSnapshots_AggregateType",
                table: "SimpleAggregateSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateSnapshots_Timestamp",
                table: "SimpleAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateSnapshots_Type",
                table: "SimpleAggregateSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_AggregateType",
                table: "SnapshotAggregateEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_PartitionId_AggregateId_PreviousInd~",
                table: "SnapshotAggregateEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_Timestamp",
                table: "SnapshotAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_Type",
                table: "SnapshotAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateSnapshots_AggregateType",
                table: "SnapshotAggregateSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateSnapshots_Timestamp",
                table: "SnapshotAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateSnapshots_Type",
                table: "SnapshotAggregateSnapshots",
                column: "Type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountProjection");

            migrationBuilder.DropTable(
                name: "BankAccountSnapshots");

            migrationBuilder.DropTable(
                name: "EmptyAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "EmptyProjection");

            migrationBuilder.DropTable(
                name: "MockAggregateEvents_MockNestedRecordList");

            migrationBuilder.DropTable(
                name: "MockAggregateProjection_MockNestedRecordList");

            migrationBuilder.DropTable(
                name: "MockAggregateSnapshots_MockNestedRecordList");

            migrationBuilder.DropTable(
                name: "SimpleAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "BankAccountEvents");

            migrationBuilder.DropTable(
                name: "EmptyAggregateEvents");

            migrationBuilder.DropTable(
                name: "MockAggregateProjection");

            migrationBuilder.DropTable(
                name: "MockAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "SimpleAggregateEvents");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateEvents");

            migrationBuilder.DropTable(
                name: "MockAggregateEvents");
        }
    }
}
