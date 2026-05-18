using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "Theories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "Tests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ParentSectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderBy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Sections_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Theories_SectionId",
                table: "Theories",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_SectionId",
                table: "Tests",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_OrderBy",
                table: "Sections",
                column: "OrderBy");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_ParentSectionId",
                table: "Sections",
                column: "ParentSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_Sections_SectionId",
                table: "Tests",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Theories_Sections_SectionId",
                table: "Theories",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tests_Sections_SectionId",
                table: "Tests");

            migrationBuilder.DropForeignKey(
                name: "FK_Theories_Sections_SectionId",
                table: "Theories");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Theories_SectionId",
                table: "Theories");

            migrationBuilder.DropIndex(
                name: "IX_Tests_SectionId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Theories");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Tests");
        }
    }
}
