﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sentinel;

#nullable disable

namespace Sentinel.Migrations
{
    [DbContext(typeof(Data))]
    partial class DataModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Sentinel.AutoResponse", b =>
                {
                    b.Property<int>("ResponseId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Chance")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastTrigger")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("RateLimit")
                        .HasColumnType("tinyint(1)");

                    b.Property<TimeSpan?>("ReloadTime")
                        .HasColumnType("time(6)");

                    b.Property<string>("ResponseEmote")
                        .HasColumnType("longtext");

                    b.Property<string>("ResponseText")
                        .HasColumnType("longtext");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("TargetChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("TargetUser")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Trigger")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("Wildcard")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("ResponseId");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("AutoResponse");
                });

            modelBuilder.Entity("Sentinel.CensorEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Phrase")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("Requirement")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("Wildcard")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("CensorEntry");
                });

            modelBuilder.Entity("Sentinel.ElectionBallot", b =>
                {
                    b.Property<int>("BallotId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Ballot")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("ElectionId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("VoterId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("BallotId");

                    b.ToTable("Ballots");
                });

            modelBuilder.Entity("Sentinel.ModLog", b =>
                {
                    b.Property<int>("ModerationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Action")
                        .HasColumnType("int");

                    b.Property<ulong>("ModId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("UserId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("ModerationId");

                    b.HasIndex("ServerId", "UserId");

                    b.ToTable("ModLogs");
                });

            modelBuilder.Entity("Sentinel.OCREntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("Channel")
                        .HasColumnType("bigint unsigned");

                    b.Property<byte[]>("ImageHash")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<string>("ImageURL")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("Message")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Server")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("OcrEntries");
                });

            modelBuilder.Entity("Sentinel.PurgeConfiguration", b =>
                {
                    b.Property<int>("ConfigurationID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("ChannelID")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTimeOffset>("LastPurge")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("ConfigurationID");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("PurgeConfiguration");
                });

            modelBuilder.Entity("Sentinel.QuoteEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("QuoteEntry");
                });

            modelBuilder.Entity("Sentinel.ReactBoardConfig", b =>
                {
                    b.Property<int>("ReactConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Reaction")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("bigint unsigned");

                    b.Property<uint>("Threshold")
                        .HasColumnType("int unsigned");

                    b.HasKey("ReactConfigId");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("ReactBoardConfig");
                });

            modelBuilder.Entity("Sentinel.Reaction", b =>
                {
                    b.Property<int>("ReactionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("ReactName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("ReacteeId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ReactorId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("ReactionId");

                    b.ToTable("ReactLog");
                });

            modelBuilder.Entity("Sentinel.ServerConfig", b =>
                {
                    b.Property<ulong>("DiscordID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("ApprovalMessage")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ArrivalMessage")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong?>("ArrivalsChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("Cost1984")
                        .HasColumnType("int");

                    b.Property<int>("CostDe1984")
                        .HasColumnType("int");

                    b.Property<int>("CostWarn")
                        .HasColumnType("int");

                    b.Property<string>("DefaultRoles")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<long>("DefaultSentence")
                        .HasColumnType("bigint");

                    b.Property<int>("DeflectorCost")
                        .HasColumnType("int");

                    b.Property<int>("FactcheckCost")
                        .HasColumnType("int");

                    b.Property<ulong?>("FlagChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("FrenchChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("FrenchCost")
                        .HasColumnType("int");

                    b.Property<bool>("FunnyCommands")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong?>("GeneralChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("IdiotChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("IdiotRole")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("LogChannel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("ModRole")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("MuteCost")
                        .HasColumnType("int");

                    b.Property<int>("NickCost")
                        .HasColumnType("int");

                    b.Property<float>("RewardChance")
                        .HasColumnType("float");

                    b.Property<int>("RewardSize")
                        .HasColumnType("int");

                    b.Property<int>("SlotsFee")
                        .HasColumnType("int");

                    b.Property<int>("SlotsPayout")
                        .HasColumnType("int");

                    b.HasKey("DiscordID");

                    b.ToTable("Servers");
                });

            modelBuilder.Entity("Sentinel.ServerUser", b =>
                {
                    b.Property<string>("CompositeID")
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("Authoritative")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Balance")
                        .HasColumnType("int");

                    b.Property<bool>("Censored")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("DeflectorExpiry")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Earnings")
                        .HasColumnType("int");

                    b.Property<bool>("Francophone")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("IdiotedUntil")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Immune")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Juvecheck")
                        .HasColumnType("tinyint(1)");

                    b.Property<float>("Multiplier")
                        .HasColumnType("float");

                    b.Property<string>("Nicklock")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("NicklockUntil")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PrevNick")
                        .HasColumnType("longtext");

                    b.Property<string>("RoleBackup")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("ServerSnowflake")
                        .HasColumnType("bigint unsigned");

                    b.Property<long>("SocialCredit")
                        .HasColumnType("bigint");

                    b.Property<ulong>("UserSnowflake")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("Verified")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("CompositeID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Sentinel.ServerWarns", b =>
                {
                    b.Property<uint>("warnid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int unsigned");

                    b.Property<ulong>("serverid")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("warnReason")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("warnTime")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("warned")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("warner")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("warnid");

                    b.ToTable("Warns");
                });

            modelBuilder.Entity("Sentinel.SocialCreditEntry", b =>
                {
                    b.Property<int>("EntryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<long>("Points")
                        .HasColumnType("bigint");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("UserId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("EntryId");

                    b.ToTable("SocialCreditLog");
                });

            modelBuilder.Entity("Sentinel.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Amount")
                        .HasColumnType("int");

                    b.Property<int>("Reason")
                        .HasColumnType("int");

                    b.Property<ulong>("RecipientID")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("SenderID")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ServerID")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Sentinel.Vote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("Poll")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("User")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("Sentinel.AutoResponse", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("AutoResponses")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.CensorEntry", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("Censor")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.PurgeConfiguration", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("PurgeConfig")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.QuoteEntry", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("Quotes")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.ReactBoardConfig", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("ReactBoards")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.ServerConfig", b =>
                {
                    b.Navigation("AutoResponses");

                    b.Navigation("Censor");

                    b.Navigation("PurgeConfig");

                    b.Navigation("Quotes");

                    b.Navigation("ReactBoards");
                });
#pragma warning restore 612, 618
        }
    }
}
