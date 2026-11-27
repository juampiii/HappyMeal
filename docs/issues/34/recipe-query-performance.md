# Recipe query performance evaluation

Evaluated on 2026-11-27 against backend commit `7b27301`. This is a local
diagnostic of the agreed owned-collection model, not a production load test.
No application query behavior was changed as part of this evaluation.

## Implementation decision after evaluation

On 2026-11-27, the user approved applying `AsSplitQuery()` only to the existing
`GET /api/recipes` listing, through `RecipeRepository.GetAllAsync`. Keep its
no-tracking behavior, visibility filter, ordering and full response contract.
Leave `GetAsync`, the detail endpoint and the global query policy unchanged.
The evaluation results below describe the baseline before this scoped change.
The listing retains read-committed consistency across its separate statements;
this change adds no transaction or snapshot guarantee.

Implemented after approval. Solution build with enforced style and all 42
PostgreSQL integration tests passed, including existing listing visibility,
owned-content ordering and detail checks. Only the listing query was changed.

## Recommendation

- Use `AsSplitQuery()` for the current full-aggregate recipe listing. The measured
  joins multiply rows across ingredients, steps and tags; the improvement is
  substantial at both 50 and 500 recipes.
- Do not enable split queries globally or automatically change the shared
  `RecipeRepository.GetAsync`. For a small detail, single query is faster; the
  typical detail improvement is modest and sensitive to database latency. Large
  details benefit substantially on the measured local topology.
- If changing the full detail GET to split queries, make the read policy explicit:
  it currently shares a tracked repository method with update/publish/hide
  commands. A dedicated no-tracking read path can be considered, but was not
  measured here. Decide whether cross-statement consistency is acceptable or
  whether a repeatable-read transaction is needed before changing behavior.
- Later, use a paginated summary projection for cards if the UI does not need
  complete recipes. Split queries reduce duplication but still load every owned
  element and do not bound the size of the existing unpaginated endpoint.

## Method

An external temporary console harness referenced the existing Infrastructure
project and used the real `HappymealDbContext`, migrations, model, domain objects
and `RecipeDto.From`. No production packages or benchmark packages were added.

- .NET SDK 10.0.404, Release build; EF Core 10.0.13 and Npgsql EF provider 10.0.3.
- PostgreSQL 18.6 (`postgres:18-alpine`), isolated disposable container, accessed
  from Windows through a Docker Desktop loopback published port.
- A fresh context per measured operation, connection pooling and
  `EnableRetryOnFailure()` as in the application. Detail queries used tracking;
  listings used `AsNoTracking()` and `OrderBy(Name)`, matching the repository.
- Equivalent repository LINQ plus DTO materialization was measured. This did not
  include HTTP middleware, JSON serialization, outbox processing or concurrent
  traffic. The listing harness materialized DTOs into an array rather than the
  handler's list; both variants used exactly the same harness operation.
- Each case used five warmups per variant, then 25 measured operations per
  variant, alternating which variant ran first. Median and empirical p95 (24th
  sorted observation of 25) are reported; p95 is indicative with this sample size.
- Each scenario reseeded the isolated database. Descriptions contained 300
  characters and step text approximately 107 characters. Cases were homogeneous
  synthetic examples, not a measured distribution of household recipes.
- SQL commands were captured by an EF interceptor, then replayed and fully read
  outside the timed samples to count actual result rows. Split command count
  remained four regardless of the number of recipes: this was not N+1 loading.
- Allocation figures are approximate process-wide managed bytes allocated during
  one operation, measured using `GC.GetTotalAllocatedBytes`. They are not peak
  live memory, retained memory or database memory.

## Results without artificial delay

The collection sizes are ingredients / steps / tags per recipe.

| Scenario | Sizes | Single median / p95, ms | Split median / p95, ms | Actual rows, single / split | Allocated MiB, single / split |
| --- | --- | --- | --- | --- | --- |
| Detail, small draft | 2 / 1 / 0 | 1.42 / 3.96 | 3.05 / 3.90 | 2 / 4 | 0.07 / 0.09 |
| Detail, typical | 10 / 6 / 4 | 4.41 / 9.46 | 3.23 / 4.90 | 240 / 21 | 0.73 / 0.11 |
| Detail, large | 20 / 15 / 8 | 29.61 / 42.64 | 3.08 / 4.52 | 2,400 / 44 | 7.14 / 0.14 |
| Listing, 50 recipes | 10 / 6 / 4 | 147.40 / 204.68 | 6.90 / 12.62 | 12,000 / 1,050 | 33.84 / 0.84 |
| Listing, 500 recipes | 10 / 6 / 4 | 1,556.69 / 1,874.60 | 31.61 / 38.54 | 120,000 / 10,500 | 324.29 / 7.91 |

The observed single query joined `Recipes` to `RecipeIngredients`, `RecipeSteps`
and `RecipeTags` through three sibling `LEFT JOIN`s. A typical recipe therefore
produced `10 * 6 * 4 = 240` rows. Split query issued one root query and three
owner-key joins returning `1 + 10 + 6 + 4 = 21` rows in total. Missing collections
do not eliminate the root in the single-query case because these are left joins.
Owned collections were loaded automatically even though the repository only
explicitly includes ingredients.

## Latency sensitivity

A separate synthetic scenario inserted `Task.Delay(5)` before every SQL command.
Windows timer granularity made this approximately a 15 ms delay, not a true
5 ms network RTT. An independent 25-sample delay probe measured median 15.34 ms
and p95 16.10 ms. These numbers illustrate sensitivity only; they are not a
simulation of a specific deployment network.

| Scenario | Single median / p95, ms | Split median / p95, ms |
| --- | --- | --- |
| Small detail | 15.70 / 19.13 | 62.72 / 65.27 |
| Typical detail | 15.84 / 21.71 | 60.83 / 66.53 |
| Listing, 500 recipes | 1,420.59 / 1,484.68 | 73.59 / 99.26 |

For details, three extra roundtrips can outweigh a small materialization saving.
The listing benefit survived this coarse delay scenario. Do not subtract the
undelayed and delayed medians to estimate RTT: run-to-run runtime/OS variation
also exists, as the lower delayed single-list median demonstrates.

## Consistency and scope of a future change

PostgreSQL's default read-committed isolation gives each statement its own
snapshot. A split read can combine root data and collections from different
committed revisions when another operation updates a draft between statements.
`AsNoTracking()` does not address that. A transaction at the default isolation
level does not provide one snapshot for all four statements either. PostgreSQL
repeatable-read can provide that snapshot, with its own costs and retry behavior;
those transaction costs were not benchmarked here.

Existing aggregate writes use the root `xmin` token. `UnitOfWork` marks the root
modified when owned-content changes raise domain events, so a concurrently
committed aggregate change after the first read should still fail the eventual
stale write. That protects persistence but does not make a split GET response a
consistent snapshot. This evaluation did not re-test concurrency behavior or
change that policy.

The measured allocation amplification is particularly large because the existing
retry-enabled configuration buffers query results. Absolute results will differ
with deployment latency, recipe lengths, collection distributions, CPU, cache
state and load. No production performance target was supplied, and the local
typical-detail difference is too small to claim a universal latency improvement.

No database-model regression is implied: the aggregate can retain owned elements
while choosing suitable query strategies. Implement and verify any selected
query change separately from this evaluation.

## References

- [Recipe repository](../../../Happymeal-BE/src/Happymeal.Infrastructure/Repositories/RecipeRepository.cs)
- [Owned collection mappings](../../../Happymeal-BE/src/Happymeal.Infrastructure/Database/Configurations/RecipeConfiguration.cs)
- [Single versus split queries, including consistency, roundtrips and buffering](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [EF Core efficient querying and retry buffering](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [Concurrency tokens and PostgreSQL repeatable-read isolation](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
