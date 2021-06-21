using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hathor.Faucet.Web.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReverseDns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhoisOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HathorTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Address",
                table: "WalletTransactions",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_IpAddress",
                table: "WalletTransactions",
                column: "IpAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");
        }
    }
}
