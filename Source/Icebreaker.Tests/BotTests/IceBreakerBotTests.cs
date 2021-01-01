using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Icebreaker.Bot;
using Icebreaker.Helpers;
using Icebreaker.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Icebreaker.Tests.BotTests
{
    public class IceBreakerBotTests
    {
        private readonly IcebreakerBot sut;
        private readonly TestAdapter botAdapter;
        private readonly TeamsChannelAccount userAccount, botAccount;
        private readonly TeamsChannelData teamsChannelData;
        private readonly Mock<IBotDataProvider> dataProvider;

        public IceBreakerBotTests()
        {
            this.botAdapter = new TestAdapter(Channels.Msteams)
            {
                Conversation =
                {
                    Conversation = new ConversationAccount
                    {
                        ConversationType = "channel"
                    }
                }
            };
            var telemetryClient = new TelemetryClient();
            var conversationHelper = new ConversationHelperMock();
            this.dataProvider = new Mock<IBotDataProvider>();
            this.dataProvider.Setup(x => x.GetInstalledTeamAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult(new TeamInstallInfo()));
            this.sut = new IcebreakerBot(dataProvider.Object, conversationHelper, MicrosoftAppCredentials.Empty, telemetryClient);
            this.userAccount = new TeamsChannelAccount { Id = Guid.NewGuid().ToString(), Properties = JObject.FromObject(new { Id = Guid.NewGuid().ToString() }) };
            this.botAccount = new TeamsChannelAccount { Id = "bot", Properties = JObject.FromObject(new { Id = "bot" }) };
            this.teamsChannelData = new TeamsChannelData
            {
                Team = new TeamInfo
                {
                    Id = "TeamId"
                },
                Tenant = new TenantInfo()
            };
        }


        [Fact]
        public async Task NewBotMessage_OnAppInstalled_SenderIsNotATeam_NoReply()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                MembersAdded = new List<ChannelAccount>
                {
                    this.botAccount
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = "msteams",
                Recipient = this.botAccount,
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            var reply = (IMessageActivity)this.botAdapter.GetNextReply();
            
            // Assert that no reply is received.
            Assert.Null(reply);
        }


        [Fact]
        public async Task NewBotMessage_OnAppInstalled_ReturnsTeamWelcomeCard()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                MembersAdded = new List<ChannelAccount>
                {
                    this.botAccount
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            this.botAdapter.GetNextReply();

            // Assert UpdateTeamInstallStatusAsync is called once
            dataProvider.Verify(m => m.UpdateTeamInstallStatusAsync(It.IsAny<TeamInstallInfo>(), true),
                Times.Exactly(1));
        }

        [Fact]
        public async Task NewBotMessage_OnMemberAdded_ReturnsUserWelcomeCard()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                MembersAdded = new List<ChannelAccount>
                {
                    this.userAccount
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            this.botAdapter.GetNextReply();

            // Assert GetInstalledTeamAsync is called once
            dataProvider.Verify(m => m.GetInstalledTeamAsync(It.IsAny<string>()),
                Times.Exactly(1));
        }

        [Fact]
        public async Task NewBotMessage_OnMemberOptOut_ReturnsSuccessMessage()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                Text = MatchingActions.OptOut,
                Type = ActivityTypes.Message,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                From = this.userAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            // Assert that we received 1 hero card with 1 button included (OptIn).
            var reply = (IMessageActivity)this.botAdapter.GetNextReply();
            Assert.Equal(ActivityTypes.Message, reply.Type);
            Assert.Equal(1, reply.Attachments.Count);
            Assert.Equal(HeroCard.ContentType, reply.Attachments.First().ContentType);
            Assert.Equal(1, ((HeroCard)reply.Attachments.First().Content).Buttons.Count);

            // Assert SetUserInfoAsync is called once with optIn param = false
            dataProvider.Verify(
                m => m.SetUserInfoAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<string>()),
                Times.Exactly(1));
        }


        [Fact]
        public async Task NewBotMessage_OnMemberOptIn_ReturnsSuccessMessage()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                Text = MatchingActions.OptIn,
                Type = ActivityTypes.Message,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                From = this.userAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            // Assert that we received 1 hero card with 1 button included (OptOut).
            var reply = (IMessageActivity)this.botAdapter.GetNextReply();
            Assert.Equal(ActivityTypes.Message, reply.Type);
            Assert.Equal(1, reply.Attachments.Count);
            Assert.Equal(HeroCard.ContentType, reply.Attachments.First().ContentType);
            Assert.Equal(1, ((HeroCard)reply.Attachments.First().Content).Buttons.Count);

            // Assert SetUserInfoAsync is called once with optIn param = true
            dataProvider.Verify(
                m => m.SetUserInfoAsync(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<string>()),
                Times.Exactly(1));
        }


        [Fact]
        public async Task NewBotMessage_OnMemberRemoved_BotIgnoreMessage()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                MembersRemoved = new List<ChannelAccount>
                {
                    this.userAccount
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            this.botAdapter.GetNextReply();

            // Assert GetInstalledTeamAsync is not called as bot ignores this case
            dataProvider.Verify(m => m.GetInstalledTeamAsync(It.IsAny<string>()),
                Times.Exactly(0));
        }


        [Fact]
        public async Task NewBotMessage_OnTeamRemoved_TeamUpdatedInDb()
        {
            // Arrange: Create activity
            var appInstalledActivity = new Activity
            {
                MembersRemoved = new List<ChannelAccount>
                {
                    this.botAccount
                },
                Type = ActivityTypes.ConversationUpdate,
                ChannelId = Channels.Msteams,
                Recipient = this.botAccount,
                ChannelData = this.teamsChannelData
            };

            // Act
            // Send the message activity to the bot.
            await this.botAdapter.ProcessActivityAsync(appInstalledActivity, this.sut.OnTurnAsync, CancellationToken.None);

            this.botAdapter.GetNextReply();

            // Assert UpdateTeamInstallStatusAsync is called once
            dataProvider.Verify(m => m.UpdateTeamInstallStatusAsync(It.IsAny<TeamInstallInfo>(), false),
                Times.Exactly(1));
        }
    }
}
