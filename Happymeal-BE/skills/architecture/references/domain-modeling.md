# Domain modeling preferences

## Aggregate boundaries and owned elements

Model related domain concepts explicitly when their meaning or rules make a
separate type clearer than a primitive collection. Keep the aggregate root as
the entry point for changes: it validates the complete input before replacing
state and enforces lifecycle rules for all its elements.

For recipes, `Recipe` is the aggregate root. `RecipeIngredient`, `RecipeStep`
and `RecipeTag` belong to that recipe and have no independent lifecycle or
repository. Construct them within the aggregate, keep setters private and expose
collections through read-only views. Do not add globally unique child identifiers
when identity within the owning recipe is sufficient.

- A step has `Position` and `Content`. Its identity for persistence is the recipe
  plus its position. Keep positions explicit and order the public step view by
  position; database row order is never a domain guarantee.
- A tag has `Name`. Its identity for persistence is the recipe plus its name.
  Tags have no preparation order. Ignore blank tags and trim names. Deduplicate
  with `OrdinalIgnoreCase`, retaining the first spelling; accents remain distinct.
  A shared tag catalog is a separate business capability.
- `UnitOfMeasure` is an immutable value object identified by its valid code.
  Ownership of a collection element does not imply that every value inside it
  needs entity identity or its own table.

These decisions do not require turning every string or primitive list into an
entity. Choose a value object, owned element or primitive based on its domain
meaning, invariants and lifecycle. Keep dependencies on EF out of Domain.

Validate text lengths and collection counts in Domain before constructing owned
elements, and configure matching column lengths in Infrastructure. Recipe limits
are 100 ingredients (names up to 200 characters), 100 steps (content up to 2,000)
and 20 distinct nonempty tags (names up to 50). Limit the incoming tag list to 100
entries before ignoring blanks or duplicates. Keep limits as named constants.
Domain validation measures the trimmed text which will actually be stored.

Hiding a recipe releases its name. Visible names remain unique under the recipe
collation, enforced by both the repository precheck and a partial unique index
excluding hidden rows. Hidden recipes remain accessible by ID. `Version` stays 1
until creating a new version is implemented; draft edits do not increment it.

## Persistence should follow the domain clearly

Prefer straightforward native EF mappings when they express the chosen model.
For recipe steps and tags, use `OwnsMany` with owner-scoped keys and backing
fields, rather than converting entire lists and writing custom equality, hash
and snapshot expressions. A conversion remains appropriate for an individual
value object such as `UnitOfMeasure`; this is not a ban on converters.

An owned-content-only edit must retain aggregate concurrency protection, audit
stamping and transactional outbox behavior. Verify replacements that reuse child
keys in the same tracked context. When changing storage from primitive arrays to
owned rows, migrate existing contents before removing columns and preserve step
order in both upgrade and downgrade.
