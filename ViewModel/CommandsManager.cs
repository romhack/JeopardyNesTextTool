using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using JeopardyNesTextTool.Model;
using JeopardyNesTextTool.Model.BinaryHelpers;
using Microsoft.Win32;
using BinaryReader = JeopardyNesTextTool.Model.BinaryHelpers.BinaryReader;
using BinaryWriter = JeopardyNesTextTool.Model.BinaryHelpers.BinaryWriter;

namespace JeopardyNesTextTool.ViewModel;

public class CommandsManager
{
    private const int QuestionsTreeSize = 0x62;
    const int QuestionsTreeOffset = 0x9D6A;
    private const int AnswersTreeSize = 0x94;
    const int AnswersTreeOffset = 0x9DCC;


    private readonly ApplicationViewModel _applicationViewModel;
    private RelayCommand _saveCommand;
    private RelayCommand _openCommand;
    private RelayCommand _extractCommand;
    private RelayCommand _setRomCommand;
    private RelayCommand _insertCommand;

    public CommandsManager(ApplicationViewModel applicationViewModel)
    {
        _applicationViewModel = applicationViewModel;
    }



    public RelayCommand SaveCommand
    {
        get
        {
            return _saveCommand ??= new RelayCommand(SaveCommand_Execute,
                _ => SaveCommand_CanExecute(null));
        }
    }

    public void SaveCommand_Execute(object _) 
    {
        var jsonWriter = new BinaryWriter(_applicationViewModel.ScriptFilePath, false);
        jsonWriter.JsonSerialize(_applicationViewModel.ModelBlocks);
    }

    public bool SaveCommand_CanExecute(object _)
    {
        return _applicationViewModel.ModelBlocks.Any() 
            && string.IsNullOrEmpty(_applicationViewModel.ScriptFilePath) == false;
    }

    public RelayCommand OpenCommand
    {
        get
        {
            return _openCommand ??= new RelayCommand(OpenCommand_Execute, _ => true);
        }
    }

    public void OpenCommand_Execute(object _)
    {

        var openFileDialog = new OpenFileDialog()
        {
            InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty
        };
        if (openFileDialog.ShowDialog() == false)
        {
            return;
        }
        _applicationViewModel.ScriptFilePath = openFileDialog.FileName;
        var jsonReader = new BinaryReader(_applicationViewModel.ScriptFilePath);
        _applicationViewModel.ModelBlocks = jsonReader.JsonDeserialize();
        _applicationViewModel.ViewModelBlocks.Clear();
        foreach (var modelBlockTuple in _applicationViewModel.ModelBlocks.Select((value, i) => (value, i)))
        {
            var blockNumber = modelBlockTuple.i + 1;
            var newViewModelBlock = new ViewModelBlock()
            {
                Name = $"Block {blockNumber}",
                ModelObject = modelBlockTuple.value,
                ViewModelTopics = GetViewModelTopics(modelBlockTuple.value, blockNumber)
            };
            _applicationViewModel.ViewModelBlocks.Add(newViewModelBlock);
        }
    }

    private static ObservableCollection<ViewModelTopic> GetViewModelTopics(StructuredTextBlock modelBlock, int blockNumber)
    {
        var viewModelTopics = new ObservableCollection<ViewModelTopic>();
        foreach (var modelTopicTuple in modelBlock.Topics.Select((value, i) => (value, i)))
        {
            var topicNumber = modelTopicTuple.i + 1;
            var newViewModelTopic = new ViewModelTopic()
            {
                Name = $"Topic {blockNumber}.{topicNumber}",
                ModelObject = modelTopicTuple.value,
                ViewModelQuestions = GetViewModelQuestions(modelTopicTuple.value, topicNumber, blockNumber)
            };
            viewModelTopics.Add(newViewModelTopic);
        }
        return viewModelTopics;
    }

    private static ObservableCollection<ViewModelQuestion> GetViewModelQuestions(Topic modelTopic, int topicNumber, int blockNumber)
    {
        var viewModelQuestions = new ObservableCollection<ViewModelQuestion>();
        foreach (var modelQuestionTuple in modelTopic.Questions.Select((value, i) => (value, i)))
        {
            var questionNumber = modelQuestionTuple.i + 1;
            var newViewModelQuestion = new ViewModelQuestion()
            {
                Name = $"Question {blockNumber}.{topicNumber}.{questionNumber}",
                ModelQuestion = modelQuestionTuple.value,
                ModelObject = modelQuestionTuple.value
            };
            viewModelQuestions.Add(newViewModelQuestion);
        }
        return viewModelQuestions;
    }

    public RelayCommand ExtractCommand
    {
        get
        {
            return _extractCommand ??= new RelayCommand(ExtractCommand_Execute, _ => true);
        }
    }

    public void ExtractCommand_Execute(object _)
    {
        var initialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var openFileDialog = new OpenFileDialog()
        {
            InitialDirectory = initialDirectory
        };
        if (openFileDialog.ShowDialog() == false)
        {
            return;
        }
        var sourceRomFilePath = openFileDialog.FileName;

        var binaryReader = new BinaryReader(sourceRomFilePath);

        var questionTree = ReadTree(binaryReader, QuestionsTreeOffset, QuestionsTreeSize);
        var answersTree = ReadTree(binaryReader, AnswersTreeOffset, AnswersTreeSize);
        answersTree.SetTreeCharsPaths();

        const int questionsInBlockCount = 61; //12 topics x 5 questions + final question
        const int topicsInBlockCount = 13; // 12 topics + final topic
        const int pronounsInBlockCount = 62; //Even nibbles to read whole bytes

        var blocks = new List<StructuredTextBlock>();
        foreach (var configGroup in _applicationViewModel.ViewModelConfig.Groups)
        {
            foreach (var blockPointer in configGroup.Pointers)
            {
                var topicsBlockOffset = binaryReader.ReadWord(blockPointer.TopicsPointerOffset) + configGroup.PointersBaseOffset;
                var questionsBlockOffset = binaryReader.ReadWord(blockPointer.QuestionsPointerOffset) + configGroup.PointersBaseOffset;
                var answersBlockOffset = binaryReader.ReadWord(blockPointer.AnswersPointerOffset) + configGroup.PointersBaseOffset;
                var pronounsBlockOffset = binaryReader.ReadWord(blockPointer.ProformsPointerOffset) + configGroup.PointersBaseOffset;

                var plainBlock = new PlainTextBlock
                {
                    Topics = DecodeBlock(binaryReader, questionTree, topicsBlockOffset, topicsInBlockCount),
                    Questions = DecodeBlock(binaryReader, questionTree, questionsBlockOffset, questionsInBlockCount),
                    Answers = DecodeBlock(binaryReader, answersTree, answersBlockOffset, questionsInBlockCount),
                    Pronouns = binaryReader.ReadNybblesBlock(pronounsBlockOffset, pronounsInBlockCount)
                };
                blocks.Add(new StructuredTextBlock(plainBlock));
            }
        }
        var scriptFilePath = Path.Combine(Path.GetDirectoryName(sourceRomFilePath) ?? string.Empty, "ExtractedScript.json");
        var jsonWriter = new BinaryWriter(scriptFilePath, false);
        jsonWriter.JsonSerialize(blocks);
        MessageBox.Show("Script was successfully extracted to ROM folder.",
            "Extraction finished",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    private static InternalNode ReadTree(BinaryReader binaryHelper, uint treeOffset, int treeSize)
    {
        var treeBytes = binaryHelper.ReadBytesBlock(treeOffset, treeSize);
        var questionTree = new InternalNode();
        questionTree.Deserialize(treeBytes, (byte)(treeBytes.Length - 2));
        return questionTree;
    }
    private static string[] DecodeBlock(BinaryReader binaryHelper, InternalNode tree, uint textOffset, uint stringsCount)
    {
        var bits = binaryHelper.ReadBitsBlock(textOffset);
        var bitsEnumerator = bits.GetEnumerator();
        var result = DecodeStringArray(tree, bitsEnumerator, stringsCount);
        return result.ToArray();
    }
    private static IEnumerable<string> DecodeStringArray(InternalNode questionTree,
        IEnumerator<bool> questionsBitsEnumerator,
        uint questionsCount)
    {
        var questions = new string[questionsCount];
        for (var i = 0; i < questionsCount; i++) questions[i] = DecodeString(questionTree, questionsBitsEnumerator);

        return questions;
    }
    private static string DecodeString(InternalNode tree, IEnumerator<bool> bitsEnumerator)
    {
        var stringBuilder = new StringBuilder();
        var decodedChar = '\0';
        while (decodedChar != '\r')
        {
            decodedChar = tree.DecodeChar(bitsEnumerator);
            stringBuilder.Append(decodedChar);
        }

        return stringBuilder.ToString();
    }

    public RelayCommand SetRomCommand
    {
        get
        {
            return _setRomCommand ??= new RelayCommand(SetRomCommand_Execute, _ => true);
        }
    }

    public void SetRomCommand_Execute(object _)
    {
        var initialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var openFileDialog = new OpenFileDialog()
        {
            InitialDirectory = initialDirectory,
            Filter = "NES ROM files (*.nes)|*.nes"
        };
        if (openFileDialog.ShowDialog() == false)
        {
            return;
        }
        _applicationViewModel.ViewModelConfig.DestinationRomPath = openFileDialog.FileName;
    }

    public RelayCommand InsertCommand
    {
        get
        {
            return _insertCommand ??= new RelayCommand(InsertCommand_Execute,
                _ => InsertCommand_CanExecute(null));
        }
    }

    public void InsertCommand_Execute(object _)
    {
        SaveCommand_Execute(null);
        var writeQueue = new WriteQueue();
        var jsonReader = new BinaryReader(_applicationViewModel.ScriptFilePath);
        var newBlocks = jsonReader.JsonDeserialize();

        var questionTopicsBlocks = newBlocks.SelectMany(block => block.GetPlainBlock().Questions)
            .Concat(newBlocks.SelectMany(block => block.GetPlainBlock().Topics));
        var questionTopicsBlocksString = string.Join("", questionTopicsBlocks);
        var questionsRebuiltTree = new InternalNode(questionTopicsBlocksString);
        questionsRebuiltTree.SetTreeCharsPaths();
        var serializedQuestionsRebuiltTree = questionsRebuiltTree.Serialize(QuestionsTreeSize);
        writeQueue.Add(new WriteQueueElement(QuestionsTreeOffset, serializedQuestionsRebuiltTree, QuestionsTreeSize, 0xFF));

        var answerBlocks = newBlocks.SelectMany(block => block.GetPlainBlock().Answers);
        var answerBlocksString = string.Join("", answerBlocks);
        var answersRebuiltTree = new InternalNode(answerBlocksString);
        answersRebuiltTree.SetTreeCharsPaths();
        var serializedAnswerRebuiltTree = answersRebuiltTree.Serialize(AnswersTreeSize);
        writeQueue.Add(new WriteQueueElement(AnswersTreeOffset, serializedAnswerRebuiltTree, AnswersTreeSize, 0xFF));

        var blockCount = 0;
        foreach (var configGroup in _applicationViewModel.ViewModelConfig.Groups)
        {
            var encodedConfigGroupBinaryBlock = new List<byte>();
            var currentPosition = configGroup.InsertRange.StartOffset;
            try
            {
                foreach (var configGroupPointer in configGroup.Pointers)
                {
                    var pointerValue = (ushort)(currentPosition - configGroup.PointersBaseOffset);
                    var plainBlock = newBlocks[blockCount].GetPlainBlock();
                    plainBlock.Encode(questionsRebuiltTree, answersRebuiltTree);
                    plainBlock.UpdatePointers(pointerValue);
                    var questionsPointerValue = BinaryWriter.WordsToBytes(new[] { plainBlock.QuestionsPointer });
                    writeQueue.Add(new WriteQueueElement(configGroupPointer.QuestionsPointerOffset, questionsPointerValue, 2, 0xFF));
                    var answersPointerValue = BinaryWriter.WordsToBytes(new[] { plainBlock.AnswersPointer });
                    writeQueue.Add(new WriteQueueElement(configGroupPointer.AnswersPointerOffset, answersPointerValue, 2, 0xFF));
                    var proformsPointerValue = BinaryWriter.WordsToBytes(new[] { plainBlock.PronounsPointer });
                    writeQueue.Add(new WriteQueueElement(configGroupPointer.ProformsPointerOffset, proformsPointerValue, 2, 0xFF));
                    var topicsPointerValue = BinaryWriter.WordsToBytes(new[] { plainBlock.TopicsPointer });
                    writeQueue.Add(new WriteQueueElement(configGroupPointer.TopicsPointerOffset, topicsPointerValue, 2, 0xFF));

                    var encodedBlock = plainBlock.GetEncodedBinaryBlock();
                    encodedConfigGroupBinaryBlock.AddRange(encodedBlock);
                    currentPosition += (uint)encodedBlock.Length;
                    blockCount++;
                }
                writeQueue.Add(new WriteQueueElement(configGroup.InsertRange.StartOffset, encodedConfigGroupBinaryBlock.ToArray(), configGroup.InsertRange.Size, 0xFF));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,
                    "Insertion failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

        }
        var binaryWriter = new BinaryWriter(_applicationViewModel.ViewModelConfig.DestinationRomPath, true);
        binaryWriter.ExecuteWriteQueue(writeQueue);
        MessageBox.Show("Script was successfully inserted in ROM.",
            "Insertion finished",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public bool InsertCommand_CanExecute(object _)
    {
        return _applicationViewModel.ModelBlocks.Any()
               && string.IsNullOrEmpty(_applicationViewModel.ScriptFilePath) == false
               && string.IsNullOrEmpty(_applicationViewModel.ViewModelConfig.DestinationRomPath) == false;
    }

}