using System.Windows.Media.Imaging;

namespace SteamHack.Models
{
    public class Jeu
    {
        public string Nom { get; set; }
        public BitmapImage Image { get; set; }
        public BitmapImage Banner { get; set; }
        public string CheminExe { get; set; }
    }
}