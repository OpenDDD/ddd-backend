using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using DDD.Functions.Extensions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace DDD.Functions
{
    public static class GetPrizeDraw
    {
        [FunctionName("GetPrizeDraw")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindFeedbackConfig]
            FeedbackConfig feedbackConfig)
        {
            var (conferenceFeedbackRepo, sessionFeedbackRepo) = await feedbackConfig.GetRepositoryAsync();
            var conferenceFeedback = await conferenceFeedbackRepo.GetAllAsync(conference.ConferenceInstance);
            var sessionFeedback = await sessionFeedbackRepo.GetAllAsync(conference.ConferenceInstance);
            
            string [] prizeDraw;
            if (feedbackConfig.IsSingleVoteEligibleForPrizeDraw)
            {
                var conferenceFeedbackCandidates = conferenceFeedback.Any()? conferenceFeedback.Select(x => x.Name): new List<string>();
                var sessionsFeedbackCandidates = sessionFeedback.Any()? sessionFeedback.Select(x => x.Name): new List<string>();

                prizeDraw = conferenceFeedbackCandidates.Concat(sessionsFeedbackCandidates).ToArray();

            }
            else
            {
                prizeDraw = conferenceFeedback.Select(x => x.Name)
                    .Where(name =>
                        sessionFeedback.Count(s => s.Name.ToLowerInvariant() == name.ToLowerInvariant()) >=
                        conference.MinNumSessionFeedbackForPrizeDraw)
                    .ToArray();
            }

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(prizeDraw, settings);
        }
    }
}
