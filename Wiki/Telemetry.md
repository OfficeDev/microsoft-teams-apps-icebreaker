The Teams Bot web app logs telemetry to [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor/). You can go to the Application Insights blade of the Azure App Service to view basic telemetry about your services, such as requests, failures, and dependency errors.

The Teams Bot integrates with Application Insights to gather bot activity analytics, as described [here](https://blog.botframework.com/2019/03/21/bot-analytics-behind-the-scenes/).

The Teams Bot logs a few kinds of custom events:

The `Activity` event:
* Basic activity info: `ActivityId`, `ActivityType`, `Event Name`
* Basic user info: `From ID`

The `UserActivity` event:
* Basic activity info: `ActivityId`, `ActivityType`, `Event Name`
* Basic user info: `UserAadObjectId`
* Context of how it was invoked: `ConversationType`, `TeamId`

The `ProcessedPairups` event:
* Basic activity info: `PairsNotifiedCount`, `UsersNotifiedCount`, `InstalledTeamsCount`, `Event Name`

From this information you can calculate key metrics:
* Which teams (team IDs) have the Icebreaker app?
* How many users are being paired up with the Icebreaker app?
