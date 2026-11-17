using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CustomObjectWPF.Model;

namespace Vitrine
{
    /// <summary>
    /// Vitrine de navigation v13 : une page par contrôle, un exemple par propriété.
    /// Les images des exemples SearchIcon / ClearIcon / RevealImage / HideImage
    /// sont intégrées en base64 dans ce fichier (comme les yeux dans la librairie),
    /// donc la vitrine tourne seule, sans dossier d'images.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<string> _navItems = new()
        {
            "Accueil", "ButtonCustom", "CheckBoxCustom", "ToggleSwitchCustom",
            "CardCustom", "SliderCustom", "ListBoxCustom", "TextBoxCustom",
            "PasswordBoxCustom", "PasswordRevealCustom", "SearchBoxCustom",
            "NumericUpDownCustom", "ComboBoxCustom"
        };

        private readonly Dictionary<string, UIElement> _pages = new();
        private string _pageCourante = "Accueil";

        private readonly List<string> _documents = new()
        {
            "Rapport.docx", "Budget.xlsx", "Photo.png", "Contrat.pdf",
            "Notes.txt", "Présentation.pptx", "Facture.pdf", "CV.docx",
            "Code source.zip", "Manuel.pdf", "Liste.csv", "Dessin.svg"
        };

        public MainWindow()
        {
            InitializeComponent();

            foreach (string nom in _navItems)
            {
                UIElement page = (UIElement)FindName("page_" + nom);
                if (page != null) _pages[nom] = page;
            }

            navList.ItemsSource = _navItems;
            navList.SelectionChanged += NavList_SelectionChanged;
            navSearch.TextChanged += NavSearch_TextChanged;

            // ComboBox : les listes viennent d'ici (ItemsSource).
            string[] categories = { "Documents", "Images", "Code", "Notes" };
            comboDemo1.ItemsSource = categories;
            comboDemo2.ItemsSource = categories;
            comboDemo4.ItemsSource = categories;
            comboDemo5.ItemsSource = categories;
            comboDemo7.ItemsSource = categories;
            comboDemo8.ItemsSource = categories;
            comboDemo9.ItemsSource = categories;
            comboDemo10.ItemsSource = categories;
            comboDemo11.ItemsSource = categories;
            comboDemo6.ItemsSource = _documents;
            comboDemo3.ItemsSource = new List<Categorie>
            {
                new("Documents"), new("Images"), new("Code"), new("Notes")
            };

            // Images des exemples : intégrées en base64 (voir ci-dessous).
            searchIconDemo.SearchIcon = ImageFromBase64(DemoLoupeBase64);
            searchClearDemo.ClearIcon = ImageFromBase64(DemoXBase64);
            pwdRevealIconDemo.RevealImage = ImageFromBase64(DemoOeilOuvertBase64);
            pwdRevealIconDemo.HideImage = ImageFromBase64(DemoOeilFermeBase64);
            btnIconDemo.Icon = ImageFromBase64(DemoLoupeBase64);
            btnIconDemo2.Icon = ImageFromBase64(DemoLoupeBase64);
            btnIconDemo3.Icon = ImageFromBase64(DemoLoupeBase64);

            // Recherche fonctionnelle : la barre filtre la liste en direct.
            searchDemo.TextChanged += SearchDemo_TextChanged;
            searchDemo.Search += SearchDemo_Search;
            lstResultats.ItemsSource = _documents;

            ShowPage("Accueil");
        }

        private void ShowPage(string nom)
        {
            _pageCourante = nom;
            foreach (UIElement page in _pages.Values) page.Visibility = Visibility.Collapsed;
            if (_pages.TryGetValue(nom, out UIElement? cible)) cible.Visibility = Visibility.Visible;
            contentScroll.ScrollToTop();
            if ((string)navList.SelectedItem != nom) navList.SelectedItem = nom;
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (navList.SelectedItem is string nom && nom != _pageCourante) ShowPage(nom);
        }

        private void NavSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtre = navSearch.Text ?? string.Empty;
            navList.SelectionChanged -= NavList_SelectionChanged;
            navList.ItemsSource = null;
            navList.ItemsSource = _navItems
                .Where(n => n.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (_navItems.Contains(_pageCourante) && navList.Items.Cast<string>().Contains(_pageCourante))
                navList.SelectedItem = _pageCourante;
            navList.SelectionChanged += NavList_SelectionChanged;
        }

        private void HomeCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border carte && carte.Tag is string nom)
            {
                if (!string.IsNullOrEmpty(navSearch.Text)) navSearch.Text = string.Empty;
                ShowPage(nom);
            }
        }

        private void SliderDemo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtSliderValeur != null) txtSliderValeur.Text = $"Valeur: {sliderDemo.Value:0}";
        }

        private void SearchDemo_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtre = searchDemo.Text ?? string.Empty;
            lstResultats.ItemsSource = _documents
                .Where(d => d.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                .ToList();
            txtStatut.Text = $"{lstResultats.Items.Count} document(s)";
        }

        private void SearchDemo_Search(object sender, RoutedEventArgs e)
        {
            txtStatut.Text = $"Recherche lancée pour « {searchDemo.Text} » — {lstResultats.Items.Count} résultat(s).";
        }

        // Clic sur la loupe (nouveau v15) : même action que Entrée, mais à la souris.
        private void SearchLoupeDemo_SearchIconClicked(object sender, RoutedEventArgs e)
        {
            string filtre = searchLoupeDemo.Text ?? string.Empty;
            lstResultats.ItemsSource = _documents
                .Where(d => d.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                .ToList();
            txtLoupeStatut.Text = $"Loupe cliquée ! Recherche lancée pour « {filtre} » — {lstResultats.Items.Count} résultat(s).";
        }

        #region Images des exemples (base64, intégrées)

        // Loupe cyan + X rouge (ceux du dossier Images/) et deux yeux
        // (ouvert cyan, fermé rouge) : intégrés ici pour que la vitrine
        // tourne seule. Pour tes propres images, mets le PNG en Resource
        // dans ton projet et écris SearchIcon="Images/loupe.png" en XAML.
        private static ImageSource ImageFromBase64(string base64)
        {
            byte[] octets = Convert.FromBase64String(base64);
            using MemoryStream flux = new(octets);
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = flux;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private const string DemoLoupeBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABdUlEQVR4nO2aW3aDMAxEJ91Q97+Mrij9TnvAwprRA3R/E4R8bYFt" +
            "DAzDMAzD8FRessg/7/fyP98v3f2NcBOwNPqIJBmcm3oa/pdgEV/uCMzGK+It8AlQJRsoYV+AOskgCXv1Zk3urJ4ZMQhcD75KfCdh" +
            "RUwj10pAlejqOmE52AWoeylJgv81CPCGaMJkyCbgzD47acaD8wK+EaDqscCRwCmBxqwFBE9Nl5Dz2R8B6mEaVAZTAtkJZDMCshPI" +
            "ZgRkJ5DN4zdE1gIKbF1/QM5nSsB19WM2RSOXqJFLb7BKgCUhYeFlF6DeskraGL02AlQSEpfc9b8LWONtsh8wq9fKzAOyJkhldoSA" +
            "W0jwvwYV2+KWmCQJtU+IBByzqX9GSCyh1krvCKGEHqtB4TOhhwBAJqGPAEAioZcAgC6hnwCAKqGnAIAmoa8AgCKhtwDALaG/AMAl" +
            "4R4CgLWEg9/vIwA4lnAi514CgP+NrfZlK4xqZ5uGYRgq8guHcqJb0bvdwwAAAABJRU5ErkJggg==";

        private const string DemoXBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABcElEQVR4nO2aQW7CQBAEizyOR/M5ciKyoiR4d7t7Qek6I892YYNn" +
            "NFBKKaWUUkopE9yv1/s71/9QFN8lQVF/WsD3omkJqvpTAn4rlpKgrD8s4FkRtwR1/WEBl9vt8uwzLglnrnvmfEemHoEdEhzhYeFH" +
            "MCnBFR4W/wYTEpzhYVHA2eKzEtzhQSDg7CFGJSTCg0gAaCWkwoNQAGgkJMODWACsSUiHB4MAmJOwIzyYBMCYhF3hASwXPaJ4GXKF" +
            "B+Md8GD18M7wEBAA8yHc4SEkAMbDJMJDUACcD5UKD2EByjdBFTEBjl5AQUSAsxtcxS4gMQ9YwSpA9YbnlGATMBJ+pwSLgJlvfpcE" +
            "uYCV236HBKkAxTOfliAToGxpkxIkAhz9fErCsgDnMCMhQbIf8BeJecBL7Af8hKqrc0qQ7gccUbe0Lgny/QAwDjANEuT7Ae5hhrq+" +
            "dD8gNclR1pftByTHWK9Q/4t33xMspZRSSinln/IJdkYfN3pGnfAAAAAASUVORK5CYII=";

        private const string DemoOeilOuvertBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAA/ElEQVR4nO3Z6xWDMAhAYe1C3X8MJ0oXkPAIoKe53+8KBE2M6XEA" +
            "AAAAAAAA2MnZkuUaI3zt9yytMT/4ymCtEpuSE6hj0JLFZsQvfnLQkkAz/A2IDHzlLhXn8xVmKaZ40cquw1asljAy6LuYWXEcMfWE" +
            "UoKqx7oi7iTmJzugSrtjFXuGScx5A+4SdCxoq01w1Cg3IPs1543XlN/+BHSs7pmM9fqmwB+iAeZfvnHrO2OsV25A9pz3xmvK75sC" +
            "1xgt3/arr1pHjfMGBDYWKm1wzTtBvgVSkxkSpmj/GvQmDxbzRD5OhFISb3kmKNnyVFjz4v8FAAAAAAAAgM38AEbRgSTGKBFBAAAA" +
            "AElFTkSuQmCC";

        private const string DemoOeilFermeBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABbklEQVR4nO2aSxaDIAxFsYtz0WyOjjinEySflyD03WmrCbdECLYU" +
            "QgghhBBCCJnQ7rutzgHNR3vBaRLEAn4HfpIE9QzotPtuJ4gQCXga6O4SRAKuWq+nz3eWIC6BUyU8DmrEbLAzWW/CnKhGgmd2RMt0" +
            "3XzVtEdKcd9ode17ZYSVwAosMtQXZNezJZ4mjiohxK+OqF9JHtI4oi9pl73MZdIba5rIKIB3YxSxvFlyNQ1Ck3y2CG3Oqm7wqvXS" +
            "Jpy9hdbmOBSATOwNfcQoBqwZ8l4fMRMk3zMfiFiYTc8Vm6tUAZ2ZhEwRpjNBBNElIb1+KCCjp898LkCWwYjpiZagzTFsJ2jBu2mC" +
            "7wRRiWnI7jvg3WBWt4fK43X9uSeOJR5PhBCBV8vwlF36kzwKq4SUFxioeo5YjbZ5g9NBS9hOQAclYkk3iAC1hd5WQCkYCVsLKGWy" +
            "zxeUwfYCSrEd1h7LKf9dcvH3AgghhBBCCJnzBeGkBg4KruUmAAAAAElFTkSuQmCC";

        #endregion

        public record Categorie(string Nom);
    }
}
