using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        builder.Property(r => r.Steps).HasConversion(
            v => v.ToArray(), v => Array.AsReadOnly(v))
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => Array.AsReadOnly(values.ToArray())));
        builder.Property(r => r.Tags).HasConversion(
            v => v.ToArray(), v => Array.AsReadOnly(v))
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => Array.AsReadOnly(values.ToArray())));
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
