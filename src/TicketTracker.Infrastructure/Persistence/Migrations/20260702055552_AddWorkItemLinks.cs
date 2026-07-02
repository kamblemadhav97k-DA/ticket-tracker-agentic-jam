using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketLinks_Tickets_SourceTicketId",
                        column: x => x.SourceTicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketLinks_Tickets_TargetTicketId",
                        column: x => x.TargetTicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ParentId",
                table: "Tickets",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLinks_SourceTicketId_TargetTicketId",
                table: "TicketLinks",
                columns: new[] { "SourceTicketId", "TargetTicketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketLinks_TargetTicketId",
                table: "TicketLinks",
                column: "TargetTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Tickets_ParentId",
                table: "Tickets",
                column: "ParentId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Tickets_ParentId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketLinks");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ParentId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Tickets");
        }
    }
}
