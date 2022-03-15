﻿// <auto-generated />
using System;
using EventSourcing.EF.Tests.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EventSourcing.EF.Tests.SqlServer.Migrations
{
    [DbContext(typeof(SqlServerTestContext))]
    partial class SqlServerTestContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("EventSourcing.Core.Tests.BankAccountProjection", b =>
                {
                    b.Property<Guid>("PartitionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AggregateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AggregateType")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FactoryType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Iban")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("PartitionId", "AggregateId");

                    b.HasIndex("AggregateType");

                    b.HasIndex("Hash");

                    b.ToTable("BankAccountProjection");
                });

            modelBuilder.Entity("EventSourcing.Core.Tests.Mocks.EmptyProjection", b =>
                {
                    b.Property<Guid>("PartitionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AggregateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AggregateType")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FactoryType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("PartitionId", "AggregateId");

                    b.HasIndex("AggregateType");

                    b.HasIndex("Hash");

                    b.ToTable("EmptyProjection");
                });

            modelBuilder.Entity("EventSourcing.Core.Tests.Mocks.MockAggregateProjection", b =>
                {
                    b.Property<Guid>("PartitionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AggregateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AggregateType")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FactoryType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("MockBoolean")
                        .HasColumnType("bit");

                    b.Property<decimal>("MockDecimal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<double>("MockDouble")
                        .HasColumnType("float");

                    b.Property<int>("MockEnum")
                        .HasColumnType("int");

                    b.Property<byte>("MockFlagEnum")
                        .HasColumnType("tinyint");

                    b.Property<byte[]>("MockFloatList")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("MockString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MockStringSet")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("PartitionId", "AggregateId");

                    b.HasIndex("AggregateType");

                    b.HasIndex("Hash");

                    b.ToTable("MockAggregateProjection");
                });

            modelBuilder.Entity("EventSourcing.EF.EventEntity", b =>
                {
                    b.Property<Guid>("PartitionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AggregateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Index")
                        .HasColumnType("bigint");

                    b.Property<string>("AggregateType")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Json")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("PartitionId", "AggregateId", "Index");

                    b.HasIndex("AggregateType");

                    b.HasIndex("Timestamp");

                    b.HasIndex("Type");

                    b.ToTable("EventEntity");
                });

            modelBuilder.Entity("EventSourcing.EF.SnapshotEntity", b =>
                {
                    b.Property<Guid>("PartitionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AggregateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Index")
                        .HasColumnType("bigint");

                    b.Property<string>("AggregateType")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Json")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("PartitionId", "AggregateId", "Index");

                    b.HasIndex("AggregateType");

                    b.HasIndex("Timestamp");

                    b.HasIndex("Type");

                    b.ToTable("SnapshotEntity");
                });

            modelBuilder.Entity("EventSourcing.Core.Tests.Mocks.MockAggregateProjection", b =>
                {
                    b.OwnsOne("EventSourcing.Core.Tests.Mocks.MockNestedRecord", "MockNestedRecord", b1 =>
                        {
                            b1.Property<Guid>("MockAggregateProjectionPartitionId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("MockAggregateProjectionAggregateId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<bool>("MockBoolean")
                                .HasColumnType("bit");

                            b1.Property<decimal>("MockDecimal")
                                .HasColumnType("decimal(18,2)");

                            b1.Property<double>("MockDouble")
                                .HasColumnType("float");

                            b1.Property<string>("MockString")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("MockAggregateProjectionPartitionId", "MockAggregateProjectionAggregateId");

                            b1.ToTable("MockAggregateProjection");

                            b1.WithOwner()
                                .HasForeignKey("MockAggregateProjectionPartitionId", "MockAggregateProjectionAggregateId");
                        });

                    b.OwnsMany("EventSourcing.Core.Tests.Mocks.MockNestedRecordItem", "MockNestedRecordList", b1 =>
                        {
                            b1.Property<Guid>("MockAggregateProjectionPartitionId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("MockAggregateProjectionAggregateId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("Id"), 1L, 1);

                            b1.Property<bool>("MockBoolean")
                                .HasColumnType("bit");

                            b1.Property<decimal>("MockDecimal")
                                .HasColumnType("decimal(18,2)");

                            b1.Property<double>("MockDouble")
                                .HasColumnType("float");

                            b1.Property<string>("MockString")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("MockAggregateProjectionPartitionId", "MockAggregateProjectionAggregateId", "Id");

                            b1.ToTable("MockNestedRecordItem");

                            b1.WithOwner()
                                .HasForeignKey("MockAggregateProjectionPartitionId", "MockAggregateProjectionAggregateId");
                        });

                    b.Navigation("MockNestedRecord")
                        .IsRequired();

                    b.Navigation("MockNestedRecordList");
                });
#pragma warning restore 612, 618
        }
    }
}