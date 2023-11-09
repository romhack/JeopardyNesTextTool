using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JeopardyNesTextTool.Model;

namespace JeopardyNesTextTool.ViewModel
{
    public class ApplicationViewModel: INotifyPropertyChanged
    {

        private object _selectedBlock;
        private string _scriptFilePath;

        public string ScriptFilePath
        {
            get => _scriptFilePath;
            set
            {
                _scriptFilePath = value;
                OnPropertyChanged("ScriptFilePath");
            }
        } 

        public Config ViewModelConfig { get; }
        public CommandsManager CommandsManager { get; }
        public List<StructuredTextBlock> ModelBlocks { get; set; } = new();

        public ObservableCollection<ViewModelBlock> ViewModelBlocks { get; set; } = new(); 

        public object SelectedBlock
        {
            get => _selectedBlock;
            set
            {
                _selectedBlock = value;
                OnPropertyChanged("SelectedBlock");
            }
        }


        public ApplicationViewModel()
        {
            ViewModelConfig = new Config();
            CommandsManager = new CommandsManager(this);
        }


        private ICommand _selectedItemChangedCommand;

        public ICommand SelectedItemChangedCommand => _selectedItemChangedCommand ??= new RelayCommand(SelectedItemChanged);

        private void SelectedItemChanged(object selectedObject)
        {
            if (selectedObject is ViewModelElement element)
            {
                SelectedBlock = element.ModelObject;
            }
            else
            {
                SelectedBlock = null;
            }
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    public abstract class ViewModelElement
    {
        public string Name { get; set; }
        public object ModelObject { get; set; }
    }

    public class ViewModelBlock : ViewModelElement
    {
        public ObservableCollection<ViewModelTopic> ViewModelTopics { get; set; }
    }

    public class ViewModelTopic : ViewModelElement
    {
        public ObservableCollection<ViewModelQuestion> ViewModelQuestions { get; set; }

    }

    public class ViewModelQuestion : ViewModelElement
    {
        public Question ModelQuestion { get; set; }
    }
    public class PronounsNames : ObservableCollection<string>
    {
        public PronounsNames()
        {
            Add("WHO IS");
            Add("WHO IS THE");
            Add("WHO ARE");
            Add("WHO ARE THE");
            Add("WHAT IS");
            Add("WHAT IS THE");
            Add("WHAT IS A");
            Add("WHAT IS AN");
            Add("WHAT ARE");
            Add("WHAT ARE THE");
            Add("WHO WAS");
            Add("WHAT WAS");
            Add("WHAT WAS THE");
            Add("WHO WERE");
            Add("WHO WERE THE");
            Add("WHO WAS A");
        }
    }
}


