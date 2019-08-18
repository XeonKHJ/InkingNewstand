﻿// <auto-generated />
using System;
using InkingNewsstand.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InkingNewsstand.Model.Migrations
{
    [DbContext(typeof(InkingNewsstandContext))]
    partial class InkingNewsstandContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("InkingNewsstand.Model.Feed", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Icon");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.ToTable("Feeds");
                });

            modelBuilder.Entity("InkingNewsstand.Model.News", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Authors");

                    b.Property<string>("Content");

                    b.Property<string>("CoverUrl");

                    b.Property<string>("ExtendedHtml");

                    b.Property<string>("FeedId");

                    b.Property<byte[]>("InkStrokes");

                    b.Property<string>("InnerHTML");

                    b.Property<bool>("IsFavorite");

                    b.Property<string>("NewsLink");

                    b.Property<DateTimeOffset>("PublishedDate");

                    b.Property<string>("Summary");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.HasIndex("FeedId");

                    b.ToTable("News");
                });

            modelBuilder.Entity("InkingNewsstand.Model.NewsPaper", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("ExtendMode");

                    b.Property<string>("IconType");

                    b.Property<string>("PaperTitle");

                    b.HasKey("Id");

                    b.ToTable("NewsPapers");
                });

            modelBuilder.Entity("InkingNewsstand.Model.NewsPaper_Feed", b =>
                {
                    b.Property<string>("FeedId");

                    b.Property<int>("NewsPaperId");

                    b.HasKey("FeedId", "NewsPaperId");

                    b.HasIndex("NewsPaperId");

                    b.ToTable("NewsPaper_Feeds");
                });

            modelBuilder.Entity("InkingNewsstand.Model.News", b =>
                {
                    b.HasOne("InkingNewsstand.Model.Feed", "Feed")
                        .WithMany("News")
                        .HasForeignKey("FeedId");
                });

            modelBuilder.Entity("InkingNewsstand.Model.NewsPaper_Feed", b =>
                {
                    b.HasOne("InkingNewsstand.Model.Feed", "Feed")
                        .WithMany("NewsPaper_Feeds")
                        .HasForeignKey("FeedId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("InkingNewsstand.Model.NewsPaper", "NewsPaper")
                        .WithMany("NewsPaper_Feeds")
                        .HasForeignKey("NewsPaperId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
