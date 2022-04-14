using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSourcing.EF.Tests.SqlServer.Migrations
{
    public partial class AddIntialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccountEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DebtorAccount = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreditorAccount = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BankAccountEvents_Discriminator", "[Type] IN (N'Event<BankAccount>', N'BankAccountCreatedEvent', N'BankAccountFundsDepositedEvent', N'BankAccountFundsTransferredEvent', N'BankAccountFundsWithdrawnEvent')");
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
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_EmptyAggregateEvents_Discriminator", "[Type] IN (N'Event<EmptyAggregate>', N'EmptyEvent')");
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
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: true),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNullableString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MockDouble = table.Column<double>(type: "float", nullable: true),
                    MockNullableDouble = table.Column<double>(type: "float", nullable: true),
                    MockEnum = table.Column<int>(type: "int", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "tinyint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "bit", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "float", nullable: true),
                    MockFloatList = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MockStringSet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_MockAggregateEvents_Discriminator", "[Type] IN (N'Event<MockAggregate>', N'MockEvent')");
                    table.CheckConstraint("CK_MockAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_MockAggregateEvents_MockEnum_Enum", "[MockEnum] IN (0, 1, 2)");
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
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: false),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNullableString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MockDouble = table.Column<double>(type: "float", nullable: false),
                    MockNullableDouble = table.Column<double>(type: "float", nullable: true),
                    MockEnum = table.Column<int>(type: "int", nullable: false),
                    MockFlagEnum = table.Column<byte>(type: "tinyint", nullable: false),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "bit", nullable: false),
                    MockNestedRecord_MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "float", nullable: false),
                    MockFloatList = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MockStringSet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactoryType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection", x => new { x.PartitionId, x.AggregateId });
                    table.CheckConstraint("CK_MockAggregateProjection_MockEnum_Enum", "[MockEnum] IN (0, 1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SimpleAggregateEvents_Discriminator", "[Type] IN (N'Event<SimpleAggregate>', N'SimpleEvent')");
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
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SnapshotAggregateEvents_Discriminator", "[Type] IN (N'Event<SnapshotAggregate>', N'SnapshotEvent')");
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
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BankAccountSnapshots_Discriminator", "[Type] IN (N'Snapshot<BankAccount>', N'BankAccountSnapshot')");
                    table.CheckConstraint("CK_BankAccountSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_BankAccountSnapshot_NotNull", "NOT \"Type\" = 'BankAccountSnapshot' OR (\"Name\" IS NOT NULL AND \"Iban\" IS NOT NULL AND \"Balance\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BankAccountSnapshots_BankAccountEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "BankAccountEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_EmptyAggregateSnapshots_Discriminator", "[Type] IN (N'Snapshot<EmptyAggregate>', N'EmptySnapshot')");
                    table.CheckConstraint("CK_EmptyAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_EmptyAggregateSnapshots_EmptyAggregateEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "EmptyAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    ReferenceAggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmptyAggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_ReferenceAggregateEvents_Discriminator", "[Type] IN (N'Event<ReferenceAggregate>', N'ReferenceEvent')");
                    table.CheckConstraint("CK_ReferenceAggregateEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_ReferenceEvent_NotNull", "NOT \"Type\" = 'ReferenceEvent' OR (\"ReferenceAggregateId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ReferenceAggregateEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "ReferenceAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferenceEvent_EmptyAggregateId",
                        columns: x => new { x.PartitionId, x.EmptyAggregateId, x.ZeroIndex },
                        principalTable: "EmptyAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferenceEvent_ReferenceAggregateId",
                        columns: x => new { x.PartitionId, x.ReferenceAggregateId, x.ZeroIndex },
                        principalTable: "ReferenceAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateEvents_MockNestedRecordList",
                columns: table => new
                {
                    MockEventPartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockEventAggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockEventIndex = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: false),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MockDouble = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateEvents_MockNestedRecordList", x => new { x.MockEventPartitionId, x.MockEventAggregateId, x.MockEventIndex, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateEvents_MockNestedRecordList_MockAggregateEvents_MockEventPartitionId_MockEventAggregateId_MockEventIndex",
                        columns: x => new { x.MockEventPartitionId, x.MockEventAggregateId, x.MockEventIndex },
                        principalTable: "MockAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: true),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNullableString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MockDouble = table.Column<double>(type: "float", nullable: true),
                    MockNullableDouble = table.Column<double>(type: "float", nullable: true),
                    MockEnum = table.Column<int>(type: "int", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "tinyint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "bit", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "float", nullable: true),
                    MockFloatList = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MockStringSet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_MockAggregateSnapshots_Discriminator", "[Type] IN (N'Snapshot<MockAggregate>', N'MockSnapshot')");
                    table.CheckConstraint("CK_MockAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_MockAggregateSnapshots_MockEnum_Enum", "[MockEnum] IN (0, 1, 2)");
                    table.CheckConstraint("CK_MockSnapshot_NotNull", "NOT \"Type\" = 'MockSnapshot' OR (\"MockBoolean\" IS NOT NULL AND \"MockString\" IS NOT NULL AND \"MockDecimal\" IS NOT NULL AND \"MockDouble\" IS NOT NULL AND \"MockEnum\" IS NOT NULL AND \"MockFlagEnum\" IS NOT NULL AND \"MockFloatList\" IS NOT NULL AND \"MockStringSet\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MockAggregateSnapshots_MockAggregateEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "MockAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateProjection_MockNestedRecordList",
                columns: table => new
                {
                    MockAggregateProjectionPartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockAggregateProjectionAggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: false),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MockDouble = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection_MockNestedRecordList", x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateProjection_MockNestedRecordList_MockAggregateProjection_MockAggregateProjectionPartitionId_MockAggregateProject~",
                        columns: x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId },
                        principalTable: "MockAggregateProjection",
                        principalColumns: new[] { "PartitionId", "AggregateId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SimpleAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_SimpleAggregateSnapshots_SimpleAggregateEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "SimpleAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_SnapshotAggregateSnapshots_Discriminator", "[Type] IN (N'Snapshot<SnapshotAggregate>', N'SnapshotSnapshot')");
                    table.CheckConstraint("CK_SnapshotAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_SnapshotSnapshot_NotNull", "NOT \"Type\" = 'SnapshotSnapshot' OR (\"Counter\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SnapshotAggregateSnapshots_SnapshotAggregateEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "SnapshotAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_ReferenceAggregateSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_ReferenceAggregateSnapshots_ReferenceAggregateEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "ReferenceAggregateEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateSnapshots_MockNestedRecordList",
                columns: table => new
                {
                    MockSnapshotPartitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockSnapshotAggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockSnapshotIndex = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MockBoolean = table.Column<bool>(type: "bit", nullable: false),
                    MockString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MockDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MockDouble = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateSnapshots_MockNestedRecordList", x => new { x.MockSnapshotPartitionId, x.MockSnapshotAggregateId, x.MockSnapshotIndex, x.Id });
                    table.ForeignKey(
                        name: "FK_MockAggregateSnapshots_MockNestedRecordList_MockAggregateSnapshots_MockSnapshotPartitionId_MockSnapshotAggregateId_MockSnaps~",
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
                name: "IX_ReferenceAggregateEvents_AggregateType",
                table: "ReferenceAggregateEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateEvents_PartitionId_AggregateId_PreviousIndex",
                table: "ReferenceAggregateEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateEvents_PartitionId_EmptyAggregateId_ZeroIndex",
                table: "ReferenceAggregateEvents",
                columns: new[] { "PartitionId", "EmptyAggregateId", "ZeroIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateEvents_PartitionId_ReferenceAggregateId_ZeroIndex",
                table: "ReferenceAggregateEvents",
                columns: new[] { "PartitionId", "ReferenceAggregateId", "ZeroIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateEvents_Timestamp",
                table: "ReferenceAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateEvents_Type",
                table: "ReferenceAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateSnapshots_AggregateType",
                table: "ReferenceAggregateSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateSnapshots_Timestamp",
                table: "ReferenceAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceAggregateSnapshots_Type",
                table: "ReferenceAggregateSnapshots",
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
                name: "IX_SnapshotAggregateEvents_PartitionId_AggregateId_PreviousIndex",
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
                name: "ReferenceAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "SimpleAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "BankAccountEvents");

            migrationBuilder.DropTable(
                name: "MockAggregateProjection");

            migrationBuilder.DropTable(
                name: "MockAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "ReferenceAggregateEvents");

            migrationBuilder.DropTable(
                name: "SimpleAggregateEvents");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateEvents");

            migrationBuilder.DropTable(
                name: "MockAggregateEvents");

            migrationBuilder.DropTable(
                name: "EmptyAggregateEvents");
        }
    }
}
