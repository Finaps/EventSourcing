using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventSourcing.Example.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BasketEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    ExpirationTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductRemovedFromBasketEvent_Quantity = table.Column<int>(type: "integer", nullable: true),
                    ProductRemovedFromBasketEvent_ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BasketEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_BasketEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "BasketEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_OrderEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrderEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "OrderEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductEvents",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    PreviousIndex = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "CASE WHEN \"Index\" = 0 THEN NULL ELSE \"Index\" - 1 END", stored: true),
                    ZeroIndex = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "cast(0 as bigint)", stored: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    ProductReservedEvent_Quantity = table.Column<int>(type: "integer", nullable: true),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: true),
                    HeldFor = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ProductSoldEvent_Quantity = table.Column<int>(type: "integer", nullable: true),
                    ProductSoldEvent_BasketId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductStockAddedEvent_Quantity = table.Column<int>(type: "integer", nullable: true),
                    ReservationRemovedEvent_Quantity = table.Column<int>(type: "integer", nullable: true),
                    ReservationRemovedEvent_BasketId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEvents", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_ProductEvents_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductEvents_ConsecutiveIndex",
                        columns: x => new { x.PartitionId, x.AggregateId, x.PreviousIndex },
                        principalTable: "ProductEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BasketSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_BasketSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_BasketSnapshots_BasketEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "BasketEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_OrderSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrderSnapshots_OrderEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "OrderEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSnapshots",
                columns: table => new
                {
                    PartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSnapshots", x => new { x.PartitionId, x.AggregateId, x.Index });
                    table.CheckConstraint("CK_ProductSnapshots_NonNegativeIndex", "\"Index\" >= 0");
                    table.CheckConstraint("CK_ProductSnapshot_NotNull", "NOT \"Type\" = 'ProductSnapshot' OR (\"Quantity\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ProductSnapshots_ProductEvents_PartitionId_AggregateId_Index",
                        columns: x => new { x.PartitionId, x.AggregateId, x.Index },
                        principalTable: "ProductEvents",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    ProductSnapshotPartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSnapshotAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSnapshotIndex = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => new { x.ProductSnapshotPartitionId, x.ProductSnapshotAggregateId, x.ProductSnapshotIndex, x.Id });
                    table.ForeignKey(
                        name: "FK_Reservation_ProductSnapshots_ProductSnapshotPartitionId_Pro~",
                        columns: x => new { x.ProductSnapshotPartitionId, x.ProductSnapshotAggregateId, x.ProductSnapshotIndex },
                        principalTable: "ProductSnapshots",
                        principalColumns: new[] { "PartitionId", "AggregateId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BasketEvents_AggregateType",
                table: "BasketEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BasketEvents_PartitionId_AggregateId_PreviousIndex",
                table: "BasketEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_BasketEvents_Timestamp",
                table: "BasketEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BasketEvents_Type",
                table: "BasketEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_BasketSnapshots_AggregateType",
                table: "BasketSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_BasketSnapshots_Timestamp",
                table: "BasketSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BasketSnapshots_Type",
                table: "BasketSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_AggregateType",
                table: "OrderEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_PartitionId_AggregateId_PreviousIndex",
                table: "OrderEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_Timestamp",
                table: "OrderEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_Type",
                table: "OrderEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSnapshots_AggregateType",
                table: "OrderSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSnapshots_Timestamp",
                table: "OrderSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSnapshots_Type",
                table: "OrderSnapshots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_AggregateType",
                table: "ProductEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_PartitionId_AggregateId_PreviousIndex",
                table: "ProductEvents",
                columns: new[] { "PartitionId", "AggregateId", "PreviousIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_Timestamp",
                table: "ProductEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_Type",
                table: "ProductEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSnapshots_AggregateType",
                table: "ProductSnapshots",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSnapshots_Timestamp",
                table: "ProductSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSnapshots_Type",
                table: "ProductSnapshots",
                column: "Type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasketSnapshots");

            migrationBuilder.DropTable(
                name: "OrderSnapshots");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "BasketEvents");

            migrationBuilder.DropTable(
                name: "OrderEvents");

            migrationBuilder.DropTable(
                name: "ProductSnapshots");

            migrationBuilder.DropTable(
                name: "ProductEvents");
        }
    }
}
