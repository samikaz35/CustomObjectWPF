using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomObject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // ============ SLIDER : Value + ValueChanged ============

        // Se déclenche à chaque mouvement de la pastille.
        // e.NewValue = la nouvelle valeur (double).
        private void SliderDemo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Peut se déclencher pendant InitializeComponent() : quand le XAML
            // applique Value="30", le slider existe déjà mais pas encore
            // lblSliderLive (déclaré APRÈS dans le XAML) -> on se protège du null.
            if (lblSliderLive == null) return;

            lblSliderLive.Text = $"En direct (ValueChanged) : {e.NewValue:F0}";
        }

        // Lecture "à la demande" : quand on clique sur le bouton, on va chercher
        // la valeur actuelle directement sur le contrôle.
        private void BtnLireSlider_Click(object sender, RoutedEventArgs e)
        {
            lblSliderLu.Text = $"sliderDemo.Value = {sliderDemo.Value:F0}";
        }

        // ============ CHECKBOX : IsChecked + Checked/Unchecked ============

        private void CaseDemo_Checked(object sender, RoutedEventArgs e)
        {
            if (lblCaseEtat == null) return; // même protection qu'au slider
            lblCaseEtat.Text = "État : cochée (événement Checked)";
        }

        private void CaseDemo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (lblCaseEtat == null) return;
            lblCaseEtat.Text = "État : décochée (événement Unchecked)";
        }

        private void BtnLireCase_Click(object sender, RoutedEventArgs e)
        {
            // NOTE : IsChecked est un bool? (bool nullable), pas un bool.
            // On compare avec == true au lieu de faire .Value (qui planterait si null).
            bool cochee = caseDemo.IsChecked == true;
            lblCaseLue.Text = $"caseDemo.IsChecked = {cochee}";
        }

        // ============ TOGGLE : même pattern (c'est un CheckBox) ============

        private void ToggleDemo_Checked(object sender, RoutedEventArgs e)
        {
            if (lblToggleEtat == null) return; // même protection qu'au slider
            lblToggleEtat.Text = "État : ON (événement Checked)";
        }

        private void ToggleDemo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (lblToggleEtat == null) return;
            lblToggleEtat.Text = "État : OFF (événement Unchecked)";
        }

        private void BtnLireToggle_Click(object sender, RoutedEventArgs e)
        {
            bool allume = toggleDemo.IsChecked == true;
            lblToggleLue.Text = $"toggleDemo.IsChecked = {allume} (ON = true)";
        }
    }
}