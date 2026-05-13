# Food Maker API — Exercise

## Goal

Build a REST API for `FoodMaker` that returns a unified list of ingredients for a requested meal.

The API must:
- support `Breakfast`, `Lunch`, and `Dinner`
- call the correct external provider for the selected meal type
- handle different provider contracts and response formats
- return a simple list of ingredient names
- handle cases where food is not served at the requested time

## Business Scenario

`FoodMaker` is an aggregator API. It does not store meal data on its own.

Instead, it must retrieve ingredients from external provider services:
- `BreakfastAPI`
- `LunchAPI`
- `DinnerAPI`

Each provider:
- serves food only during specific hours
- exposes a different endpoint shape
- returns data in a different response format

Your task is to hide those differences behind one consistent `FoodMaker` API.

## Task

Implement an endpoint in the `FoodMaker` project that prepares a meal based on:
- meal type
- requested time, if provided

If time is not provided, the API should use the current date and time.

You may choose the exact endpoint design, but it should be a clear REST API.

## AI Usage

You may use AI tools while working on this exercise.

If you do, please share:
- the tool or tools you used
- the prompts you used
- which parts of the solution were AI-assisted

## Supported Meal Types

The API must support:
- `Breakfast`
- `Lunch`
- `Dinner`

Meal type matching may be case-insensitive.

## Provider Contracts

### 1. `BreakfastAPI`

**Purpose:** returns breakfast ingredients.

**Endpoint:**
- `GET /ingridients?time=HH:mm`

**Serving hours and ingredients:**

| Time range | Ingredients |
|---|---|
| `07:00 - 09:00` | `bread`, `eggs`, `onion` |
| `09:00 - 11:00` | `bread`, `cheese`, `tomato` |

**Example response:**

```json
{
  "products": [
    { "name": "bread" },
    { "name": "eggs" },
    { "name": "onion" }
  ]
}
```

**Outside serving hours:**
- provider returns an empty `products` collection

---

### 2. `LunchAPI`

**Purpose:** returns lunch ingredients.

**Endpoint:**
- `GET /items?hour=12`

**Serving hours and ingredients:**

| Time range | Ingredients |
|---|---|
| `12:00 - 14:00` | `soup`, `potatoes`, `salad` |
| `14:00 - 17:00` | `pizza` |

**Example response:**

```json
{
  "items": [
    "soup",
    "potatoes",
    "salad"
  ]
}
```

**Outside serving hours:**
- provider returns `400 Bad Request`

---

### 3. `DinnerAPI`

**Purpose:** returns dinner ingredients.

**Endpoint:**
- `GET /components/{hour}`

**Serving hours and ingredients:**

| Time range | Ingredients |
|---|---|
| `18:00 - 20:00` | `steak`, `rice`, `sauce` |
| `20:00 - 22:00` | `pasta`, `cheese`, `wine sauce` |

**Example response:**

```json
{
  "dinnerSet": {
    "components": [
      { "name": "steak" },
      { "name": "rice" },
      { "name": "sauce" }
    ]
  }
}
```

**Outside serving hours:**
- provider returns `400 Bad Request`

## Required Unified Output

`FoodMaker` must transform provider-specific responses into one simple result.

A successful response should contain only ingredient names.

Example:

```json
[
  "bread",
  "eggs",
  "onion"
]
```

Returning an object wrapper is also acceptable if the ingredient list is clearly exposed, for example:

```json
{
  "ingredients": ["bread", "eggs", "onion"]
}
```

## Error Handling

Handle at least the following cases:
- unsupported meal type
- invalid or missing time value
- food not served at the requested time
- downstream provider errors

## Acceptance Criteria

A solution is complete when:
- `FoodMaker` exposes one endpoint for requesting a meal
- the endpoint accepts meal type and optional time
- if time is not provided, the current date and time is used
- the correct provider is called based on meal type
- each provider contract is mapped to a unified ingredient list
- unavailable meal hours are handled correctly
- invalid requests return meaningful errors
- the implementation is clean and easy to extend

## Technical Expectations

The solution should:
- use HTTP calls to communicate with provider APIs
- keep provider-specific mapping isolated from the main endpoint logic
- avoid duplicating parsing and error-handling logic where possible
- be easy to extend with another provider in the future

### Caching

Add caching for provider results.

The caching approach may be simple, but it should:
- avoid repeated downstream calls for identical requests
- be scoped by meal type and requested time
- use a reasonable expiration strategy

### Resilience

Add retry handling for transient downstream failures.

The retry strategy should be conservative and should not mask permanent failures.

## Local Setup

Projects in the solution:
- `BreakfastAPI`
- `LunchAPI`
- `DinnerAPI`
- `FoodMaker`

Default local URLs from `launchSettings.json`:
- `BreakfastAPI`: `http://localhost:5234`
- `LunchAPI`: `http://localhost:5032`
- `DinnerAPI`: `http://localhost:5211`
- `FoodMaker`: `http://localhost:5221`

Run all provider APIs together with `FoodMaker` before testing the final endpoint.

## Example Scenarios

### Example 1
Request:
- meal type: `Breakfast`
- time: `08:00`

Expected result:

```json
["bread", "eggs", "onion"]
```

### Example 2
Request:
- meal type: `Lunch`
- time: `15:00`

Expected result:

```json
["pizza"]
```

### Example 3
Request:
- meal type: `Dinner`
- time: `21:00`

Expected result:

```json
["pasta", "cheese", "wine sauce"]
```

### Example 4
Request:
- meal type: `Breakfast`
- time: `06:30`

Expected result:
- a clear response indicating that breakfast is not served at that time

## Summary

Implement `FoodMaker` as a single API that normalizes three different provider integrations into one consistent meal response.
