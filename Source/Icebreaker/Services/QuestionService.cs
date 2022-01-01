namespace Icebreaker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Icebreaker.Properties;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure;

    /// <summary>
    /// Implements minor question logic
    /// </summary>
    public class QuestionService
    {
        private readonly IBotDataProvider dataProvider;
        private readonly TelemetryClient telemetryClient;
        private readonly Random random;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestionService"/> class.
        /// </summary>
        /// <param name="dataProvider">DataProvider to use</param>
        /// <param name="telemetryClient">Used for logging</param>
        public QuestionService(IBotDataProvider dataProvider, TelemetryClient telemetryClient)
        {
            this.dataProvider = dataProvider;
            this.telemetryClient = telemetryClient;
            this.random = new Random();
            this.Initialize();
        }

        /// <summary>
        /// Select a random question from Database in given Language or the Default Language.
        /// </summary>
        /// <param name="cultureName">Language of Question</param>
        /// <returns>Question</returns>
        public virtual async Task<string> GetRandomOrDefaultQuestion(string cultureName)
        {
            var questions = await this.RetrieveQuestions(cultureName);
            if (questions is null || questions.Length == 0)
            {
                this.telemetryClient.TrackEvent("QuestionsNotFound", new Dictionary<string, string>() { { "cultureName", cultureName } });
                return Resources.DefaultQuestion;
            }
            else
            {
                return questions[this.random.Next(questions.Length)];
            }
        }

        private async Task<string[]> RetrieveQuestions(string cultureName)
        {
            return await this.dataProvider.GetQuestionsAsync(cultureName);
        }

        /// <summary>
        /// Initilize DataProvider with DefaultQuestion for Current Culture
        /// Just here because there is no way to add questions without editing the DataBase currently
        /// </summary>
        private async void Initialize()
        {
            var cultureName = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            if (await this.RetrieveQuestions(cultureName) == null)
            {
                var question = Resources.DefaultQuestion;
                await this.dataProvider.SetQuestionsAsync(cultureName, new string[] { question });
            }
        }
    }
}