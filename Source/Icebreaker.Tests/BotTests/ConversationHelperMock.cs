using System.Threading;
using System.Threading.Tasks;
using Icebreaker.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema.Teams;

namespace Icebreaker.Tests.BotTests
{
    /// <summary>
    /// Provide fake implementation to static methods call in BotFramework
    /// </summary>
    public class ConversationHelperMock : ConversationHelper
    {
        public ConversationHelperMock() : base(MicrosoftAppCredentials.Empty, new TelemetryClient())
        {
            
        }

        public override Task<TeamsChannelAccount> GetMemberAsync(ITurnContext turnContext, string memberId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TeamsChannelAccount());
        }
    }
}