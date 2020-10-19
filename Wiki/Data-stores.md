The app uses the following data stores:
1. Azure Cosmos DB Account - Teams and Users Collections
1. Application Insights

All these resources are created in your Azure subscription. None are hosted directly by Microsoft.

## Azure Cosmos DB Account

### Teams Collection

The Teams Collection stores the metadata needed to determine which teams are being tracked for pairups by the bot and the metadata needed to fetch the roster and notify the users for each team.

| Value         | Description
| ---           | ---
| Id            | The team's team ID
| TenantId      | The team's tenant ID
| ServiceUrl    | The service URL that can be used to fetch the team's roster
| InstallerName | The best effort name of the person who installed the Icebreaker app to that team

### Users Collection

The Users Collection stores the metadata for users as they opt out/in for pairups.

| Value      | Description
| ---        | ---
| TenantId   | The user's tenant ID
| UserId     | The user's user ID
| OptedIn    | A bool value representing the status of the user's choice to opt out/in for pairups
| ServiceUrl | The user's service URL that can be used to notify the user

> **NOTE**: Users have, by default, a status of being opted "in" for pairups. When in this initial default state, the user's data is not stored in the Users Collection. Their preference is only stored in the collection if they opt out. If, following opting out, the user chooses to opt back in, then their data will remain in the collection, but with an opt in status.

## Application Insights

See [Telemetry](Telemetry)
