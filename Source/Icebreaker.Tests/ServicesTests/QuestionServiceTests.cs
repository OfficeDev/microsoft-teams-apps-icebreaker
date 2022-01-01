namespace Icebreaker.Services.Tests
{
    using Icebreaker.Interfaces;
    using Icebreaker.Properties;
    using Moq;
    using System.Threading.Tasks;
    using Xunit;
    public class QuestionServiceTests
    {
        private readonly Mock<IBotDataProvider> dataProvider;
        private readonly QuestionService questionService;
        private readonly string defaultCulture;
        public QuestionServiceTests()
        {
            this.dataProvider = new Mock<IBotDataProvider>();
            this.questionService = new QuestionService(this.dataProvider.Object, new Microsoft.ApplicationInsights.TelemetryClient());
            this.defaultCulture = "en";
        }

        /// <summary>
        /// Should Return Null if no questions are available
        /// </summary>
        [Fact]
        public async void GetRandomQuestionFromNullTest()
        {
            this.dataProvider.Setup(x => x.GetQuestionsAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult<string[]>(null));

            var question = await this.questionService.GetRandomOrDefaultQuestion(this.defaultCulture);

            Assert.Equal(Resources.DefaultQuestion, question);
        }

        /// <summary>
        /// Should return Default Question if no questions are available
        /// </summary>
        [Fact]
        public async void GetRandomQuestionFromEmptyArrayTest()
        {
            this.dataProvider.Setup(x => x.GetQuestionsAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult(new string[0]));

            var question = await this.questionService.GetRandomOrDefaultQuestion(this.defaultCulture);

            Assert.Equal(Resources.DefaultQuestion, question);
        }

        /// <summary>
        /// Return the same question if only one question is available
        /// </summary>
        [Fact]
        public async void GetRandomQuestionOneQuestionTest()
        {
            var question = "question";
            this.dataProvider.Setup(x => x.GetQuestionsAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult(new string[] { question }));

            var randomQuestion = await this.questionService.GetRandomOrDefaultQuestion(this.defaultCulture);

            Assert.Equal(question, randomQuestion);
        }

        /// <summary>
        /// Return the any question
        /// </summary>
        [Fact]
        public async void GetRandomQuestionTwoQuestionsTest()
        {
            var questions = new string[] { "question1", "question2" };
            this.dataProvider.Setup(x => x.GetQuestionsAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult(questions));

            var randomQuestion = await this.questionService.GetRandomOrDefaultQuestion(this.defaultCulture);

            Assert.Contains(randomQuestion, questions);
        }
    }
}