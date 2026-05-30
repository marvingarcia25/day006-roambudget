# RoamBudget v2 — Spec

## What changed
Complete pivot: group expense splitter → personal trip budget tracker.
Dropped: members, splits, settlement algorithm.
Added: multiple trips, categories, per-category budgets, budget overview.

## Design tokens (from mockup image)
| Token | Value |
|---|---|
| Navy | `#1B3B5C` |
| Teal | `#14A89E` |
| Teal-tint | `#E2F4F2` |
| Progress bar | `linear-gradient(90deg, #28B16D, #14A89E)` |
| Gray text | `#8B96A2` |
| Card bg | `#FFFFFF` |
| Page bg | `#F0F4F8` |
| Card shadow | `0 4px 20px rgba(0,0,0,.06)` |
| Heading font | Poppins 600/700 |
| Body font | Inter 400/500/600 |
| Card radius | 18px |
| Pill radius | 24px |

## Data model

### Category enum
Food=0, Accommodation=1, Transport=2, Activities=3, Shopping=4, Other=5

### Trip
```
Guid        Id
string      Name
string      DateRange        // free text e.g. "May 20 – May 27"
decimal     TotalBudget
Dictionary<Category, decimal> CategoryBudgets   // optional per-category sub-budgets
List<Expense> Expenses
int         CoverIndex       // 0–7 → gradient palette
DateTime    CreatedAt
```

### Expense
```
Guid        Id
string      Description
decimal     Amount
Category    Category
DateTime    Date
```

## API
| Method | Route | Body / Response |
|---|---|---|
| GET | /api/trips | → TripSummaryDto[] |
| POST | /api/trips | CreateTripRequest → TripSummaryDto |
| DELETE | /api/trips/{id} | 204 |
| GET | /api/trips/{id}/expenses | → TripDetailDto |
| POST | /api/trips/{id}/expenses | AddExpenseRequest → TripDetailDto |
| DELETE | /api/trips/{id}/expenses/{eid} | → TripDetailDto |
| GET | /api/trips/{id}/budget | → BudgetOverviewDto |
| PUT | /api/trips/{id}/category-budgets | CategoryBudgetsRequest → 200 |

## Screens (4-tab bottom nav)

### Tab 1 — Trips
- Header: "My Trips" + "+ New Trip" teal pill button
- Scrollable list of trip cards:
  - Gradient cover (140 px, 8 palettes)
  - Overlapping white info panel: name, date range, "$X of $Y" + thin progress bar + "XX%"
- Tap card → sets active trip + switches to Expenses tab
- New Trip modal: Name, Date Range (text), Total Budget, optional per-category budgets

### Tab 2 — Expenses
- Active trip sub-header (name + total spent badge)
- "+ Add Expense" teal button
- Expense list rows: 40px teal-tint icon circle · description/date stack · bold amount
- Add Expense modal: description, amount, category select, date (defaults today)

### Tab 3 — Budget
- Trip dropdown selector
- "Total Budget $X,XXX.XX" large
- Chart.js donut (center label: "XX%\nSpent")
- Spent / Remaining stat pair
- Categories section: icon · name · inline progress bar · "$spent / $budget" · "%"

### Tab 4 — Profile
- App logo + tagline
- Stat row: Total Trips, Total Expenses, Total Spent
- Last 5 expenses across all trips

## Category icons & colors
| Category | Icon | Tint bg |
|---|---|---|
| Food | 🍽️ | #FFF4E6 |
| Accommodation | 🏨 | #E6F4FF |
| Transport | 🚗 | #EFF6FF |
| Activities | 🎭 | #F4F0FF |
| Shopping | 🛍️ | #FFF0F9 |
| Other | 💼 | #F0F4F8 |

## Cover gradients (CoverIndex)
0: teal→blue  1: purple→pink  2: amber→red  3: green→cyan
4: blue→purple  5: teal→navy  6: orange→red  7: green→blue
