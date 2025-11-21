using Microsoft.Win32;
using SteamHack.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing; // Utilisé pour Icon.ExtractAssociatedIcon
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media; // Pour MediaPlayer

namespace SteamHack
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Jeu> Jeux { get; set; } = new();
        public ObservableCollection<Jeu> JeuxFiltrés { get; set; } = new();

        private const string DataFileName = "games.json";
        private const string ImgFolder = "img";
        private const string IconFolder = "icon";
        private const string BannerFolder = "banner";

        // Champ pour la musique
        private MediaPlayer player = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadGamesFromFile(); // Charge les jeux et initialise JeuxFiltrés
            PlayBackgroundMusic(); // Lance la musique
        }

        // ---------------------------------------------------------------------
        // SECTION 1: GESTION DE LA PERSISTANCE ET CHEMINS D'ACCÈS
        // ---------------------------------------------------------------------

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveGames();
            base.OnClosing(e);
        }

        private string GetAppDirectoryPath()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(folder, "SteamHack");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            return appFolder;
        }

        private string GetDataFilePath()
        {
            return Path.Combine(GetAppDirectoryPath(), DataFileName);
        }

        private void LoadGamesFromFile()
        {
            string filePath = GetDataFilePath();
            bool loadedSuccessfully = false;

            Jeux.Clear(); // S'assurer que la collection est vide avant le chargement

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var serializedGames = JsonSerializer.Deserialize<List<SerializedJeu>>(json);
                        if (serializedGames != null)
                        {
                            foreach (var sg in serializedGames)
                            {
                                AjouterJeu(sg.IconFile, sg.Nom, sg.BannerFile, sg.CheminExe);
                            }
                            loadedSuccessfully = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur de chargement des jeux : {ex.Message}");
                }
            }

            if (!loadedSuccessfully)
            {
                ChargerJeuxParDéfaut();
            }

            // Initialise la liste filtrée à partir de la liste complète après chargement
            JeuxFiltrés.Clear();
            foreach (var jeu in Jeux)
            {
                JeuxFiltrés.Add(jeu);
            }
        }

        private void SaveGames()
        {
            string filePath = GetDataFilePath();
            var serializedGames = new List<SerializedJeu>();

            foreach (var jeu in Jeux)
            {
                // Sauvegarde uniquement le nom du fichier (Ex: "monjeu.ico")
                string iconFile = jeu.Image?.UriSource != null ? Path.GetFileName(jeu.Image.UriSource.LocalPath) : "";
                string bannerFile = jeu.Banner?.UriSource != null ? Path.GetFileName(jeu.Banner.UriSource.LocalPath) : "";

                serializedGames.Add(new SerializedJeu
                {
                    Nom = jeu.Nom,
                    IconFile = iconFile,
                    BannerFile = bannerFile,
                    CheminExe = jeu.CheminExe
                });
            }

            try
            {
                string json = JsonSerializer.Serialize(serializedGames, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement des jeux : {ex.Message}");
            }
        }

        // ---------------------------------------------------------------------
        // SECTION 2: GESTION DES JEUX (Ajout, Extraction d'Icône)
        // ---------------------------------------------------------------------

        private void ChargerJeuxParDéfaut()
        {
            // Ces fichiers doivent exister dans: [Dossier_Executable]/img/icon/
            AjouterJeu("elden_ring.ico", "Elden Ring", "elden_ring.ico");
            AjouterJeu("undertale.ico", "Undertale", "undertale.ico");
        }

        private void AjouterJeu(string iconFile, string gameName, string bannerFile, string cheminExe = "")
        {
            // 1. Détermine les chemins d'accès (dossier AppData ou dossier de l'application)
            string defaultIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImgFolder, IconFolder, iconFile);
            string defaultBannerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImgFolder, BannerFolder, bannerFile);

            string savedIconPath = Path.Combine(GetAppDirectoryPath(), ImgFolder, IconFolder, iconFile);
            string savedBannerPath = Path.Combine(GetAppDirectoryPath(), ImgFolder, BannerFolder, bannerFile);

            // On vérifie le chemin sauvegardé en priorité, puis le chemin par défaut
            string iconFilePath = File.Exists(savedIconPath) ? savedIconPath : (File.Exists(defaultIconPath) ? defaultIconPath : null);
            string bannerFilePath = File.Exists(savedBannerPath) ? savedBannerPath : (File.Exists(defaultBannerPath) ? defaultBannerPath : null);

            // 2. Crée les BitmapImages
            BitmapImage iconImage = iconFilePath != null ? new BitmapImage(new Uri(iconFilePath, UriKind.Absolute)) : null;
            BitmapImage bannerImage = bannerFilePath != null ? new BitmapImage(new Uri(bannerFilePath, UriKind.Absolute)) : null;

            // 3. Ajoute à la collection principale
            Jeux.Add(new Jeu
            {
                Nom = gameName,
                Image = iconImage,
                Banner = bannerImage,
                CheminExe = cheminExe
            });
        }

        private string ExtractIconFromFilePath(string exePath, string gameName)
        {
            string iconFileName = gameName + ".ico";
            string iconSavePath = Path.Combine(GetAppDirectoryPath(), ImgFolder, IconFolder);
            string fullIconPath = Path.Combine(iconSavePath, iconFileName);

            if (!Directory.Exists(iconSavePath))
            {
                Directory.CreateDirectory(iconSavePath);
            }

            try
            {
                // Utilisation du nom entièrement qualifié pour éviter l'ambiguïté avec les classes WPF
                using (System.Drawing.Icon theIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                {
                    if (theIcon != null)
                    {
                        using (FileStream stream = new FileStream(fullIconPath, FileMode.Create))
                        {
                            theIcon.Save(stream);
                        }
                        return iconFileName;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'extraction de l'icône : {ex.Message}");
                MessageBox.Show($"Impossible d'extraire l'icône du fichier.\nErreur : {ex.Message}");
            }
            return "";
        }

        // ---------------------------------------------------------------------
        // SECTION 3: GESTION DE LA MUSIQUE
        // ---------------------------------------------------------------------

        private void PlayBackgroundMusic()
        {
            string musicDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musique");

            // 1. Récupérer tous les fichiers .mp3 dans le répertoire
            string[] musicFiles = Directory.GetFiles(musicDirectory, "*.mp3");

            if (musicFiles.Length > 0)
            {
                // 2. Choisir un fichier aléatoire dans la liste
                Random random = new Random();
                int randomIndex = random.Next(musicFiles.Length);
                string filePath = musicFiles[randomIndex]; // Sélectionne le chemin du fichier

                // 3. Lire le fichier sélectionné
                try
                {
                    Uri fileUri = new Uri(filePath, UriKind.Absolute);
                    player.Open(fileUri);

                    // Utiliser le volume du Slider, s'il est initialisé, sinon valeur par défaut.
                    player.Volume = (VolumeSlider != null) ? VolumeSlider.Value : 0.15;

                    player.MediaEnded += (s, e) =>
                    {
                        // Pour jouer une autre musique au pif après la fin de la précédente
                        // Nous rappelons la méthode PlayBackgroundMusic() elle-même.
                        PlayBackgroundMusic();
                    };

                    player.Play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la lecture de la musique : {ex.Message}", "Erreur Média", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Gérer le cas où aucun fichier n'est trouvé
                MessageBox.Show("Aucun fichier .mp3 trouvé dans le dossier Musique.", "Alerte Média", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PauseMusic_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }


        private void PlayMusic_Click(object sender, RoutedEventArgs e)
        {
            // Vérifie si le fichier est ouvert avant de lancer
            if (player.Source == null)
            {
                PlayBackgroundMusic();
            }

            else
            {
                player.Play();
            }
        }


        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
            {
                player.Volume = VolumeSlider.Value;
            }
        }

        // ---------------------------------------------------------------------
        // SECTION 4: ÉVÉNEMENTS D'INTERFACE UTILISATEUR
        // ---------------------------------------------------------------------

        private void Addexe(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string exePath = openFileDialog.FileName;
                string gameName = Path.GetFileNameWithoutExtension(exePath);

                string iconFile = ExtractIconFromFilePath(exePath, gameName);
                string bannerFile = gameName + "_banner.jpg";

                // 1. Ajoute le jeu à la collection principale 'Jeux'
                AjouterJeu(iconFile, gameName, bannerFile, exePath);

                // 2. Récupère le dernier jeu ajouté
                Jeu nouveauJeu = Jeux[Jeux.Count - 1];

                // 3. L'ajoute à la collection filtrée 'JeuxFiltrés' pour l'affichage immédiat
                JeuxFiltrés.Add(nouveauJeu);

                // 4. Sélectionne le nouveau jeu dans la liste
                if (GameList.Items.Count > 0)
                {
                    GameList.SelectedItem = nouveauJeu;
                    GameList.ScrollIntoView(nouveauJeu);
                }
                MessageBox.Show($"Jeu ajouté : {gameName}");
            }
        }

        // Supprime le jeu actuellement sélectionné des listes
        private void DeleteGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameList.SelectedItem is Jeu selectedGame)
            {
                string confirmationMessage = $"Êtes-vous sûr de vouloir supprimer '{selectedGame.Nom}' de la liste ?";
                MessageBoxResult result = MessageBox.Show(confirmationMessage, "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Supprimer des deux collections
                    Jeux.Remove(selectedGame);
                    JeuxFiltrés.Remove(selectedGame);

                    // Masquer les panneaux de détails après la suppression
                    GameBanner.Visibility = Visibility.Collapsed;
                    PlayButton.Visibility = Visibility.Collapsed;
                    DeleteButton.Visibility = Visibility.Collapsed;
                    SelectExeText.Visibility = Visibility.Collapsed;

                    MessageBox.Show($"Le jeu '{selectedGame.Nom}' a été supprimé.", "Suppression réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void PlayButton1(object sender, RoutedEventArgs e)
        {
            if (GameList.SelectedItem is Jeu selectedGame)
            {
                if (!string.IsNullOrEmpty(selectedGame.CheminExe) && File.Exists(selectedGame.CheminExe))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = selectedGame.CheminExe,
                            WorkingDirectory = Path.GetDirectoryName(selectedGame.CheminExe),
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du lancement du jeu : {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Aucun fichier .exe sélectionné pour ce jeu ! Utilisez le lien 'Le jeu est-il déjà installé ?' pour le sélectionner.");
                }
            }
        }

        private void SetGameExePath_Click(object sender, RoutedEventArgs e)
        {
            if (GameList.SelectedItem is Jeu selectedGame)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Executable Files (*.exe)|*.exe"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    selectedGame.CheminExe = openFileDialog.FileName;
                    MessageBox.Show($"Fichier .exe sélectionné pour {selectedGame.Nom} :\n{selectedGame.CheminExe}");
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Jeu selectedGame)
            {
                GameBanner.Source = selectedGame.Banner;
                GameBanner.Visibility = selectedGame.Banner != null ? Visibility.Visible : Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible; // Affiche le bouton Supprimer
                SelectExeText.Visibility = Visibility.Visible;
            }
            else
            {
                // Masquer les contrôles si la sélection est effacée
                GameBanner.Visibility = Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
                SelectExeText.Visibility = Visibility.Collapsed;
            }
        }

        private void AddGame(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pour faire une demande d'ajout de jeux, envoie un message sur Discord à 'nonock.'");
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Rechercher un jeu...")
            {
                textBox.Text = "";
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Rechercher un jeu...";
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string filter = textBox.Text.ToLower();

            // Clear la liste filtrée
            JeuxFiltrés.Clear();

            // Si la recherche est vide ou le placeholder, affiche tous les jeux
            if (string.IsNullOrWhiteSpace(filter) || filter == "rechercher un jeu...")
            {
                foreach (var jeu in Jeux)
                {
                    JeuxFiltrés.Add(jeu);
                }
            }
            // Sinon, filtre les jeux
            else
            {
                foreach (var jeu in Jeux)
                {
                    if (jeu.Nom.ToLower().Contains(filter))
                    {
                        JeuxFiltrés.Add(jeu);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------
        // SECTION 5: CLASSES DE DONNÉES INTERNES
        // ---------------------------------------------------------------------

        // Classe interne pour la sérialisation des données d'un jeu
        private class SerializedJeu
        {
            public string Nom { get; set; }
            public string IconFile { get; set; }
            public string BannerFile { get; set; }
            public string CheminExe { get; set; }
        }
    }
}