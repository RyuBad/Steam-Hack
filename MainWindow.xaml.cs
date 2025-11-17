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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadGamesFromFile();
        }

        // ---------------------------------------------------------------------
        // SECTION 1: GESTION DE LA PERSISTANCE ET CHEMINS D'ACCÈS
        // ---------------------------------------------------------------------

        // Sauvegarde les jeux au moment de la fermeture de l'application
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveGames();
            base.OnClosing(e);
        }

        /// <summary>
        /// Obtient le chemin racine pour les données de l'application dans LocalApplicationData.
        /// (Ex: C:\Users\User\AppData\Local\SteamHack)
        /// </summary>
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

        /// <summary>
        /// Obtient le chemin complet du fichier de données JSON.
        /// </summary>
        private string GetDataFilePath()
        {
            return Path.Combine(GetAppDirectoryPath(), DataFileName);
        }

        // Charge la liste des jeux depuis un fichier JSON s'il existe, sinon charge les jeux par défaut
        private void LoadGamesFromFile()
        {
            string filePath = GetDataFilePath();
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
                        }
                    }
                    else
                    {
                        ChargerJeuxParDéfaut();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur de chargement des jeux : {ex.Message}");
                    ChargerJeuxParDéfaut();
                }
            }
            else
            {
                ChargerJeuxParDéfaut();
            }
            // Initialise la liste filtrée à partir de la liste complète après chargement
            JeuxFiltrés = new ObservableCollection<Jeu>(Jeux);
        }

        // Sauvegarde la liste des jeux dans un fichier JSON
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

        // Charge les jeux par défaut
        private void ChargerJeuxParDéfaut()
        {
            AjouterJeu("elden_ring.ico", "Elden Ring", "elden_ring.ico");
            AjouterJeu("undertale.ico", "Undertale", "undertale.ico");
        }

        // Ajoute un jeu à la collection et initialise ses images si disponibles
        private void AjouterJeu(string iconFile, string gameName, string bannerFile, string cheminExe = "")
        {
            // Chemin pour les icônes de démonstration (dans le répertoire de l'application)
            string defaultIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImgFolder, IconFolder, iconFile);
            string defaultBannerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImgFolder, BannerFolder, bannerFile);

            // Chemin pour les icônes extraites/sauvegardées (dans le répertoire utilisateur)
            string savedIconPath = Path.Combine(GetAppDirectoryPath(), ImgFolder, IconFolder, iconFile);
            string savedBannerPath = Path.Combine(GetAppDirectoryPath(), ImgFolder, BannerFolder, bannerFile);

            // On vérifie le chemin sauvegardé en priorité, puis le chemin par défaut
            string iconFilePath = File.Exists(savedIconPath) ? savedIconPath : (File.Exists(defaultIconPath) ? defaultIconPath : null);
            string bannerFilePath = File.Exists(savedBannerPath) ? savedBannerPath : (File.Exists(defaultBannerPath) ? defaultBannerPath : null);


            BitmapImage iconImage = iconFilePath != null ? new BitmapImage(new Uri(iconFilePath, UriKind.Absolute)) : null;
            BitmapImage bannerImage = bannerFilePath != null ? new BitmapImage(new Uri(bannerFilePath, UriKind.Absolute)) : null;

            Jeux.Add(new Jeu
            {
                Nom = gameName,
                Image = iconImage,
                Banner = bannerImage,
                CheminExe = cheminExe
            });
        }


        // Extrait l'icône du fichier .exe, la sauvegarde et retourne le nom du fichier sauvegardé.
        private string ExtractIconFromFilePath(string exePath, string gameName)
        {
            string iconFileName = gameName + ".ico";
            // Construit le chemin de sauvegarde dans le répertoire AppData
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
        // SECTION 3: ÉVÉNEMENTS D'INTERFACE UTILISATEUR
        // ---------------------------------------------------------------------

        // Bouton pour ajouter un jeu via le fichier .exe
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

                // Extraction de l'icône et sauvegarde dans AppData
                string iconFile = ExtractIconFromFilePath(exePath, gameName);

                // On utilise un nom de fichier par défaut pour la bannière, qui sera soit trouvée
                // dans le dossier de l'application (si le jeu est par défaut) ou ignorée.
                string bannerFile = gameName + "_banner.jpg";

                // 1. Ajoute le jeu à la collection principale 'Jeux'
                AjouterJeu(iconFile, gameName, bannerFile, exePath);

                // --- C'EST LA PARTIE À AJOUTER/MODIFIER ---

                // 2. Récupère le dernier jeu ajouté (qui est le nouveau jeu)
                Jeu nouveauJeu = Jeux[Jeux.Count - 1];

                // 3. L'ajoute à la collection filtrée 'JeuxFiltrés' pour l'affichage immédiat
                JeuxFiltrés.Add(nouveauJeu);

                // Optionnel : Sélectionne le nouveau jeu dans la liste
                if (GameList.Items.Count > 0)
                {
                    GameList.SelectedItem = nouveauJeu;
                    GameList.ScrollIntoView(nouveauJeu);
                }
                // ------------------------------------------
                MessageBox.Show($"Jeu ajouté : {gameName}\nChemin de l'exécutable : {exePath}");
            }
        }

        // Bouton "Jouer/installer" qui lance l'exécutable du jeu sélectionné
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
                            // Définit le répertoire de travail au répertoire de l'exécutable pour un lancement correct.
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
                    MessageBox.Show("Aucun fichier .exe sélectionné pour ce jeu !");
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un jeu.");
            }
        }

        // Permet de sélectionner le chemin de l'exécutable pour le jeu sélectionné
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
            else
            {
                MessageBox.Show("Veuillez sélectionner un jeu.");
            }
        }

        // Lorsqu'un jeu est sélectionné, met à jour l'affichage
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Jeu selectedGame)
            {
                GameBanner.Source = selectedGame.Banner;
                GameBanner.Visibility = selectedGame.Banner != null ? Visibility.Visible : Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
                SelectExeText.Visibility = Visibility.Visible;
            }
        }

        // Bouton "Demande d'ajout"
        private void AddGame(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pour faire une demande d'ajout de jeux, envoie un message sur Discord à 'nonock.'");
        }

        // Gestion des événements pour la zone de recherche
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

            // S'assurer que la liste de jeux filtrés est bien vidée et remplie
            JeuxFiltrés.Clear();
            if (string.IsNullOrWhiteSpace(filter) || filter == "rechercher un jeu...")
            {
                foreach (var jeu in Jeux)
                {
                    JeuxFiltrés.Add(jeu);
                }
            }
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
        // SECTION 4: CLASSES DE DONNÉES INTERNES
        //test2
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