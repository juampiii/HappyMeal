using System;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Happymeal.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecipeLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                schema: "Happymeal",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                schema: "Happymeal",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NameKey",
                schema: "Happymeal",
                table: "Recipes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreparationMinutes",
                schema: "Happymeal",
                table: "Recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Servings",
                schema: "Happymeal",
                table: "Recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Steps",
                schema: "Happymeal",
                table: "Recipes",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                schema: "Happymeal",
                table: "Recipes",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "Happymeal",
                table: "Recipes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "Happymeal",
                table: "Recipes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                schema: "Happymeal",
                columns: table => new
                {
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.RecipeId, x.Position });
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "Happymeal",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(NameBackfillSql());

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_NameKey",
                schema: "Happymeal",
                table: "Recipes",
                column: "NameKey",
                unique: true);
        }

        // Freeze the same Unicode decomposition, mark removal and invariant case mapping
        // used at runtime. SQL performs the row update without reading data outside the
        // migration transaction; the unique index aborts the migration on collisions.
        private static string NameBackfillSql()
        {
            var source = new StringBuilder();
            var target = new StringBuilder();
            var marks = new StringBuilder();
            for (var code = 1; code <= 0x10ffff; code++)
            {
                if (!Rune.IsValid(code))
                {
                    continue;
                }
                var rune = new Rune(code);
                if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
                {
                    marks.Append(rune);
                }
                else if (Rune.ToUpperInvariant(rune) != rune)
                {
                    source.Append(rune);
                    target.Append(Rune.ToUpperInvariant(rune));
                }
            }
            var whitespace = string.Concat(Enumerable.Range(1, char.MaxValue).Select(x => (char)x).Where(char.IsWhiteSpace));
            static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
            return "UPDATE \"Happymeal\".\"Recipes\" SET \"NameKey\" = translate(normalize(btrim(\"Name\", "
                + Quote(whitespace) + "), NFD), " + Quote(source.ToString() + marks) + ", " + Quote(target.ToString()) + ");";
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients",
                schema: "Happymeal");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_NameKey",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "NameKey",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "PreparationMinutes",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Servings",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Steps",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "Happymeal",
                table: "Recipes");
        }
    }
}
