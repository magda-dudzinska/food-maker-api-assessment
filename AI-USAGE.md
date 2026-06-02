# AI Usage Documentation

## Introduction

This document records every prompt used during the AI-assisted development of the FoodMaker API aggregator. The solution was built incrementally using **Test-Driven Development (TDD)** with Claude Code as the AI pair-programming assistant.

The development followed a strict red-green-refactor cycle: tests were written first (red), then the minimum implementation to pass them (green), followed by refactoring guided by review prompts. Key architectural decisions — such as adopting the Result pattern over exceptions, introducing the Strategy pattern, and adding caching as a decorator — emerged through this iterative conversation.

**Tech stack:** .NET 10, xUnit, NSubstitute, Moq, Shouldly, Polly, Microsoft.Extensions.Caching.Memory

---

## Prompts

### 1. Project Scaffolding

> I'm starting the FoodMaker project in a .NET 10 solution. The solution already contains BreakfastAPI, LunchAPI, DinnerAPI (provider stubs) and FoodMaker (the aggregator I'm building).
>
> Generate:
> 1. A FoodMaker.Tests xUnit project file (net10.0) referencing xUnit, NSubstitute, Shouldly, Microsoft.AspNetCore.Mvc.Testing, Microsoft.Extensions.Http.
> 2. Folder layout inside FoodMaker: Controllers, Domain (value objects, exceptions), Providers, Services, Infrastructure (DI, caching, resilience, error mapping).
> 3. An .editorconfig with nullable enabled, file-scoped namespaces, var preferences, and standard C# defaults.
>
> No business logic. Scaffolding only.

### 2. Domain Types

> Generate these domain types for FoodMaker (net10.0, nullable enabled, file-scoped namespaces):
>
> 1. enum MealType { Breakfast, Lunch, Dinner }
> 2. Static MealTypeParser.TryParse(string input, out MealType result), case-insensitive.
> 3. Exceptions, all deriving from a base FoodMakerException:
>    - MealNotServedException(MealType type, TimeOnly time)
>    - UnsupportedMealTypeException(string requested)
>    - ProviderUnavailableException(MealType type, Exception inner)
>
> Each exception should carry its data as properties (not just in the message). No other code. No interfaces, no providers.

### 3. IMealProvider Interface

> Generate this interface in FoodMaker.Providers:
>
> ```csharp
> public interface IMealProvider {
>     MealType MealType { get; }
>     Task<IReadOnlyList<string>> GetIngredientsAsync(
>         TimeOnly time, CancellationToken ct);
> }
> ```
>
> Interface only. No implementations.

### 4. BreakfastProvider Tests (Red Step)

> I want to use TDD. First will be red step. I have IMealProvider interface. I will implement BreakfastProvider next. It uses a typed HttpClient named "Breakfast" (BaseAddress configured in DI). It calls:
>
> GET /ingridients?time=HH:mm
>
> 200 response: { "products": [{"name":"bread"}, {"name":"eggs"}] }
> Outside serving hours: 200 with { "products": [] }
> 5xx: infrastructure failure.
>
> Write xUnit tests using Moq for HttpMessageHandler and Shouldly. Test ONLY these behaviors, one test per behavior, AAA structure:
>
> 1. Maps products[].name to ingredient names on 200 OK
> 2. Returns empty list when products array is empty
> 3. Sends `time` query parameter formatted as HH:mm (e.g. "08:00")
> 4. Throws OperationCanceledException when token is pre-cancelled
> 5. Throws ProviderUnavailableException on 500
> 6. MealType property returns MealType.Breakfast
>
> Do not write BreakfastProvider. Do not add tests beyond this list.

### 5. Add Test Project to Solution

> add test project to the solution

### 6. Naming Conventions Feedback

> I don't want to use sut, I want clear name of what is tested, I also don't want to use _ in field names

### 7. Empty BreakfastProvider (Stub)

> create empty BreakfastProvider it should implement IMealProvider

### 8. BreakfastProvider Implementation (Green Step)

> The tests in BreakfastProviderTests are red. Implement BreakfastProvider so exactly these tests pass.
>
> Constraints:
> - System.Text.Json with explicit [JsonPropertyName] attributes
> - DTOs as sealed private records in separate files
> - Manual mapping (no AutoMapper)
> - Catch HttpRequestException -> wrap in ProviderUnavailableException
> - Do not catch OperationCanceledException
> - No caching, no retry -- those come later

### 9. Remove AAA Comments

> don't use AAA comments

### 10. BreakfastProvider Code Review

> Review for BreakfastProvider: (a) mutability, (b) cancellation token propagation through every async call, (c) overly broad exception handling, (d) extractions that would meaningfully improve readability. Focus on overall code readability, KISS and YAGNI

### 11. Full Parameter Names

> for variables and params use full names, I want good readability, no need for guessing

### 12. Apply Review Suggestions

> apply changes suggested for review

### 13. Move DTOs to Responses Folder

> move DTOs to separate folder, name it Responses and remove Dto from names

### 14. LunchProvider Tests (Red Step)

> Same TDD pattern. LunchProvider implements IMealProvider. It calls:
>
> GET /items?hour=12 (hour as integer 0-23)
>
> 200 response: { "items": ["soup", "potatoes", "salad"] }
> Outside serving hours: 400 Bad Request (no guaranteed body).
>
> xUnit + Moq + Shouldly. Test ONLY these behaviors:
>
> 1. Maps items array to ingredient names on 200 OK
> 2. Sends `hour` query parameter as integer derived from TimeOnly.Hour
> 3. Throws MealNotServedException on 400 -- domain signal, NOT infra failure
> 4. Throws ProviderUnavailableException on 500
> 5. Throws OperationCanceledException when token is pre-cancelled
> 6. MealType property returns MealType.Lunch
>
> Implement only empty LunchProvider.

### 15. LunchProvider Implementation (Green Step)

> now implement LunchProvider against these failing tests.
>
> Same constraints as BreakfastProvider. Critical: 400 -> MealNotServedException (domain), 5xx -> ProviderUnavailableException (infra). The retry policy (added later) must be able to tell these apart.

### 16. LunchProvider Review

> review implementation in the same way as for BreakfastProvider

### 17. DinnerProvider Tests (Red Step)

> DinnerProvider implements IMealProvider. It calls:
>
> GET /components/{hour} (hour in URL path)
>
> 200 response:
> { "dinnerSet": { "components": [{"name":"steak"}, {"name":"rice"}] } }
>
> Outside serving hours: 400 Bad Request.
>
> xUnit + Moq + Shouldly. Test ONLY:
>
> 1. Maps dinnerSet.components[].name to ingredient names on 200 OK
> 2. Sends hour as URL path segment (e.g. /components/21)
> 3. Throws MealNotServedException on 400
> 4. Throws ProviderUnavailableException on 500
> 5. Throws OperationCanceledException when token is pre-cancelled
> 6. MealType property returns MealType.Dinner
> 7. Defensive: missing dinnerSet or components -> empty list, no NRE
>
> Implement DinnerProvider empty so it builds.

### 18. DinnerProvider Implementation (Green Step)

> Implement DinnerProvider, same constraints. Watch null handling on the nested response shape -- test 7 exists specifically to catch a NullReferenceException.

### 19. Extract Methods for Readability

> introduce more methods to improve readability in each provider

### 20. Result Pattern Introduction

> instead of using exceptions it would be better to introduce Results, so result would provide details about outcomes of the request and not throwing exception

*Follow-up decisions via interactive prompts:*
- Result type: ProviderResult (specific, not generic)
- Cancellation: OperationCanceledException still propagates as exception

### 21. Handle HttpRequestException with Results

> I still see throwing HttpRequestException can we also handle it with results?

### 22. Remove Null Return from SendRequest

> why it returns null? it should still return Result

### 23. Strategy Pattern Discussion

> would it make sense to have one provider class with common logic and use strategy for specific provider?

### 24. Strategy Pattern Implementation

> let's try it

### 25. Folder Structure Cleanup

> why results are in domain and responses in providers? should be remove domain and have only providers?

### 26. Strategy Classes Instead of Config

> I would prefer configs as separate strategy classes

### 27. Organize into Subfolders

> add results and strategies folders into providers, move files accordingly

### 28. IHttpClientFactory Introduction

> instead of HttpClient in HttpMealProvider introduce IHttpClientFactory which will provide correct httpclient for given provider strategy

### 29. Remove Redundant HttpClientName

> do we need HttpClientName, wouldn't it be better to reuse ProviderName?

### 30. Rename to MealProvider

> HttpMealProvider -> MealProvider

### 31. MealService Tests (Red Step)

> Create tests for MealService which dispatches by MealType. It receives IEnumerable<IMealProvider> and ILogger<MealService>.
> It selects the correct provider by matching provider.MealType to the requested mealType.
>
> Use: xUnit + NSubstitute + Shouldly.
>
> Tests only, create empty MealService class.
>
> Scenarios to cover:
>
> - Given Breakfast provider and Lunch provider registered -- when mealType is Breakfast -- calls Breakfast provider and returns its ingredients
> - Given Breakfast provider and Lunch provider registered -- when mealType is Lunch -- calls Lunch provider and returns its ingredients
> - Given provider returns ProviderResult.Failure with ProviderErrorKind.MealNotServed -- service should propagate that error appropriately
> - Given provider returns ProviderResult.Failure with ProviderErrorKind.ProviderUnavailable -- service should propagate that error
> - Given no provider matches the requested mealType -- should throw ArgumentException (or appropriate exception)
> - Given cancelled CancellationToken -- throws OperationCanceledException
> - Verify the correct provider is called -- the non-matching provider should not be called

### 32. MealService Implementation (Green Step)

> now provide implementation which will make this tests green

### 33. UnsupportedMealType as Result

> don't throw ArgumentException, use UnsupportedMealType error as ProviderResult

### 34. Remove Logger

> we probably don't need logger at this point

### 35. MealTypeParser Tests

> Create tests for MealTypeParser. Use xUnit + Shouldly.
>
> "Breakfast" -> returns true, result is MealType.Breakfast
> "lunch" (lowercase) -> returns true, result is MealType.Lunch (case-insensitive)
> "DINNER" (uppercase) -> returns true, result is MealType.Dinner
> "Brunch" -> returns false (unsupported meal type)
> "" (empty string) -> returns false
> "123" (numeric but valid enum int) -> returns false (Enum.IsDefined guard)
>
> Use [Theory] with [InlineData] for the valid and invalid cases where it makes sense.

### 36. Integration Tests for Endpoint

> Create integration tests for the FoodMaker API endpoint.
>
> Use WebApplicationFactory<Program> to test the full HTTP pipeline. Override DI to replace IMealProvider registrations with NSubstitute mocks.
>
> Use xUnit + NSubstitute + Shouldly + Microsoft.AspNetCore.Mvc.Testing. Tests only -- do not create the endpoint.
>
> Endpoint design assumption: GET /meals/{mealType}?time=HH:mm -- returns JSON array of ingredient strings.
>
> Scenarios:
>
> - GET /meals/Breakfast?time=08:00 with provider returning ProviderResult.Success(["bread", "eggs"]) -> 200 with ["bread", "eggs"]
> - GET /meals/lunch?time=13:00 (lowercase) -> 200 -- meal type matching is case-insensitive
> - GET /meals/Dinner (no time param) -> 200 -- service uses current time as default
> - GET /meals/Brunch?time=08:00 -> 400 with meaningful error message (unsupported meal type)
> - GET /meals/Breakfast?time=invalid -> 400 (invalid time format)
> - Provider returns ProviderResult.Failure with MealNotServed -> 404 with message including meal type and time
> - Provider returns ProviderResult.Failure with ProviderUnavailable -> 502 (bad gateway)
> - Provider returns ProviderResult.Failure with UnsupportedMealType -> 400

### 37. DI/Service Registration Tests

> Create tests for DI/service registration.
>
> Use WebApplicationFactory<Program> to verify the DI container is wired correctly.
>
> Stack: xUnit + Shouldly. Tests only.
>
> Scenarios:
>
> - Resolving IEnumerable<IMealProvider> returns exactly 3 providers
> - The resolved providers cover all three MealType values: Breakfast, Lunch, Dinner
> - Resolving MealService does not throw (all dependencies satisfied)
> - Each named HttpClient is registered with the correct base address from configuration:
>   - Breakfast -> http://localhost:5234
>   - Lunch -> http://localhost:5032
>   - Dinner -> http://localhost:5211

### 38. Endpoint Implementation (Green Step)

> Make all tests in endpoint tests pass. create minimal api endpoint in Program.
>
> Behavior:
> - Parse mealType string via MealTypeParser.TryParse -> if false, return BadRequest with error message
> - Parse time string via TimeOnly.TryParse -> if invalid, return BadRequest
> - If time is null, use TimeOnly.FromDateTime(DateTime.Now)
> - Call mealService.GetIngredientsAsync(mealType, time, cancellationToken)
> - Map result:
>   - Success -> Ok(result.Ingredients)
>   - MealNotServed -> NotFound with error message
>   - ProviderUnavailable -> Bad Gateway with error message
>   - UnsupportedMealType -> BadRequest with error message

### 39. DI Registration Implementation (Green Step)

> Make all Service Registration tests pass.
>
> Services to register:
> - MealService as scoped/transient
> - Three IMealProvider registrations (one per meal type), each constructed from:
>   - The matching IMealProviderStrategy
>   - A named HttpClient via IHttpClientFactory
> - Named HttpClient registrations with base addresses from configuration (appsettings.json):
>   - "Breakfast" -> http://localhost:5234
>   - "Lunch" -> http://localhost:5032
>   - "Dinner" -> http://localhost:5211
> - Add controllers support
> - Also create appsettings.json with provider URLs in a config section.

### 40. Clean Up Program.cs

> clean up Program. create extensions methods for registration to make it easier to read

### 41. CachedMealProvider Tests (Red Step)

> Create tests for cached meal provider behavior in FoodMaker.Tests/Providers/CachedMealProviderTests.cs.
>
> Design: CachedMealProvider is a decorator over IMealProvider. It wraps an inner provider, caches successful results by (MealType, TimeOnly) key, and delegates to the inner provider on cache miss.
>
> Use: xUnit + NSubstitute + Shouldly. Tests only -- do not create the implementation.
>
> Use a real MemoryCache -- not a mock. It's lightweight and avoids brittle mock setups.
>
> Scenarios:
>
> - First call for (Breakfast, 08:00) -> delegates to inner provider, returns its result
> - Second call for same (Breakfast, 08:00) -> does NOT call inner provider again, returns cached result
> - Call for (Breakfast, 08:00) then (Breakfast, 09:00) -> calls inner provider twice (different time = different cache key)
> - Inner provider returns ProviderResult.Failure -> result is NOT cached, next call delegates again
> - MealType property returns the inner provider's MealType
> - Cancelled token -> throws OperationCanceledException, does not cache

### 42. CachedMealProvider Implementation (Green Step)

> Make all tests in CachedMealProviderTests pass. Create CachedMealProvider.
>
> Behavior:
> - Decorator over IMealProvider
> - Cache key: combination of MealType and TimeOnly
> - Only cache successful results (IsSuccess == true)
> - Use IMemoryCache with a reasonable expiration (e.g. 5 minutes absolute)
> - Delegate MealType to inner provider
> - Check cancellation before cache lookup
> - Update Program.cs DI to wrap each IMealProvider with CachedMealProvider. Register IMemoryCache via builder.Services.AddMemoryCache().

### 43. Retry Policy with Polly

> Add Microsoft.Extensions.Http.Polly package
> On each named HttpClient, add a retry policy for transient HTTP errors (HttpRequestException, 5xx, 408)
> Retry up to 2 times with short delay with WaitAndRetryAsync with exponential backoff
> Do not retry 400 Bad Request - that's MealNotServed, a permanent/business failure

### 44. Add Jitter to Retry

> use exponential backoff with jitter

### 45. Increase Retry Count

> change retries to 3

### 46. Documentation

> create AI-USAGE.md with all prompts used in creating this solution and some short introduction

### 47. Unified Behaviour for Outside Serving Hours

> why if I have GET {{host}}/meals/breakfast I get 200, but if I do GET {{host}}/meals/lunch I get 404. it is 17:40

*This led to a discussion about inconsistent aggregator behaviour: Breakfast returned 200 with empty array (upstream returns empty products), while Lunch returned 404 (upstream returns 400). The decision was to unify: all meal types return 404 when outside serving hours.*

> yeah, but I build aggregate so all types of meal should behave the same way for all users of FoodMaker API

*Interactive decision: Always 404 with message for outside serving hours.*

*Implementation: Added `TreatEmptyResponseAsMealNotServed` to `IMealProviderStrategy`. Breakfast sets it to `true` so empty 200 responses become `MealNotServed` failures, matching the 400-based behaviour of Lunch and Dinner.*

### 48. Update Documentation

> update AI-USAGE.md for new prompts

### 49. Simplify Empty Response Handling

> remove TreatEmptyResponseAsMealNotServed and always treat is as meal not served for all strategies

*Removed the per-strategy flag. `MealProvider` now unconditionally treats empty ingredient lists as `MealNotServed` — simpler and consistent across all providers. Updated Dinner defensive tests accordingly.*

### 50. Update Documentation

> update AI-USAGE.md
