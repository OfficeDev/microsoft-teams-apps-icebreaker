## Assumptions

The estimate below assumes:

-   Icebreaker performs user matches once in a week
-   400 RUs used across all Azure Cosmos DB collections

## [](/wiki/costestimate#sku-recommendations)SKU recommendations

The recommended SKUs for a production environment are:

-   Azure Cosmos DB: Pay as you go (default 400 provisioned RUs)
-   App Service: Standard (S1)
-   Azure Bot Service: S1 (includes unlimited messages for Microsoft Teams channel)

## [](/wiki/costestimate#estimated-load)Estimated load

**Data storage**: 1 GB max

**Logic Apps**: 1 action executions per week

## [](/wiki/costestimate#estimated-cost)Estimated cost

**IMPORTANT:**  This is only an estimate, based on the assumptions above. Your actual costs may vary.

Prices were taken from the  [Azure Pricing Overview](https://azure.microsoft.com/en-us/pricing/)  on 23 December 2019, for the West US region.

Use the  [Azure Pricing Calculator](https://azure.com/e/ecf1f0efa694499cb0b6b8ac2b466b5a)  to model different service tiers and usage patterns.


|  Resource |  Tier |  Load |  Monthly price |
|---|---|---|---|
| Azure Cosmos DB| Single region write, Pay as you go |< 1GB storage, 400 RUs| $23.61 |
|  Azure Bot Service | S1  |  N/A | $0  |
|  App Service Plan | S1  | 730 hours  | $73.00  |
|  Logic Apps| -|1 action execution / 7 day(s)  | $0.01 |
|  Azure Monitor (Application Insights) | -  |  < 1GB data | (free up to 5 GB)|
|**Total**|||**$96.62**|