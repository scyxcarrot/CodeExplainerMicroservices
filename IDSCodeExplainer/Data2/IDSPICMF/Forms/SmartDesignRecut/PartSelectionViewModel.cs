using Rhino.Geometry;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Forms
{
    public class PartSelectionViewModel : INotifyPropertyChanged
    {
        private string _partName;
        public string PartName
        {
            get => _partName;
            set
            {
                _partName = value;

                OnPropertyChanged("PartName");
            }
        }

        private ObservableCollection<string> _sourcePartNames;
        public ObservableCollection<string> SourcePartNames
        {
            get => _sourcePartNames;
            set
            {
                _sourcePartNames = value;

                OnPropertyChanged("SourcePartNames");
            }
        }


        private bool _isRequired;
        public bool IsRequired
        {
            get => _isRequired;
            set
            {
                _isRequired = value;

                OnPropertyChanged("IsRequired");
            }
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;

                OnPropertyChanged("Color");
            }
        }

        public List<Mesh> SelectedMeshes;
        public List<string> DefaultPartNames;
        public bool MultiParts;
        public bool IsSeparateContainer;

        public PartSelectionViewModel()
        {
            DefaultPartNames = new List<string>();
            IsRequired = true;
            Color = Color.Gray;
            MultiParts = false;
            IsSeparateContainer = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Reset()
        {
            SelectedMeshes?.Clear();
            SourcePartNames?.Clear();
        }

        public bool IsSelected()
        {
            return SelectedMeshes != null && SelectedMeshes.Any() &&
                SourcePartNames != null && SourcePartNames.Any();
        }
    }
}
