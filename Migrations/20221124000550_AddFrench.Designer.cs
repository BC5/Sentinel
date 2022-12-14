// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sentinel;

#nullable disable

namespace Sentinel.Migrations
{
    [DbContext(typeof(Data))]
    [Migration("20221124000550_AddFrench")]
    partial class AddFrench
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.0-rc.2.22472.11");

            modelBuilder.Entity("Sentinel.CensorEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Phrase")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Requirement")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ServerConfigDiscordID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Wildcard")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ServerConfigDiscordID");

                    b.ToTable("CensorEntry");
                });

            modelBuilder.Entity("Sentinel.OCREntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Channel")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("ImageHash")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("ImageURL")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("Message")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Server")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("OcrEntries");
                });

            modelBuilder.Entity("Sentinel.Reaction", b =>
                {
                    b.Property<int>("ReactionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReactName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ReacteeId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReactorId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ServerId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ReactionId");

                    b.ToTable("ReactLog");
                });

            modelBuilder.Entity("Sentinel.ServerConfig", b =>
                {
                    b.Property<ulong>("DiscordID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Cost1984")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CostDe1984")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CostWarn")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan>("DefaultSentence")
                        .HasColumnType("TEXT");

                    b.Property<int>("DeflectorCost")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("FlagChannel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("FrenchCost")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("FunnyCommands")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("IdiotRole")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ModRole")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MuteCost")
                        .HasColumnType("INTEGER");

                    b.Property<int>("NickCost")
                        .HasColumnType("INTEGER");

                    b.Property<float>("RewardChance")
                        .HasColumnType("REAL");

                    b.Property<int>("RewardSize")
                        .HasColumnType("INTEGER");

                    b.HasKey("DiscordID");

                    b.ToTable("Servers");
                });

            modelBuilder.Entity("Sentinel.ServerUser", b =>
                {
                    b.Property<string>("CompositeID")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Authoritative")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Balance")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Censored")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DeflectorExpiry")
                        .HasColumnType("TEXT");

                    b.Property<int>("Earnings")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Francophone")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("IdiotedUntil")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Immune")
                        .HasColumnType("INTEGER");

                    b.Property<float>("Multiplier")
                        .HasColumnType("REAL");

                    b.Property<string>("Nicklock")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("NicklockUntil")
                        .HasColumnType("TEXT");

                    b.Property<string>("PrevNick")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleBackup")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SentinelAttitude")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ServerSnowflake")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserSnowflake")
                        .HasColumnType("INTEGER");

                    b.HasKey("CompositeID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Sentinel.ServerWarns", b =>
                {
                    b.Property<uint>("warnid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("serverid")
                        .HasColumnType("INTEGER");

                    b.Property<string>("warnReason")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("warnTime")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("warned")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("warner")
                        .HasColumnType("INTEGER");

                    b.HasKey("warnid");

                    b.ToTable("Warns");
                });

            modelBuilder.Entity("Sentinel.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Amount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Reason")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("RecipientID")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("SenderID")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ServerID")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Sentinel.Vote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Poll")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("User")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("Sentinel.CensorEntry", b =>
                {
                    b.HasOne("Sentinel.ServerConfig", null)
                        .WithMany("Censor")
                        .HasForeignKey("ServerConfigDiscordID");
                });

            modelBuilder.Entity("Sentinel.ServerConfig", b =>
                {
                    b.Navigation("Censor");
                });
#pragma warning restore 612, 618
        }
    }
}
