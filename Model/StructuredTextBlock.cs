using System.Linq;
using System.Text.Json.Serialization;

namespace JeopardyNesTextTool.Model
{
    public class StructuredTextBlock
    {

        public Topic[] Topics { get; set; }
        [JsonPropertyName("Final topic")]
        public FinalTopic FinalTopic { get; set; }

        public StructuredTextBlock()//For JSON deserialize construction
        {

        }

        public StructuredTextBlock(PlainTextBlock plainTextBlock)//For ROM parsing construction
        {
            const int questionsPerTopic = 5;
            var fullTopicsCount = plainTextBlock.Topics.Length - 1;
            Topics = new Topic[fullTopicsCount];
            for (var i = 0; i < fullTopicsCount; i++)//Process full topics (all but last)
            {
                var topic = new Topic
                {
                    Name = plainTextBlock.Topics[i],
                    Questions = new Question[questionsPerTopic]
                };
                for (var j = 0; j < questionsPerTopic; j++)
                {
                    var question = new Question
                    {
                        Text = plainTextBlock.Questions[i * questionsPerTopic + j],
                        PronounIndex = plainTextBlock.Pronouns[i * questionsPerTopic + j],
                        Answer = plainTextBlock.Answers[i * questionsPerTopic + j]
                    };
                    topic.Questions[j] = question;
                }
                Topics[i] = topic;
            }

            FinalTopic = new FinalTopic //Process final topic (last in block) with only one question
            {
                Name = plainTextBlock.Topics.Last(),
                Question = new Question
                {
                    Text = plainTextBlock.Questions.Last(),
                    PronounIndex = plainTextBlock.Pronouns[fullTopicsCount* questionsPerTopic],
                    Answer = plainTextBlock.Answers.Last()
                }
            };
        }

        public PlainTextBlock GetPlainBlock()
        {
            var topics = Topics.Select(topic => topic.Name).Append(FinalTopic.Name);
            var questions = (
                    from topic in Topics
                    from topicQuestion in topic.Questions
                    select topicQuestion.Text)
                .ToList();
            questions.Add(FinalTopic.Question.Text);
            var answers = (
                    from topic in Topics
                    from topicQuestion in topic.Questions
                    select topicQuestion.Answer)
                .ToList();
            answers.Add(FinalTopic.Question.Answer);
            var pronouns = (
                    from topic in Topics
                    from topicQuestion in topic.Questions
                    select topicQuestion.PronounIndex)
                .ToList();
            pronouns.Add(FinalTopic.Question.PronounIndex);

            return new PlainTextBlock
            {
                Topics = topics.ToArray(),
                Questions = questions.ToArray(),
                Answers = answers.ToArray(),
                Pronouns = pronouns.ToArray()
            };

        }


    }

    public class Topic
    {
        [JsonPropertyName("Topic name")]
        public string Name { get; set; }
        public Question[] Questions { get; set; }
    }

    public class FinalTopic
    {
        [JsonPropertyName("Final topic name")]
        public string Name { get; set; }
        public Question Question { get; set; }
    }

    public class Question
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("Presenter text")]
        public string Text { get; set; }
        [JsonPropertyOrder(2)]
        [JsonPropertyName("Pronoun index")]
        public byte PronounIndex { get; set; }
        [JsonPropertyOrder(3)]
        [JsonPropertyName("Player reply")]
        public string Answer { get; set; }

    }
}