﻿// <auto-generated />
using System;
using InkingNewsstand.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InkingNewsstand.Models.Migrations
{
    [DbContext(typeof(InkingNewsstandDbContext))]
    [Migration("20200101074208_CreateInitial")]
    partial class CreateInitial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0");

            modelBuilder.Entity("InkingNewsstand.Models.ArticleModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<string>("FeedId")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("InkStrikes")
                        .HasColumnType("BLOB");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PublishDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.Property<string>("Titile")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("FeedId");

                    b.ToTable("Articles");
                });

            modelBuilder.Entity("InkingNewsstand.Models.FeedModel", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Icon")
                        .HasColumnType("BLOB");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Feeds");
                });

            modelBuilder.Entity("InkingNewsstand.Models.ArticleModel", b =>
                {
                    b.HasOne("InkingNewsstand.Models.FeedModel", "Feed")
                        .WithMany("Articles")
                        .HasForeignKey("FeedId");
                });
#pragma warning restore 612, 618
        }
    }
}