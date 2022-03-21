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
                    Name = table.Column<string>(type: "text", nullable: true),
                    Iban = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    DebtorAccount = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditorAccount = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "BankAccountProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Iban = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountProjection", x => new { x.PartitionId, x.AggregateId });
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
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    SomeString = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "EmptyAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "EmptyProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
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
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockString = table.Column<string>(type: "text", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockEnum = table.Column<int>(type: "integer", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: true),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "MockAggregateProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockString = table.Column<string>(type: "text", nullable: true),
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockDouble = table.Column<double>(type: "double precision", nullable: false),
                    MockEnum = table.Column<int>(type: "integer", nullable: false),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: false),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: false),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: false),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: false),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: false),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection", x => new { x.PartitionId, x.AggregateId });
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
                    MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockEnum = table.Column<int>(type: "integer", nullable: true),
                    MockFlagEnum = table.Column<byte>(type: "smallint", nullable: true),
                    MockNestedRecord_MockBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    MockNestedRecord_MockString = table.Column<string>(type: "text", nullable: true),
                    MockNestedRecord_MockDecimal = table.Column<decimal>(type: "numeric", nullable: true),
                    MockNestedRecord_MockDouble = table.Column<double>(type: "double precision", nullable: true),
                    MockFloatList = table.Column<List<float>>(type: "real[]", nullable: true),
                    MockStringSet = table.Column<List<string>>(type: "text[]", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "SimpleAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "SnapshotAggregateEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "SnapshotAggregateSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotAggregateSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
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
                name: "IX_BankAccountSnapshots_Timestamp",
                table: "BankAccountSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountSnapshots_Type",
                table: "BankAccountSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_Timestamp",
                table: "EmptyAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyAggregateEvents_Type",
                table: "EmptyAggregateEvents",
                column: "Type");

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
                name: "IX_MockAggregateSnapshots_Timestamp",
                table: "MockAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MockAggregateSnapshots_Type",
                table: "MockAggregateSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_Timestamp",
                table: "SimpleAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateEvents_Type",
                table: "SimpleAggregateEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateSnapshots_Timestamp",
                table: "SimpleAggregateSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleAggregateSnapshots_Type",
                table: "SimpleAggregateSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_Timestamp",
                table: "SnapshotAggregateEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotAggregateEvents_Type",
                table: "SnapshotAggregateEvents",
                column: "Type");

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
                name: "BankAccountEvents");

            migrationBuilder.DropTable(
                name: "BankAccountProjection");

            migrationBuilder.DropTable(
                name: "BankAccountSnapshots");

            migrationBuilder.DropTable(
                name: "EmptyAggregateEvents");

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
                name: "SimpleAggregateEvents");

            migrationBuilder.DropTable(
                name: "SimpleAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateEvents");

            migrationBuilder.DropTable(
                name: "SnapshotAggregateSnapshots");

            migrationBuilder.DropTable(
                name: "MockAggregateEvents");

            migrationBuilder.DropTable(
                name: "MockAggregateProjection");

            migrationBuilder.DropTable(
                name: "MockAggregateSnapshots");
        }
    }
}
