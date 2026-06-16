using Xunit;

// WebApplicationFactory hosts (each with its own BackgroundService run executor) are heavy and
// timing-sensitive; run the API integration tests serially to avoid boot/CPU contention flakiness.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
