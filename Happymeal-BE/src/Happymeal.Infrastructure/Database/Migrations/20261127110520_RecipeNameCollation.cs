using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Happymeal.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecipeNameCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_NameKey",
                schema: "Happymeal",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "NameKey",
                schema: "Happymeal",
                table: "Recipes");

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

            // Existing rows may predate domain trimming. Use the .NET whitespace
            // characters so the persisted name follows the same boundary rule.
            migrationBuilder.Sql("""
                UPDATE "Happymeal"."Recipes"
                SET "Name" = btrim("Name", U&'\0009\000A\000B\000C\000D\0020\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                schema: "Happymeal",
                table: "Recipes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_Name",
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

            migrationBuilder.AddColumn<string>(
                name: "NameKey",
                schema: "Happymeal",
                table: "Recipes",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Restore the historical representation only when rolling back.
            migrationBuilder.Sql(RecipeLifecycle.NameBackfillSql());

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_NameKey",
                schema: "Happymeal",
                table: "Recipes",
                column: "NameKey",
                unique: true);
        }
    }
}
