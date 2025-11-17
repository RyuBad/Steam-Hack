using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace SteamHack.Models
{
    public class Jeu : INotifyPropertyChanged
    {
        private string _cheminExe;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Nom { get; set; }
        public BitmapImage Image { get; set; } // Icône du jeu pour la ListBox
        public BitmapImage Banner { get; set; } // Bannière du jeu pour la vue principale

        public string CheminExe
        {
            get => _cheminExe;
            set
            {
                if (_cheminExe != value)
                {
                    _cheminExe = value;
                    OnPropertyChanged(nameof(CheminExe));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}