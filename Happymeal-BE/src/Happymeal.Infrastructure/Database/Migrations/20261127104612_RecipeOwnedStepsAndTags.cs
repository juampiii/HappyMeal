using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Happymeal.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecipeOwnedStepsAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeSteps",
                schema: "Happymeal",
                columns: table => new
                {
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
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
                    Name = table.Column<string>(type: "text", nullable: false),
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
                INSERT INTO "Happymeal"."RecipeSteps" ("RecipeId", "Position", "Content")
                SELECT recipe."Id", (step.ordinality - 1)::integer, step.content
                FROM "Happymeal"."Recipes" AS recipe
                CROSS JOIN LATERAL unnest(recipe."Steps") WITH ORDINALITY AS step(content, ordinality);

                INSERT INTO "Happymeal"."RecipeTags" ("RecipeId", "Name")
                SELECT DISTINCT recipe."Id", tag.name
                FROM "Happymeal"."Recipes" AS recipe
                CROSS JOIN LATERAL unnest(recipe."Tags") AS tag(name);
                """);

            migrationBuilder.DropColumn(
                name: "Steps",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "Happymeal",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql("""
                UPDATE "Happymeal"."Recipes" AS recipe
                SET "Steps" = ARRAY(
                        SELECT step."Content"
                        FROM "Happymeal"."RecipeSteps" AS step
                        WHERE step."RecipeId" = recipe."Id"
                        ORDER BY step."Position"),
                    "Tags" = ARRAY(
                        SELECT tag."Name"
                        FROM "Happymeal"."RecipeTags" AS tag
                        WHERE tag."RecipeId" = recipe."Id"
                        ORDER BY tag."Name");
                """);

            migrationBuilder.DropTable(
                name: "RecipeSteps",
                schema: "Happymeal");

            migrationBuilder.DropTable(
                name: "RecipeTags",
                schema: "Happymeal");
        }
    }
}
