using Xunit;

// AppSettings (I7) is session state — an options menu has to be one mutable thing, not one per
// thread. xunit would otherwise run test classes in parallel and a settings toggle in one class
// could race a harness boot in another (and the state hash covers the assist flag). The suite
// runs in well under a second, so deterministic beats parallel: every test here is sequential.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
