using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Happymeal.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ManageRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.happymeal_recipe_name", "und-u-ks-level1,und-u-ks-level1,icu,False");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Happymeal",
                table: "Recipes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                collation: "happymeal_recipe_name",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

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
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RecipeSteps",
                schema: "Happymeal",
                columns: table => new
                {
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeSteps", x => new { x.RecipeId, x.Position });
                    table.ForeignKey(
                        name: "FK_RecipeSteps_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "Happymeal",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeTags",
                schema: "Happymeal",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeTags", x => new { x.RecipeId, x.Name });
                    table.ForeignKey(
                        name: "FK_RecipeTags_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "Happymeal",
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE "Happymeal"."Recipes" SET "Name" = btrim("Name");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                schema: "Happymeal",
                table: "Recipes",
                column: "Name",
                unique: true,
                filter: "\"IsHidden\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients",
                schema: "Happymeal");

            migrationBuilder.DropTable(
                name: "RecipeSteps",
                schema: "Happymeal");

            migrationBuilder.DropTable(
                name: "RecipeTags",
                schema: "Happymeal");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Name",
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
                name: "PreparationMinutes",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Servings",
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

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Happymeal",
                table: "Recipes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldCollation: "happymeal_recipe_name");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:CollationDefinition:public.happymeal_recipe_name", "und-u-ks-level1,und-u-ks-level1,icu,False");
        }
    }
}
