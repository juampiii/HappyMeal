using Happymeal.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Happymeal.Infrastructure.Database.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasMaxLength(Recipe.NameMaxLength)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(Recipe.DescriptionMaxLength);
        builder.Property(r => r.NameKey).IsRequired();
        builder.HasIndex(r => r.NameKey).IsUnique();
        builder.Property(r => r.Revision).IsRowVersion();
        builder.OwnsMany(r => r.Steps, steps =>
        {
            steps.ToTable("RecipeSteps");
            steps.WithOwner().HasForeignKey("RecipeId");
            steps.HasKey("RecipeId", nameof(RecipeStep.Position));
            steps.Property(step => step.Position).ValueGeneratedNever();
            steps.Property(step => step.Content).IsRequired();
        });
        builder.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(r => r.Tags, tags =>
        {
            tags.ToTable("RecipeTags");
            tags.WithOwner().HasForeignKey("RecipeId");
            tags.HasKey("RecipeId", nameof(RecipeTag.Name));
            tags.Property(tag => tag.Name).IsRequired();
        });
        builder.Navigation(r => r.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(r => r.Ingredients, ingredients =>
        {
            ingredients.ToTable("RecipeIngredients");
            ingredients.WithOwner().HasForeignKey("RecipeId");
            ingredients.HasKey("RecipeId", nameof(RecipeIngredient.Position));
            ingredients.Property(x => x.Position).ValueGeneratedNever();
            ingredients.Property(x => x.Name).IsRequired();
            ingredients.Property(x => x.Quantity).HasColumnType("numeric");
            ingredients.Property(x => x.Unit)
                .HasConversion(
                    unit => unit!.Code,
                    code => UnitOfMeasure.Create(code).Value)
                .HasColumnType("text")
                .IsRequired(false);
        });
        builder.Navigation(r => r.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
