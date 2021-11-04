// <copyright file="IceBreakerBotMiddlewareTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.BotTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Icebreaker.Bot;
    using Icebreaker.Interfaces;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json.Linq;
    using Xunit;

    /// <summary>
    /// IceBreakerBotMiddlewareTests
    /// </summary>
    public class IceBreakerBotMiddlewareTests
    {
        private readonly Mock<IAppSettings> appSettings;
        private readonly Mock<ITurnContext> mockContext;
        private readonly TeamsChannelAccount botAccount;
        private readonly TeamsChannelAccount userAccount;
        private readonly TeamsChannelData teamsChannelData;
        private readonly Mock<ILogger<IceBreakerBotMiddleware>> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IceBreakerBotMiddlewareTests"/> class.
        /// </summary>
        public IceBreakerBotMiddlewareTests()
        {
            this.appSettings = new Mock<IAppSettings>();
            this.mockContext = new Mock<ITurnContext>();
            this.userAccount = new TeamsChannelAccount { Id = Guid.NewGuid().ToString(), Properties = JObject.FromObject(new { Id = Guid.NewGuid().ToString() }) };
            this.botAccount = new TeamsChannelAccount { Id = "bot", Properties = JObject.FromObject(new { Id = "bot" }) };
            this.teamsChannelData = new TeamsChannelData
            {
                Team = new TeamInfo
                {
                    Id = "TeamId",
                },
                Tenant = new TenantInfo(),
            };
            this.logger = new Mock<ILogger<IceBreakerBotMiddleware>>();
        }

        [Fact]
        public async Task Middleware_CurrentTenantAllowed_NextDelegateGetsInvoked()
        {
            var tenantGuid = Guid.NewGuid().ToString();

            var appInstalledActivity = new Activity
            {
                MembersAdded = new List<ChannelAccount>
                {
                    this.botAccount,
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                From = this.userAccount,
                ChannelData = this.teamsChannelData,
                Conversation = new ConversationAccount(tenantId: tenantGuid),
            };

            // Tenant filter is active
            var currAppSettings = this.appSettings;
            currAppSettings.Setup(x => x.AllowedTenantIds).Returns(() => new HashSet<string> { tenantGuid });
            currAppSettings.Setup(x => x.DisableTenantFilter).Returns(() => false);

            // Tenant filtering in the middleware; throws an exception for failures.
            var middleware = new IceBreakerBotMiddleware(currAppSettings.Object, this.logger.Object);
            var nextDelegate = new Mock<NextDelegate>();
            this.mockContext.Setup(x => x.Activity).Returns(() => appInstalledActivity);

            await middleware.OnTurnAsync(this.mockContext.Object, nextDelegate.Object);
            var inv = nextDelegate.Invocations;
            Assert.Equal(1, inv.Count);
        }

        [Fact]
        public async Task Middleware_CurrentTenantIsEmpty_NextDelegateNotInvoked()
        {
            var appInstalledActivity = new Activity
            {
                MembersAdded = new List<ChannelAccount>
                {
                    this.botAccount,
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                From = this.userAccount,
                ChannelData = this.teamsChannelData,
                Conversation = new ConversationAccount(tenantId: null),
            };

            var currAppSettings = this.appSettings;
            currAppSettings.Setup(x => x.AllowedTenantIds).Returns(() => null);
            currAppSettings.Setup(x => x.DisableTenantFilter).Returns(() => false);
            this.mockContext.Setup(x => x.Activity).Returns(() => appInstalledActivity);

            // Tenant filtering in the middleware; throws an exception for failures.
            var middleware = new IceBreakerBotMiddleware(currAppSettings.Object, this.logger.Object);
            var nextDelegate = new Mock<NextDelegate>();

            await middleware.OnTurnAsync(this.mockContext.Object, nextDelegate.Object);
            var inv = nextDelegate.Invocations;
            Assert.Equal(0, inv.Count);
        }

        [Fact]
        public async Task Middleware_TenantFilterDisabled_ByPassesTenantChecks()
        {
            var currAppSettings = this.appSettings;
            currAppSettings.Setup(x => x.DisableTenantFilter).Returns(() => true);
            this.mockContext.Setup(x => x.Activity).Returns(() => new Mock<Activity>().Object);
            var middleware = new IceBreakerBotMiddleware(currAppSettings.Object, this.logger.Object);
            var nextDelegate = new Mock<NextDelegate>();

            await middleware.OnTurnAsync(this.mockContext.Object, nextDelegate.Object);
            var inv = nextDelegate.Invocations;
            Assert.Equal(1, inv.Count);
        }

        [Fact]
        public async Task Middleware_NoActivityTurnContext_ThrowsException()
        {
            var currAppSettings = this.appSettings;
            currAppSettings.Setup(x => x.DisableTenantFilter).Returns(() => true);
            var middleware = new IceBreakerBotMiddleware(currAppSettings.Object, this.logger.Object);
            var nextDelegate = new Mock<NextDelegate>();

            await Assert.ThrowsAsync<NullReferenceException>(async () => await middleware.OnTurnAsync(this.mockContext.Object, nextDelegate.Object));
        }
    }
}
