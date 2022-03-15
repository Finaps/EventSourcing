using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventSourcing.EF.Tests.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccountProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Iban = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "EmptyProjection",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmptyProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "EventEntity",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEntity", x => new { x.PartitionId, x.AggregateId, x.Index });
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
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FactoryType = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAggregateProjection", x => new { x.PartitionId, x.AggregateId });
                });

            migrationBuilder.CreateTable(
                name: "SnapshotEntity",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    AggregateType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotEntity", x => new { x.PartitionId, x.AggregateId, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "MockNestedRecordItem",
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
                    table.PrimaryKey("PK_MockNestedRecordItem", x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId, x.Id });
                    table.ForeignKey(
                        name: "FK_MockNestedRecordItem_MockAggregateProjection_MockAggregateP~",
                        columns: x => new { x.MockAggregateProjectionPartitionId, x.MockAggregateProjectionAggregateId },
                        principalTable: "MockAggregateProjection",
                        principalColumns: new[] { "PartitionId", "AggregateId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_AggregateType",
                table: "BankAccountProjection",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountProjection_Hash",
                table: "BankAccountProjection",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_AggregateType",
                table: "EmptyProjection",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_EmptyProjection_Hash",
                table: "EmptyProjection",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_EventEntity_AggregateType",
                table: "EventEntity",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_EventEntity_Timestamp",
                table: "EventEntity",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EventEntity_Type",
                table: "EventEntity",
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
                name: "IX_SnapshotEntity_AggregateType",
                table: "SnapshotEntity",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEntity_Timestamp",
                table: "SnapshotEntity",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEntity_Type",
                table: "SnapshotEntity",
                column: "Type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountProjection");

            migrationBuilder.DropTable(
                name: "EmptyProjection");

            migrationBuilder.DropTable(
                name: "EventEntity");

            migrationBuilder.DropTable(
                name: "MockNestedRecordItem");

            migrationBuilder.DropTable(
                name: "SnapshotEntity");

            migrationBuilder.DropTable(
                name: "MockAggregateProjection");
        }
    }
}
