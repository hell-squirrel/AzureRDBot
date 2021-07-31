using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using MuskBot.Models;
using RestSharp;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class MuskBot : ActivityHandler
    {
        private readonly IOptions<AuthConfig> _authConfig;
        public MuskBot(IOptions<AuthConfig> config)
        {
            _authConfig = config;
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var selectedActivity = turnContext.Activity.Text;
            switch (selectedActivity.ToLower())
            {
                case "info":
                    var accountInfo = GetInfo();
                    _ =  turnContext.SendActivityAsync(MessageFactory.Text(accountInfo), cancellationToken).Result;
                    break;
                case "tweets":
                    GetLastTweets(turnContext, cancellationToken);
                    break;
                default:
                    break;
            }

            var reply = GetActions();
            _ = turnContext.SendActivityAsync(reply, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome to Elon Musk bot!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    _ = turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken).Result;
                    var reply = GetActions();
                    _ = turnContext.SendActivityAsync(reply, cancellationToken).Result;
                }
            }
        }

        private Activity GetActions()
        {
            var message = MessageFactory.Text("Select action");
            message.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Info", Type = ActionTypes.ImBack, Value = "info" },
                    new CardAction() { Title = "Tweets", Type = ActionTypes.ImBack, Value = "tweets" }
                },
            };
            return message;
        }

        private string GetInfo()
        {
            var client = new RestClient("https://api.twitter.com/2/users?ids=44196397&user.fields=public_metrics,profile_image_url");
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", $"Bearer {_authConfig.Value.BearerToken}");
            var response = client.Execute(request);

            var output = JsonSerializer.Deserialize<ElonInfoViewModel>(response.Content);
            var info = output.Data.Single();
            return $"{info.Name} account follower count is {info.PublicMetrics.FollowersCount} and tweet count is {info.PublicMetrics.TweetCount}.";
        }

        private void GetLastTweets(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var client = new RestClient("https://api.twitter.com/2/users/44196397/tweets?max_results=15");
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", $"Bearer {_authConfig.Value.BearerToken}");
            var response = client.Execute(request);

            var output = JsonSerializer.Deserialize<TweetSetModel>(response.Content);
            try
            {
                for (int i = 0; i < output.Payload.Length; i++)
                {
                    var tweet = output.Payload[i];
                    _ = turnContext.SendActivityAsync(MessageFactory.Text(tweet.Text), cancellationToken).Result;
                }
            }
            catch (Exception)
            {
                _ = turnContext.SendActivityAsync(MessageFactory.Text("Something went wrong with this request =("), cancellationToken).Result;
            }
        }
    }
}
