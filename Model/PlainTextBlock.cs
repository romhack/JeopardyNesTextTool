using System.Linq;
using JeopardyNesTextTool.Model.BinaryHelpers;

namespace JeopardyNesTextTool.Model
{
    public class PlainTextBlock
    {
        public string[] Topics { get; set; }
        public string[] Questions { get; set; }
        public string[] Answers{ get; set; }
        public byte[] Pronouns { get; set; }

        public byte[] TopicsEncoded { get; set; }
        public byte[] QuestionsEncoded { get; set; }
        public byte[] AnswersEncoded { get; set; }
        public byte[] PronounsEncoded { get; set; }

        public ushort TopicsPointer { get; set; }
        public ushort QuestionsPointer { get; set; }
        public ushort AnswersPointer { get; set; }
        public ushort PronounsPointer { get; set; }

        public void Encode(InternalNode questionsTree, InternalNode answersTree)
        {
            var topicsEncodedBits = questionsTree.EncodeString(string.Join("", Topics));
            TopicsEncoded = BinaryWriter.BoolsToBytes(topicsEncodedBits.ToList());
            var questionsEncodedBits = questionsTree.EncodeString(string.Join("", Questions));
            QuestionsEncoded = BinaryWriter.BoolsToBytes(questionsEncodedBits.ToList());
            var answersEncodedBits = answersTree.EncodeString(string.Join("", Answers));
            AnswersEncoded = BinaryWriter.BoolsToBytes(answersEncodedBits.ToList());
            PronounsEncoded = BinaryWriter.NybblesToBytes(Pronouns);
        }

        public void UpdatePointers(ushort topicsPointer)
        {
            TopicsPointer = topicsPointer;
            QuestionsPointer = (ushort)(TopicsPointer + TopicsEncoded.Length);
            AnswersPointer = (ushort)(QuestionsPointer + QuestionsEncoded.Length);
            PronounsPointer = (ushort)(AnswersPointer + AnswersEncoded.Length);
        }
        
        public byte[] GetEncodedBinaryBlock()
        {
            return TopicsEncoded.Concat(QuestionsEncoded).Concat(AnswersEncoded).Concat(PronounsEncoded).ToArray();
        }

    }
}