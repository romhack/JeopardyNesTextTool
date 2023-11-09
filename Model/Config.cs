using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JeopardyNesTextTool.Model
{
    public class Config: INotifyPropertyChanged
    {
        private const string ConfigFilePath = "Config.json";
        private string _destinationRomPath;
        private JsonConfigRoot _root;

        public string DestinationRomPath
        {
            get => _destinationRomPath;
            set
            {
                _destinationRomPath = value;
                _root.DestinationRomPath = value;
                Serialize();
                OnPropertyChanged("DestinationRomPath");
            }
        }
        public Group[] Groups { get; }
        public Config()
        {
            var jsonString = File.ReadAllText(ConfigFilePath);
            _root = JsonSerializer.Deserialize<JsonConfigRoot>(jsonString);
            if (_root?.JsonGroups is null || _root.JsonGroups.Any() == false)
            {
                throw new InvalidOperationException("Could not deserialize config");
            }
            DestinationRomPath = _root.DestinationRomPath;
            var jsonGroups = _root.JsonGroups;
            Groups = jsonGroups.Select(jsonGroup => new Group(jsonGroup)).ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void Serialize()
        {
            var jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonString = JsonSerializer.Serialize(_root, jsonSerializerOptions);
            File.WriteAllText(ConfigFilePath, jsonString);
        }
    }

    public class JsonConfigRoot
    {
        public string DestinationRomPath { get; set; }
        public List<JsonGroup> JsonGroups { get; set; }

    }

    public class JsonGroup
    {
        public JsonInsertRange InsertRange { get; set; }
        public List<JsonPointer> Pointers { get; set; }
        public string PointersBaseOffset { get; set; }
    }

    public class JsonInsertRange
    {
        public string StartOffset { get; set; }
        public string Size { get; set; }
    }


    public class JsonPointer
    {
        public string QuestionsPointerOffset { get; set; }
        public string AnswersPointerOffset { get; set; }
        public string ProformsPointerOffset { get; set; }
        public string TopicsPointerOffset { get; set; }
    }



    public class Group
    {
        public InsertRange InsertRange { get; }
        public Pointer[] Pointers { get; }
        public uint PointersBaseOffset { get; }

        public Group(JsonGroup jsonGroup)
        {
            InsertRange = new InsertRange(jsonGroup.InsertRange);
            Pointers = jsonGroup.Pointers.Select(jsonPointer => new Pointer(jsonPointer)).ToArray();
            PointersBaseOffset = Convert.ToUInt32(jsonGroup.PointersBaseOffset, 16);
        }
    }
    public class InsertRange
    {
        public uint StartOffset { get; }
        public uint Size { get; }

        public InsertRange(JsonInsertRange jsonInsertRange)
        {
            StartOffset = Convert.ToUInt32(jsonInsertRange.StartOffset, 16);
            Size = Convert.ToUInt16(jsonInsertRange.Size, 16);
        }
    }

    public class Pointer
    {
        public uint QuestionsPointerOffset { get; }
        public uint AnswersPointerOffset { get; }
        public uint ProformsPointerOffset { get; }
        public uint TopicsPointerOffset { get; }


        public Pointer(JsonPointer jsonPointer)
        {
            QuestionsPointerOffset = Convert.ToUInt32(jsonPointer.QuestionsPointerOffset, 16);
            AnswersPointerOffset = Convert.ToUInt32(jsonPointer.AnswersPointerOffset, 16);
            ProformsPointerOffset = Convert.ToUInt32(jsonPointer.ProformsPointerOffset, 16);
            TopicsPointerOffset = Convert.ToUInt32(jsonPointer.TopicsPointerOffset, 16);
        }
    }
}