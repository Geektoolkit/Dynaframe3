using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynaframe3.Server.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HostName = table.Column<string>(type: "TEXT", nullable: true),
                    Ip = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    LastCheckin = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Shuffle = table.Column<bool>(type: "INTEGER", nullable: false),
                    VideoVolume = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpandDirectoriesByDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    OXMOrientnation = table.Column<string>(type: "TEXT", nullable: true),
                    SearchDirectories = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentPlayList = table.Column<string>(type: "TEXT", nullable: true),
                    InfoBarFontSize = table.Column<int>(type: "INTEGER", nullable: false),
                    InfoBarState = table.Column<int>(type: "INTEGER", nullable: false),
                    FadeTransitionTime = table.Column<int>(type: "INTEGER", nullable: false),
                    SlideshowTransitionTime = table.Column<int>(type: "INTEGER", nullable: false),
                    DateTimeFormat = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeFontFamily = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfSecondsToShowIP = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageStretch = table.Column<int>(type: "INTEGER", nullable: false),
                    VideoStretch = table.Column<string>(type: "TEXT", nullable: true),
                    PlaybackFullVideo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReloadSettings = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefreshDirctories = table.Column<bool>(type: "INTEGER", nullable: false),
                    ListenerPort = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSyncEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScreenStatus = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowInfoDateTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowInfoFileName = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowInfoIP = table.Column<string>(type: "TEXT", nullable: true),
                    ShowEXIFData = table.Column<bool>(type: "INTEGER", nullable: false),
                    HideInfoBar = table.Column<bool>(type: "INTEGER", nullable: false),
                    DynaframeIP = table.Column<string>(type: "TEXT", nullable: true),
                    SlideShowPaused = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemoteClients = table.Column<string>(type: "TEXT", nullable: true),
                    IgnoreFolders = table.Column<string>(type: "TEXT", nullable: true),
                    InclusiveTagFilters = table.Column<string>(type: "TEXT", nullable: true),
                    EnableLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlurBoxSigmaX = table.Column<float>(type: "REAL", nullable: false),
                    BlurBoxSigmaY = table.Column<float>(type: "REAL", nullable: false),
                    BlurBoxMargin = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSettings_Devices_Id",
                        column: x => x.Id,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_Device_Ip",
                table: "Devices",
                column: "Ip");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
