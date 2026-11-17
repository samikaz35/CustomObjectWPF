// ============================================================================
// CustomObjectWPF — contrôles personnalisés (WPF)
// VERSION : 16
// RÈGLE : incrémenter VERSION de 1 à CHAQUE modification du fichier
// (même une petite), et ajouter une ligne dans l'historique ci-dessous.
// Ça permet de savoir quelle version du fichier on a sous les yeux.
// Historique des modifications :
//   v1 — Ajout de TextBoxCustom, PasswordBoxCustom, PasswordRevealCustom.
//   v2 — PasswordBoxCustom : ne peut pas hériter de PasswordBox (scellé,
//        CS0509) -> réécrit en wrapper Control autour d'un vrai PasswordBox.
//   v3 — BuildEyeButtonTemplate : nameof(Content) ne compile pas (CS0103)
//        -> "Content" en littéral.
//   v4 — Images de l'œil intégrées en base64 dans le .cs (plus de /Image/).
//   v5 — Avertissements nullable corrigés dans le nouveau code (champs ?).
//   v6 — Néon sur les 3 contrôles + taille de base et bordure sur les 2
//        mots de passe + œil en haute qualité + tentative EyeColor
//        (FindEyePart — n'a pas marché, voir v7).
//   v7 — Fond blanc par défaut sur les 2 mots de passe + œil sorti du
//        Contenu du bouton (EyeColor et changement d'œil réparés).
//   v8 — Œil teinté en recolorant l'image (même taille et position que
//        l'œil noir) + EyeHoverColor + le champ ne grandit plus avec
//        le texte (MeasureOverride plafonné à MinWidth).
//   v9 — Ajout de SearchBoxCustom (loupe + X + Watermark + événement
//        Search) et NumericUpDownCustom (Value/Minimum/Maximum/
//        Increment, boutons +/−, molette, flèches).
//   v10 — Ajout de ComboBoxCustom (ItemsSource/SelectedItem/
//        DisplayMemberPath/Placeholder, liste en Popup, fermeture sur
//        clic/Entrée/Échap/clic ailleurs) + passe de vérification
//        complète de la librairie.
//   v11 — Watermark (+ WatermarkColor) sur TextBoxCustom,
//        PasswordBoxCustom et PasswordRevealCustom (texte d'aide quand
//        le champ est vide, même taille que le texte, couleur pleine
//        sans opacité) + SearchBoxCustom : ClearIconColor (couleur du X
//        seule), SearchIcon/ClearIcon (images perso à la place de la
//        loupe et du X, comme les yeux).
//   v12 — Loupe + X INTÉGRÉS en base64 dans le .cs (comme les yeux) :
//        les formes dessinées sont retirées, les icônes sont des images
//        noires 512x512 teintables par IconColor/ClearIconColor.
//   v13 — Néon sur CardCustom + watermark toujours un peu transparent
//        (0,6, comme un vrai placeholder) + texte noir par défaut sur
//        les champs (le texte, le curseur et les points étaient
//        invisibles dans une fenêtre sombre) + watermark derrière le
//        texte (le curseur reste visible) + IconHoverColor /
//        ClearIconHoverColor sur SearchBoxCustom (comme EyeHoverColor).
//   v14 — Thème blanc : sélection de la liste déroulante lisible
//        (SelectedItemBackground/Foreground/BorderBrush, comme
//        ListBoxCustom) + la liste suit le champ quand on défile +
//        titre de CardCustom noir par défaut + survol des lignes de
//        ListBoxCustom + voiles de survol gris (le voile blanc était
//        invisible sur les champs blancs).
//   v15 — Bouton avec icône (Icon/IconWidth/IconSpacing) + clic sur la
//        loupe de la recherche (événement SearchIconClicked, sa propre
//        action) + Format d'affichage du nombre (décimales, prix, %)
//        + graisse du titre des cartes (TitleFontWeight).
//   v16 — Le survol ne cache plus la sélection (ListBox + liste du
//        ComboBox) : quand on survole un élément choisi, c'est son fond
//        (et son texte) qui restent. Watermark aligné avec le curseur
//        (décalé d'un Padding vers la droite). Texte du champ du
//        ComboBox lié au Foreground (noir par défaut).
// ============================================================================
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using WPF = System.Windows;

namespace CustomObjectWPF.Model
{
    #region Helpers

    /// <summary>
    /// Factorise l'effet néon partagé par tous les contrôles customs.
    /// Les DPs restent déclarés sur chaque contrôle (pour garder la syntaxe
    /// Neon="True" dans le XAML), mais le corps de l'effet n'est écrit qu'ici.
    /// NOTE : la cible doit être la couche visuelle de fond (pas le contrôle
    /// entier), sinon le texte devient flou (ClearType désactivé).
    /// </summary>
    public static class NeonHelper
    {
        public static void ApplyEffect(UIElement target, bool neon, Color color, double radius)
        {
            if (target == null) return;

            if (!neon)
            {
                target.Effect = null;
                return;
            }

            target.Effect = new DropShadowEffect
            {
                BlurRadius = radius,
                ShadowDepth = 0,
                Opacity = 1,
                Color = color
            };
        }
    }

    /// <summary>
    /// Convertit un double (espacement) en Thickness avec marge à gauche.
    /// Utilisé pour l'espacement case/label du CheckBoxCustom.
    /// </summary>
    public class DoubleToLeftMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = value is double v ? v : 0;
            return new Thickness(d, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Convertit un double (espacement) en Thickness avec marge à droite.
    /// Utilisé pour l'espacement icône/texte du ButtonCustom.
    /// </summary>
    public class DoubleToRightMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = value is double v ? v : 0;
            return new Thickness(0, 0, d, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Renvoie le premier Brush non nul parmi les valeurs liées
    /// (ex : HoverBackground, sinon Background).
    /// Sert aux triggers survol/clic du ButtonCustom : si l'utilisateur n'a
    /// pas défini HoverBackground/PressedBackground, le bouton garde son
    /// Background au lieu de passer à une couleur par défaut imposée.
    /// </summary>
    public class FirstNonNullBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object v in values)
            {
                if (v is Brush b) return b;
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region ButtonCustom

    public class ButtonCustom : Controls.Button
    {
        #region Dependency Properties

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(ButtonCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(ButtonCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(ButtonCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ButtonCustom),
                new PropertyMetadata(new CornerRadius(15)));

        // FIX (2026-11-05) : défaut null, pas gris clair imposé.
        // Avant, HoverBackground valait #E4E4E4 et PressedBackground #A8A8A8
        // par défaut : dès qu'on survolait un bouton SANS avoir défini ces
        // propriétés, le trigger mettait ce gris clair — sur les boutons
        // sombres de Samuel (Background #FF272727, texte blanc), le texte
        // devenait illisible. Avec null, le MultiBinding du template
        // retombe sur Background : le bouton garde son fond au survol/clic
        // tant qu'on n'a rien demandé d'autre (c'est aussi ce que dit la
        // Vitrine : « Si tu ne le mets pas, le bouton garde son Background »).
        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.Register(
                nameof(HoverBackground),
                typeof(Brush),
                typeof(ButtonCustom),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.Register(
                nameof(PressedBackground),
                typeof(Brush),
                typeof(ButtonCustom),
                new PropertyMetadata(null));

        // FIX (2026-11-08) : « bouton toujours pressé » suivi par nous-mêmes.
        // ButtonBase.IsPressed n'est PAS « bouton de souris enfoncé » : WPF le
        // met à false dès que la souris sort du bouton, même si on tient
        // toujours le clic (c'est le comportement normal d'un Button : le
        // Click ne se déclenche que si on relâche AU-DESSUS du bouton).
        // Donc le trigger sur IsPressed se désactivait quand on glissait
        // hors du bouton, et le fond retombait sur Background. IsPressedHeld,
        // lui, passe à true au PreviewMouseLeftButtonDown et ne retombe à
        // false qu'au relâché (ou perte de capture) : il reste vrai tant
        // qu'on tient le clic, souris dessus ou non. DP en lecture seule :
        // seul le contrôle lui-même peut le changer.
        private static readonly DependencyPropertyKey IsPressedHeldPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPressedHeld),
                typeof(bool),
                typeof(ButtonCustom),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsPressedHeldProperty =
            IsPressedHeldPropertyKey.DependencyProperty;

        #endregion

        /// <summary>
        /// Image affichée à gauche du texte (null = pas d'icône).
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(Media.ImageSource),
                typeof(ButtonCustom),
                new PropertyMetadata(null));

        /// <summary>
        /// Taille (en px) de l'icône, en largeur et en hauteur (carrée). Défaut : 20.
        /// </summary>
        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register(
                nameof(IconWidth),
                typeof(double),
                typeof(ButtonCustom),
                new PropertyMetadata(20.0));

        /// <summary>
        /// Espacement (en px) entre l'icône et le texte. Défaut : 8.
        /// </summary>
        public static readonly DependencyProperty IconSpacingProperty =
            DependencyProperty.Register(
                nameof(IconSpacing),
                typeof(double),
                typeof(ButtonCustom),
                new PropertyMetadata(8.0));

        #region Propriétés

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Fond affiché au survol de la souris.
        /// null (défaut) = garde le Background actuel.
        /// </summary>
        public Brush HoverBackground
        {
            get => (Brush)GetValue(HoverBackgroundProperty);
            set => SetValue(HoverBackgroundProperty, value);
        }

        /// <summary>
        /// Fond affiché pendant le clic.
        /// null (défaut) = garde HoverBackground si défini, sinon Background.
        /// </summary>
        public Brush PressedBackground
        {
            get => (Brush)GetValue(PressedBackgroundProperty);
            set => SetValue(PressedBackgroundProperty, value);
        }

        /// <summary>
        /// Vrai tant que le bouton gauche de la souris est tenu enfoncé
        /// après un clic sur ce bouton, même si la souris a glissé hors
        /// du bouton. Faux au relâché ou à la perte de capture.
        /// </summary>
        public bool IsPressedHeld
        {
            get => (bool)GetValue(IsPressedHeldProperty);
        }

        #endregion

        public Media.ImageSource? Icon
        {
            get => (Media.ImageSource?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public double IconWidth
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        public double IconSpacing
        {
            get => (double)GetValue(IconSpacingProperty);
            set => SetValue(IconSpacingProperty, value);
        }

        #region Constructeur

        public ButtonCustom()
        {
            // FIX (2026-11-07) : défauts posés avec SetCurrentValue, pas avec
            // une affectation directe. Une affectation (Background = ...) crée
            // une VALEUR LOCALE, qui a priorité sur les setters d'un Style
            // (doc Microsoft : « Dependency property value precedence » —
            // valeur locale = rang 3, setter de Style = rang 8). Conséquence :
            // un Style implicite ou explicite qui définissait Background,
            // Foreground, BorderThickness ou FontWeight était silencieusement
            // ignoré — le bouton restait gris clair / texte noir, et ça ne se
            // voyait pas dans InfiniteMouse ni dans la Vitrine parce que les
            // deux mettent Background en attribut direct (valeur locale aussi,
            // posée après le constructeur, donc elle gagne pareil). Avec
            // SetCurrentValue, les défauts ci-dessous s'appliquent quand rien
            // d'autre ne les définit, mais un Style, un trigger ou un binding
            // reprend le dessus normalement. Les 4 autres contrôles ne posent
            // pas de défauts dans leur constructeur, rien à changer pour eux.
            SetCurrentValue(BackgroundProperty, Media.Brushes.LightGray);
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            SetCurrentValue(BorderThicknessProperty, new Thickness(1));

            SetCurrentValue(FontWeightProperty, FontWeights.SemiBold);

            // FIX : le scale du clic pivote autour de ce point. (0.5, 0.5) = centre.
            // Sans ça, le défaut (0,0) = coin haut-gauche et le bouton "saute" à chaque pression.
            RenderTransformOrigin = new Point(0.5, 0.5);

            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué

            Template = BuildTemplate();

            // Note : l'ancien fondu au survol (Opacity 0.85) est remplacé par
            // HoverBackground, plus propre. Le scale au clic est conservé.
            MouseLeave += ButtonCustom_MouseLeave;
            PreviewMouseLeftButtonDown += ButtonCustom_Down;
            PreviewMouseLeftButtonUp += ButtonCustom_Up;
            LostMouseCapture += ButtonCustom_LostMouseCapture;
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ButtonCustom button)
            {
                button.RefreshNeon();
            }
        }

        #endregion

        #region Néon (lueur derrière le texte)

        // La lueur est appliquée sur le Border interne ("glowBorder"), PAS sur le
        // contrôle entier : le texte reste net (sinon ClearType désactivé + flou).
        private Border _glowBorder;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _glowBorder = Template.FindName("glowBorder", this) as Border;
            RefreshNeon();
        }

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Événements Souris

        private void ButtonCustom_MouseLeave(object sender, MouseEventArgs e)
        {
            // FIX : si le clic est relâché hors du bouton, PreviewMouseLeftButtonUp
            // ne passe jamais par lui. Sans cette ligne, le scale resterait à 0.97
            // ("écrasé") jusqu'au prochain clic.
            // MAIS : tant qu'on tient le clic (IsPressedHeld), on ne touche à
            // rien — le bouton est toujours pressé, juste la souris est sortie,
            // le scale 0.97 et le fond PressedBackground doivent rester.
            if (!IsPressedHeld)
            {
                RenderTransform = null;
            }
        }

        private void ButtonCustom_Down(object sender, MouseButtonEventArgs e)
        {
            // Le bouton reste « tenu » même si la souris glisse hors du
            // bouton ensuite (voir IsPressedHeld) : c'est ce flag, et non
            // IsPressed de WPF, qui pilote le fond pressé hors du bouton.
            SetValue(IsPressedHeldPropertyKey, true);
            RenderTransform = new ScaleTransform(0.97, 0.97);
        }

        private void ButtonCustom_Up(object sender, MouseButtonEventArgs e)
        {
            SetValue(IsPressedHeldPropertyKey, false);
            RenderTransform = null;
        }

        private void ButtonCustom_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // Sécurité : si la capture est perdue sans relâché propre
            // (fenêtre désactivée, autre contrôle qui capture...), on ne
            // reste jamais coincé en état pressé.
            SetValue(IsPressedHeldPropertyKey, false);
            RenderTransform = null;
        }

        #endregion

        #region Template

        private static Controls.ControlTemplate BuildTemplate()
        {
            // Structure : un Grid avec DEUX couches superposées.
            // - "glowBorder" : le fond du bouton, qui porte la lueur néon.
            // - le ContentPresenter : le texte, SANS effet -> reste net et lisible.
            FrameworkElementFactory grid = new(typeof(Controls.Grid));

            FrameworkElementFactory glowBorder = new(typeof(Controls.Border));
            glowBorder.Name = "glowBorder";

            glowBorder.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            glowBorder.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            glowBorder.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            glowBorder.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            // Contenu : une rangée horizontale (icône + texte), centrée.
            FrameworkElementFactory contentPanel = new(typeof(Controls.StackPanel));
            contentPanel.SetValue(Controls.StackPanel.OrientationProperty, Controls.Orientation.Horizontal);
            contentPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            contentPanel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            FrameworkElementFactory iconImage = new(typeof(Controls.Image));
            iconImage.Name = "iconImage";
            iconImage.SetBinding(
                Controls.Image.SourceProperty,
                new WPF.Data.Binding(nameof(Icon))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            iconImage.SetBinding(
                FrameworkElement.WidthProperty,
                new WPF.Data.Binding(nameof(IconWidth))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            iconImage.SetBinding(
                FrameworkElement.HeightProperty,
                new WPF.Data.Binding(nameof(IconWidth))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            iconImage.SetValue(Controls.Image.StretchProperty, Media.Stretch.Uniform);
            iconImage.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            // L'espacement icône/texte est la marge droite de l'icône.
            iconImage.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(IconSpacing))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent),
                    Converter = new DoubleToRightMarginConverter()
                });

            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));

            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding(nameof(Content))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            presenter.SetValue(
                Controls.ContentPresenter.HorizontalAlignmentProperty,
                WPF.HorizontalAlignment.Center);

            presenter.SetValue(
                Controls.ContentPresenter.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            contentPanel.AppendChild(iconImage);
            contentPanel.AppendChild(presenter);

            grid.AppendChild(glowBorder);
            grid.AppendChild(contentPanel);

            Controls.ControlTemplate template = new(typeof(ButtonCustom));
            template.VisualTree = grid;

            // Pas d'icône : on cache l'image (sinon elle prend de la place).
            Trigger noIconTrigger = new()
            {
                Property = IconProperty,
                Value = null
            };
            noIconTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "iconImage",
                Value = Visibility.Collapsed
            });
            template.Triggers.Add(noIconTrigger);

            // Survol : fond HoverBackground, ou Background si non défini
            // (FirstNonNullBrushConverter : premier Brush non nul).
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            MultiBinding hoverBinding = new() { Converter = new FirstNonNullBrushConverter() };
            hoverBinding.Bindings.Add(new WPF.Data.Binding(nameof(HoverBackground))
            {
                RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
            });
            hoverBinding.Bindings.Add(new WPF.Data.Binding(nameof(Background))
            {
                RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
            });
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "glowBorder",
                Value = hoverBinding
            });
            template.Triggers.Add(hoverTrigger);

            // Clic : fond PressedBackground (ajouté après = prioritaire sur le survol),
            // ou HoverBackground si défini, sinon Background.
            Trigger pressedTrigger = new()
            {
                Property = Controls.Primitives.ButtonBase.IsPressedProperty,
                Value = true
            };
            MultiBinding pressedBinding = new() { Converter = new FirstNonNullBrushConverter() };
            pressedBinding.Bindings.Add(new WPF.Data.Binding(nameof(PressedBackground))
            {
                RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
            });
            pressedBinding.Bindings.Add(new WPF.Data.Binding(nameof(HoverBackground))
            {
                RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
            });
            pressedBinding.Bindings.Add(new WPF.Data.Binding(nameof(Background))
            {
                RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
            });
            pressedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "glowBorder",
                Value = pressedBinding
            });
            template.Triggers.Add(pressedTrigger);

            // Clic tenu hors du bouton : IsPressed (WPF) retombe à false dès
            // que la souris sort, mais IsPressedHeld reste vrai tant qu'on
            // tient le clic. Ajouté après = prioritaire, même fond pressé.
            // Le trigger IsPressed ci-dessus est gardé : c'est lui qui couvre
            // le clic au clavier (Espace), où la souris n'est pas enfoncée.
            Trigger pressedHeldTrigger = new()
            {
                Property = IsPressedHeldProperty,
                Value = true
            };
            pressedHeldTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "glowBorder",
                Value = pressedBinding
            });
            template.Triggers.Add(pressedHeldTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region CheckBoxCustom

    public class CheckBoxCustom : Controls.CheckBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(CheckBoxCustom),
                new PropertyMetadata(new CornerRadius(0)));

        public static readonly DependencyProperty BorderHoverProperty =
            DependencyProperty.Register(
                nameof(BorderHover),
                typeof(Brush),
                typeof(CheckBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF2997DE"))));

        public static readonly DependencyProperty CheckColorProperty =
            DependencyProperty.Register(
                nameof(CheckColor),
                typeof(Brush),
                typeof(CheckBoxCustom),
                new PropertyMetadata(Media.Brushes.Black));

        /// <summary>
        /// Taille (côté, en px) de la case. Le crochet s'adapte automatiquement.
        /// </summary>
        public static readonly DependencyProperty BoxSizeProperty =
            DependencyProperty.Register(
                nameof(BoxSize),
                typeof(double),
                typeof(CheckBoxCustom),
                new PropertyMetadata(14.0));

        /// <summary>
        /// Espacement horizontal (en px) entre la case et le libellé.
        /// </summary>
        public static readonly DependencyProperty BoxSpacingProperty =
            DependencyProperty.Register(
                nameof(BoxSpacing),
                typeof(double),
                typeof(CheckBoxCustom),
                new PropertyMetadata(5.0));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(CheckBoxCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(CheckBoxCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(CheckBoxCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));

        #endregion

        #region Propriétés

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush BorderHover
        {
            get => (Brush)GetValue(BorderHoverProperty);
            set => SetValue(BorderHoverProperty, value);
        }

        public Brush CheckColor
        {
            get => (Brush)GetValue(CheckColorProperty);
            set => SetValue(CheckColorProperty, value);
        }

        public double BoxSize
        {
            get => (double)GetValue(BoxSizeProperty);
            set => SetValue(BoxSizeProperty, value);
        }

        public double BoxSpacing
        {
            get => (double)GetValue(BoxSpacingProperty);
            set => SetValue(BoxSpacingProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Constructeur

        public CheckBoxCustom()
        {
            Template = BuildTemplate();
            RefreshNeon();
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CheckBoxCustom chk)
            {
                chk.RefreshNeon();
            }
        }

        // Note : contrairement à l'ancienne version, CheckColor et BorderHover
        // n'ont plus besoin de reconstruire le template à chaque changement :
        // ce sont maintenant des bindings dynamiques (voir BuildTemplate).

        #endregion

        #region Néon (lueur sur la case, libellé net)

        // La lueur est appliquée sur la case ("checkBoxBorder"), pas sur le contrôle
        // entier : le libellé à côté reste net et lisible.
        private Border _glowBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _glowBox = Template.FindName("checkBoxBorder", this) as Border;
            RefreshNeon();
        }

        private void RefreshNeon()
        {
            if (_glowBox != null)
            {
                NeonHelper.ApplyEffect(_glowBox, Neon, NeonColor, NeonRadius);
                Effect = null;
            }
            else
            {
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            FrameworkElementFactory panel = new(typeof(Controls.StackPanel));

            panel.SetValue(Controls.StackPanel.OrientationProperty, Controls.Orientation.Horizontal);
            panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "checkBoxBorder";

            // Taille de la case pilotée par BoxSize (binding dynamique)
            border.SetBinding(
                Controls.Border.WidthProperty,
                new WPF.Data.Binding(nameof(BoxSize))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            border.SetBinding(
                Controls.Border.HeightProperty,
                new WPF.Data.Binding(nameof(BoxSize))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            // Crochet : binding dynamique sur CheckColor (pas de snapshot),
            // et pas de taille fixe -> il grandit avec BoxSize (Stretch Uniform).
            FrameworkElementFactory path = new(typeof(WPF.Shapes.Path));
            path.Name = "optionMark";

            path.SetValue(
                WPF.Shapes.Path.DataProperty,
                Geometry.Parse(
                    "F1 M 9.97498,1.22334L 4.6983,9.09834L 4.52164,9.09834L 0,5.19331L 1.27664,3.52165L 4.255,6.08833L 8.33331,1.52588e-005L 9.97498,1.22334 Z"));

            path.SetBinding(
                WPF.Shapes.Shape.FillProperty,
                new WPF.Data.Binding(nameof(CheckColor))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            path.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
            path.SetValue(WPF.Shapes.Shape.StretchProperty, Stretch.Uniform);
            path.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            path.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            path.SetValue(UIElement.OpacityProperty, 0.0);

            border.AppendChild(path);

            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));

            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding(nameof(Content))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            // Espacement case/libellé piloté par BoxSpacing (via converter)
            presenter.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(BoxSpacing))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent),
                    Converter = new DoubleToLeftMarginConverter()
                });

            presenter.SetValue(
                Controls.ContentPresenter.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            panel.AppendChild(border);
            panel.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(CheckBoxCustom));
            template.VisualTree = panel;

            // Coché : affiche le crochet.
            // (Le fond reste celui de Background, déjà implémenté par défaut sur CheckBox.)
            Trigger checkedTrigger = new()
            {
                Property = ToggleButton.IsCheckedProperty,
                Value = true
            };
            checkedTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                Value = 1.0,
                TargetName = "optionMark"
            });
            template.Triggers.Add(checkedTrigger);

            // Survol : bordure BorderHover (binding dynamique, pas de snapshot)
            Trigger mouseTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            mouseTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "checkBoxBorder",
                Value = new WPF.Data.Binding(nameof(BorderHover))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                }
            });
            template.Triggers.Add(mouseTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region ToggleSwitchCustom

    /// <summary>
    /// Interrupteur on/off style mobile.
    /// Hérite de CheckBox : IsChecked, Checked/Unchecked, bindings TwoWay
    /// et clavier (Espace) fonctionnent sans aucun code supplémentaire.
    /// </summary>
    public class ToggleSwitchCustom : Controls.CheckBox
    {
        #region Dependency Properties

        /// <summary>
        /// Couleur de la piste quand l'interrupteur est ON.
        /// </summary>
        public static readonly DependencyProperty OnBrushProperty =
            DependencyProperty.Register(
                nameof(OnBrush),
                typeof(Brush),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        /// <summary>
        /// Couleur de la piste quand l'interrupteur est OFF.
        /// </summary>
        public static readonly DependencyProperty OffBrushProperty =
            DependencyProperty.Register(
                nameof(OffBrush),
                typeof(Brush),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF555555"))));

        /// <summary>
        /// Couleur de la pastille mobile.
        /// </summary>
        public static readonly DependencyProperty ThumbBrushProperty =
            DependencyProperty.Register(
                nameof(ThumbBrush),
                typeof(Brush),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(Media.Brushes.White));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(ToggleSwitchCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));

        #endregion

        #region Propriétés

        public Brush OnBrush
        {
            get => (Brush)GetValue(OnBrushProperty);
            set => SetValue(OnBrushProperty, value);
        }

        public Brush OffBrush
        {
            get => (Brush)GetValue(OffBrushProperty);
            set => SetValue(OffBrushProperty, value);
        }

        public Brush ThumbBrush
        {
            get => (Brush)GetValue(ThumbBrushProperty);
            set => SetValue(ThumbBrushProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Constructeur

        public ToggleSwitchCustom()
        {
            Template = BuildTemplate();
            RefreshNeon();
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitchCustom toggle)
            {
                toggle.RefreshNeon();
            }
        }

        #endregion

        #region Néon (lueur sur la piste, libellé net)

        // La lueur est appliquée sur la piste ("switchTrack"), pas sur le contrôle
        // entier : le libellé à côté reste net et lisible.
        private Border _glowTrack;
        private Border _thumb;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _glowTrack = Template.FindName("switchTrack", this) as Border;
            _thumb = Template.FindName("switchThumb", this) as Border;
            RefreshNeon();

            // FIX (2026-11-05) : position initiale de la pastille.
            // Les EnterActions/ExitActions du trigger ne se déclenchent que sur
            // une TRANSITION de IsChecked, pas au chargement quand IsChecked
            // vaut déjà True : les setters du trigger, eux, s'appliquent dès
            // le départ (piste cyan), mais l'animation de glisse ne part pas.
            // Résultat sans ce fix : dans la Vitrine, les 3 toggles avec
            // IsChecked="True" affichent une piste cyan avec la pastille
            // restée à gauche — incohérent, et ça ne se répare qu'au premier
            // clic (décoche puis recoche). On place donc la pastille ici,
            // sans animation (c'est l'état initial, pas un mouvement) ; les
            // clics suivants passent toujours par les animations du trigger.
            if (_thumb != null)
            {
                _thumb.Margin = IsChecked == true
                    ? new Thickness(23, 0, 0, 0)
                    : new Thickness(3, 0, 0, 0);
            }
        }

        private void RefreshNeon()
        {
            if (_glowTrack != null)
            {
                NeonHelper.ApplyEffect(_glowTrack, Neon, NeonColor, NeonRadius);
                Effect = null;
            }
            else
            {
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            FrameworkElementFactory panel = new(typeof(Controls.StackPanel));
            panel.SetValue(Controls.StackPanel.OrientationProperty, Controls.Orientation.Horizontal);
            panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            // Piste (46x26, forme pilule)
            FrameworkElementFactory track = new(typeof(Controls.Border));
            track.Name = "switchTrack";
            track.SetValue(Controls.Border.WidthProperty, 46.0);
            track.SetValue(Controls.Border.HeightProperty, 26.0);
            track.SetValue(Controls.Border.CornerRadiusProperty, new CornerRadius(13));
            track.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(OffBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            // Pastille mobile (20px, centrée verticalement, à gauche par défaut)
            FrameworkElementFactory thumb = new(typeof(Controls.Border));
            thumb.Name = "switchThumb";
            thumb.SetValue(Controls.Border.WidthProperty, 20.0);
            thumb.SetValue(Controls.Border.HeightProperty, 20.0);
            thumb.SetValue(Controls.Border.CornerRadiusProperty, new CornerRadius(10));
            thumb.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(ThumbBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            thumb.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Left);
            thumb.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            thumb.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 0, 0, 0));

            track.AppendChild(thumb);

            // Libellé optionnel à droite
            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding(nameof(Content))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            presenter.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
            presenter.SetValue(Controls.ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            panel.AppendChild(track);
            panel.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(ToggleSwitchCustom));
            template.VisualTree = panel;

            // ON : piste colorée + la pastille GLISSE vers la droite
            // (mini animation de 150 ms au lieu de se téléporter).
            // Les triggers se désactivent seuls quand IsChecked=false -> retour à l'état OFF.
            Trigger onTrigger = new()
            {
                Property = ToggleButton.IsCheckedProperty,
                Value = true
            };
            onTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "switchTrack",
                Value = new WPF.Data.Binding(nameof(OnBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                }
            });

            // Glisse vers la droite : marge gauche 3 -> 23 (46 piste - 20 pastille - 3 marge).
            // Sans "From" : l'animation part de la valeur actuelle, donc ça marche
            // dans tous les cas (clic, binding, état initial).
            Storyboard slideOn = new();
            ThicknessAnimation animOn = new()
            {
                To = new Thickness(23, 0, 0, 0),
                Duration = new Duration(TimeSpan.FromMilliseconds(150))
            };
            Storyboard.SetTargetName(animOn, "switchThumb");
            Storyboard.SetTargetProperty(animOn, new PropertyPath(FrameworkElement.MarginProperty));
            slideOn.Children.Add(animOn);
            onTrigger.EnterActions.Add(new BeginStoryboard { Storyboard = slideOn });

            // Retour vers la gauche quand on décoche
            Storyboard slideOff = new();
            ThicknessAnimation animOff = new()
            {
                To = new Thickness(3, 0, 0, 0),
                Duration = new Duration(TimeSpan.FromMilliseconds(150))
            };
            Storyboard.SetTargetName(animOff, "switchThumb");
            Storyboard.SetTargetProperty(animOff, new PropertyPath(FrameworkElement.MarginProperty));
            slideOff.Children.Add(animOff);
            onTrigger.ExitActions.Add(new BeginStoryboard { Storyboard = slideOff });

            template.Triggers.Add(onTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region CardCustom

    /// <summary>
    /// Panneau "carte" : conteneur arrondi avec titre optionnel.
    /// Utile pour structurer des sections (ex: panneau d'options).
    /// Néon disponible comme sur les autres contrôles.
    /// </summary>
    public class CardCustom : ContentControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(CardCustom),
                new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Lueur autour de la carte. Défaut : false (pas de lueur).
        /// </summary>
        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(CardCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        /// <summary>
        /// Portée de la lueur (flou, en px). Défaut : 20.
        /// </summary>
        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(CardCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        /// <summary>
        /// Couleur de la lueur. Défaut : Cyan.
        /// </summary>
        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(CardCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CardCustom card)
            {
                card.RefreshNeon();
            }
        }

        /// <summary>
        /// Titre affiché en haut de la carte. Vide = pas de titre.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(CardCustom),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TitleForegroundProperty =
            DependencyProperty.Register(
                nameof(TitleForeground),
                typeof(Brush),
                typeof(CardCustom),
                new PropertyMetadata(Media.Brushes.Black));

        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register(
                nameof(TitleFontSize),
                typeof(double),
                typeof(CardCustom),
                new PropertyMetadata(14.0));

        /// <summary>
        /// Graisse du titre. Défaut : SemiBold.
        /// </summary>
        public static readonly DependencyProperty TitleFontWeightProperty =
            DependencyProperty.Register(
                nameof(TitleFontWeight),
                typeof(FontWeight),
                typeof(CardCustom),
                new PropertyMetadata(FontWeights.SemiBold));

        #endregion

        #region Propriétés

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }

        public FontWeight TitleFontWeight
        {
            get => (FontWeight)GetValue(TitleFontWeightProperty);
            set => SetValue(TitleFontWeightProperty, value);
        }

        #endregion

        #region Constructeur

        public CardCustom()
        {
            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Néon (lueur derrière le contenu)

        // La lueur est appliquée sur le Border interne ("glowBorder"), PAS sur le
        // contrôle entier : le titre et le contenu restent nets (sinon ClearType
        // désactivé + flou).
        private Border? _glowBorder;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _glowBorder = Template.FindName("glowBorder", this) as Border;
            RefreshNeon();
        }

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            FrameworkElementFactory grid = new(typeof(Controls.Grid));

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";

            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            FrameworkElementFactory panel = new(typeof(Controls.StackPanel));
            // Même retrait que l'ancien Padding (12) du Border : le contenu
            // ne touche pas les bords, mais le Border reste la couche de
            // fond (c'est elle qui porte le néon, pas le contenu).
            panel.SetValue(FrameworkElement.MarginProperty, new Thickness(12));

            FrameworkElementFactory title = new(typeof(Controls.TextBlock));
            title.Name = "cardTitle";
            title.SetBinding(
                Controls.TextBlock.TextProperty,
                new WPF.Data.Binding(nameof(Title))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            title.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(TitleForeground))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            title.SetBinding(
                Controls.TextBlock.FontSizeProperty,
                new WPF.Data.Binding(nameof(TitleFontSize))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            title.SetBinding(
                Controls.TextBlock.FontWeightProperty,
                new WPF.Data.Binding(nameof(TitleFontWeight))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            title.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 8));

            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding(nameof(Content))
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });

            panel.AppendChild(title);
            panel.AppendChild(presenter);
            grid.AppendChild(border);
            grid.AppendChild(panel);

            Controls.ControlTemplate template = new(typeof(CardCustom));
            template.VisualTree = grid;

            // Pas de titre -> on cache la zone de titre
            Trigger noTitleTrigger = new()
            {
                Property = TitleProperty,
                Value = string.Empty
            };
            noTitleTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                Value = Visibility.Collapsed,
                TargetName = "cardTitle"
            });
            template.Triggers.Add(noTitleTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region SliderCustom

    /// <summary>
    /// Slider custom : glisser de la pastille, clic sur la piste pour sauter,
    /// flèches du clavier. Value/Minimum/Maximum/Ticks hérités de Slider.
    /// Le template est reconstruit si Orientation change.
    /// </summary>
    public class SliderCustom : Slider
    {
        #region Dependency Properties

        /// <summary>
        /// Couleur de la piste.
        /// </summary>
        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(
                nameof(TrackBrush),
                typeof(Brush),
                typeof(SliderCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF555555"))));

        /// <summary>
        /// Couleur de la pastille.
        /// </summary>
        public static readonly DependencyProperty ThumbBrushProperty =
            DependencyProperty.Register(
                nameof(ThumbBrush),
                typeof(Brush),
                typeof(SliderCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        /// <summary>
        /// Diamètre (en px) de la pastille.
        /// </summary>
        public static readonly DependencyProperty ThumbSizeProperty =
            DependencyProperty.Register(
                nameof(ThumbSize),
                typeof(double),
                typeof(SliderCustom),
                new PropertyMetadata(18.0));

        /// <summary>
        /// Épaisseur (en px) de la piste.
        /// </summary>
        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(
                nameof(TrackHeight),
                typeof(double),
                typeof(SliderCustom),
                new PropertyMetadata(6.0));

        /// <summary>
        /// Forme de la piste : 0 = rectangle aux coins vifs,
        /// petite valeur = coins légèrement arrondis,
        /// grande valeur = pilule (défaut).
        /// </summary>
        public static readonly DependencyProperty TrackCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(TrackCornerRadius),
                typeof(CornerRadius),
                typeof(SliderCustom),
                new PropertyMetadata(new CornerRadius(1000)));

        /// <summary>
        /// Forme de la pastille : 0 = carré, grande valeur = rond (défaut).
        /// </summary>
        public static readonly DependencyProperty ThumbCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(ThumbCornerRadius),
                typeof(CornerRadius),
                typeof(SliderCustom),
                new PropertyMetadata(new CornerRadius(1000)));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(SliderCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(SliderCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(SliderCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));

        #endregion

        #region Propriétés

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public Brush ThumbBrush
        {
            get => (Brush)GetValue(ThumbBrushProperty);
            set => SetValue(ThumbBrushProperty, value);
        }

        public double ThumbSize
        {
            get => (double)GetValue(ThumbSizeProperty);
            set => SetValue(ThumbSizeProperty, value);
        }

        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        public CornerRadius TrackCornerRadius
        {
            get => (CornerRadius)GetValue(TrackCornerRadiusProperty);
            set => SetValue(TrackCornerRadiusProperty, value);
        }

        public CornerRadius ThumbCornerRadius
        {
            get => (CornerRadius)GetValue(ThumbCornerRadiusProperty);
            set => SetValue(ThumbCornerRadiusProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Border _track;
        private Thumb _thumb;
        private Grid _grid;

        #endregion

        #region Constructeur

        public SliderCustom()
        {
            Template = BuildTemplate();
            NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SliderCustom slider)
            {
                NeonHelper.ApplyEffect(slider, slider.Neon, slider.NeonColor, slider.NeonRadius);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Le layout dépend de l'orientation : on reconstruit le template.
            // (Changement rare, aucun impact en utilisation normale.)
            if (e.Property == OrientationProperty)
            {
                Template = BuildTemplate();
            }

            // ThumbSize influence la course de la pastille : on la repositionne
            // (sinon elle reste décalée jusqu'au prochain changement de valeur).
            if (e.Property == ThumbSizeProperty)
            {
                PositionThumb();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué)
            if (_thumb != null) _thumb.DragDelta -= Thumb_DragDelta;
            if (_grid != null) _grid.MouseLeftButtonDown -= Track_MouseDown;

            _track = Template.FindName("trackBorder", this) as Border;
            _thumb = Template.FindName("sliderThumb", this) as Thumb;
            // (Repli : si FindName ne retrouvait pas la grille racine,
            // on la prend comme premier enfant visuel — c'est elle.)
            _grid = Template.FindName("sliderGrid", this) as Grid
                ?? (VisualTreeHelper.GetChildrenCount(this) > 0
                    ? VisualTreeHelper.GetChild(this, 0) as Grid
                    : null);

            if (_thumb != null) _thumb.DragDelta += Thumb_DragDelta;
            // FIX (2026-11-05) : le clic est écouté sur la grille entière,
            // pas juste sur la piste de 6 px (voir Track_MouseDown).
            if (_grid != null) _grid.MouseLeftButtonDown += Track_MouseDown;

            PositionThumb();
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            PositionThumb();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            PositionThumb();
        }

        #endregion

        #region Positionnement

        private double TrackLength()
        {
            if (_track == null) return 0;
            return Orientation == Orientation.Horizontal ? _track.ActualWidth : _track.ActualHeight;
        }

        /// <summary>
        /// Place la pastille selon Value. Appelé à chaque changement de
        /// valeur, de taille ou de template.
        /// </summary>
        private void PositionThumb()
        {
            if (_track == null || _thumb == null) return;

            double range = Maximum - Minimum;
            double ratio = range <= 0 ? 0 : (Value - Minimum) / range;
            ratio = Math.Max(0, Math.Min(1, ratio)); // clamp 0..1

            // La pastille voyage sur (piste - sa propre taille)
            double pos = ratio * Math.Max(0, TrackLength() - ThumbSize);

            if (Orientation == Orientation.Horizontal)
            {
                _thumb.Margin = new Thickness(pos, 0, 0, 0);
            }
            else
            {
                // Vertical : 0 en bas. Avec VerticalAlignment=Bottom,
                // une marge basse = pos place la pastille en haut quand Value = max.
                _thumb.Margin = new Thickness(0, 0, 0, pos);
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double travel = Math.Max(0, TrackLength() - ThumbSize);
            if (travel <= 0) return;

            double range = Maximum - Minimum;
            double newValue;

            if (Orientation == Orientation.Horizontal)
            {
                newValue = Value + e.HorizontalChange / travel * range;
            }
            else
            {
                // Vers le haut (VerticalChange négatif) = augmenter
                newValue = Value - e.VerticalChange / travel * range;
            }

            Value = ApplySnap(newValue);
        }

        private void Track_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_track == null) return;

            // FIX (2026-11-05) : le handler est sur la grille entière, donc
            // un clic sur la pastille arrive aussi ici — on l'ignore, sinon
            // la valeur sauterait avant même que le drag commence.
            if (_thumb != null && _thumb.IsMouseOver) return;

            // Le clic donne le focus au slider : les flèches ← → marchent
            // juste après, sans avoir à faire Tab d'abord.
            Focus();

            double travel = Math.Max(0, TrackLength() - ThumbSize);
            if (travel <= 0) return;

            Point p = e.GetPosition(_track);
            double ratio;

            if (Orientation == Orientation.Horizontal)
            {
                ratio = (p.X - ThumbSize / 2) / travel;
            }
            else
            {
                ratio = 1 - (p.Y - ThumbSize / 2) / travel;
            }

            ratio = Math.Max(0, Math.Min(1, ratio));
            Value = ApplySnap(Minimum + ratio * (Maximum - Minimum));
        }

        /// <summary>
        /// Applique le snap sur les ticks quand IsSnapToTickEnabled.
        /// Fait explicitement ici car la valeur est posée par code (drag, clic,
        /// clavier) et on ne peut pas compter sur la coercition de la base.
        /// Idempotent : snapper une valeur déjà snappée ne change rien, donc
        /// aucun risque si la base le fait aussi.
        /// </summary>
        private double ApplySnap(double value)
        {
            if (!IsSnapToTickEnabled) return value;

            double freq = TickFrequency;
            if (freq <= 0) return value;

            double snapped = Math.Round((value - Minimum) / freq, MidpointRounding.AwayFromZero) * freq + Minimum;
            return Math.Max(Minimum, Math.Min(Maximum, snapped));
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            bool vertical = Orientation == Orientation.Vertical;

            // Binding "remonte d'un niveau" : utilisé pour les propriétés
            // de la pastille, dont le TemplatedParent est le Thumb lui-même.
            RelativeSource ancestor =
                new(RelativeSourceMode.FindAncestor, typeof(SliderCustom), 1);
            RelativeSource templatedParent =
                new(RelativeSourceMode.TemplatedParent);

            FrameworkElementFactory grid = new(typeof(Grid));
            grid.Name = "sliderGrid";
            grid.SetValue(Grid.BackgroundProperty, Brushes.Transparent); // zone cliquable

            if (vertical)
            {
                grid.SetBinding(
                    FrameworkElement.MinWidthProperty,
                    new WPF.Data.Binding(nameof(ThumbSize)) { RelativeSource = templatedParent });
            }
            else
            {
                grid.SetBinding(
                    FrameworkElement.MinHeightProperty,
                    new WPF.Data.Binding(nameof(ThumbSize)) { RelativeSource = templatedParent });
            }

            // Piste
            FrameworkElementFactory track = new(typeof(Border));
            track.Name = "trackBorder";
            // Forme pilotée par TrackCornerRadius (binding dynamique)
            track.SetBinding(
                Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(TrackCornerRadius)) { RelativeSource = templatedParent });
            track.SetBinding(
                Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(TrackBrush)) { RelativeSource = templatedParent });

            if (vertical)
            {
                track.SetBinding(
                    Border.WidthProperty,
                    new WPF.Data.Binding(nameof(TrackHeight)) { RelativeSource = templatedParent });
                track.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
                track.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            }
            else
            {
                track.SetBinding(
                    Border.HeightProperty,
                    new WPF.Data.Binding(nameof(TrackHeight)) { RelativeSource = templatedParent });
                track.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                track.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Stretch);
            }

            // Pastille : template avec bindings vers le SliderCustom
            Controls.ControlTemplate thumbTemplate = new(typeof(Thumb));
            FrameworkElementFactory thumbBorder = new(typeof(Border));
            // Forme pilotée par ThumbCornerRadius (binding dynamique)
            thumbBorder.SetBinding(
                Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(ThumbCornerRadius)) { RelativeSource = ancestor });
            thumbBorder.SetBinding(
                Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(ThumbBrush)) { RelativeSource = ancestor });
            thumbBorder.SetBinding(
                Border.WidthProperty,
                new WPF.Data.Binding(nameof(ThumbSize)) { RelativeSource = ancestor });
            thumbBorder.SetBinding(
                Border.HeightProperty,
                new WPF.Data.Binding(nameof(ThumbSize)) { RelativeSource = ancestor });
            thumbTemplate.VisualTree = thumbBorder;

            FrameworkElementFactory thumb = new(typeof(Thumb));
            thumb.Name = "sliderThumb";
            thumb.SetValue(Thumb.TemplateProperty, thumbTemplate);
            thumb.SetValue(FrameworkElement.FocusableProperty, false); // le focus reste sur le slider (clavier)

            if (vertical)
            {
                thumb.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
                thumb.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
            }
            else
            {
                thumb.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Left);
                thumb.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            }

            grid.AppendChild(track);
            grid.AppendChild(thumb);

            Controls.ControlTemplate template = new(typeof(SliderCustom));
            template.VisualTree = grid;

            return template;
        }

        #endregion
    }

    #endregion

    #region ListBoxCustom

    /// <summary>
    /// ListBox avec coins arrondis et couleurs de l'item sélectionné
    /// pilotables par propriétés (au lieu du bleu système de WPF).
    /// Hérite de ListBox : ItemsSource, SelectedItem, SelectionChanged,
    /// clavier (flèches) et scroll fonctionnent sans aucun code
    /// supplémentaire. Exemple d'usage dans la Vitrine / une Page :
    /// ItemsSource = liste, SelectedItem = l'objet choisi directement.
    /// </summary>
    public class ListBoxCustom : Controls.ListBox
    {
        #region Dependency Properties

        /// <summary>
        /// Coins arrondis de la liste elle-même. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ListBoxCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Coins arrondis de chaque item (la carte de fond de la ligne).
        /// 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty ItemCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(ItemCornerRadius),
                typeof(CornerRadius),
                typeof(ListBoxCustom),
                new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Fond de l'item sélectionné.
        /// </summary>
        public static readonly DependencyProperty SelectedItemBackgroundProperty =
            DependencyProperty.Register(
                nameof(SelectedItemBackground),
                typeof(Brush),
                typeof(ListBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        /// <summary>
        /// Couleur du texte de l'item sélectionné.
        /// </summary>
        public static readonly DependencyProperty SelectedItemForegroundProperty =
            DependencyProperty.Register(
                nameof(SelectedItemForeground),
                typeof(Brush),
                typeof(ListBoxCustom),
                new PropertyMetadata(Media.Brushes.Black));

        /// <summary>
        /// Couleur de la bordure de l'item sélectionné.
        /// </summary>
        public static readonly DependencyProperty SelectedItemBorderBrushProperty =
            DependencyProperty.Register(
                nameof(SelectedItemBorderBrush),
                typeof(Brush),
                typeof(ListBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        #endregion

        #region Propriétés

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public CornerRadius ItemCornerRadius
        {
            get => (CornerRadius)GetValue(ItemCornerRadiusProperty);
            set => SetValue(ItemCornerRadiusProperty, value);
        }

        public Brush SelectedItemBackground
        {
            get => (Brush)GetValue(SelectedItemBackgroundProperty);
            set => SetValue(SelectedItemBackgroundProperty, value);
        }

        public Brush SelectedItemForeground
        {
            get => (Brush)GetValue(SelectedItemForegroundProperty);
            set => SetValue(SelectedItemForegroundProperty, value);
        }

        public Brush SelectedItemBorderBrush
        {
            get => (Brush)GetValue(SelectedItemBorderBrushProperty);
            set => SetValue(SelectedItemBorderBrushProperty, value);
        }

        #endregion

        #region Constructeur

        public ListBoxCustom()
        {
            Template = BuildTemplate();
            // Style des lignes : si tu définis ton propre ItemContainerStyle
            // en XAML, le tien remplace celui-ci (posé après le constructeur).
            ItemContainerStyle = BuildItemContainerStyle();
        }

        #endregion

        #region Helpers

        // Binding vers une propriété du ListBoxCustom qui contient la ligne.
        // (Dans le template d'une ligne, TemplatedParent = la ligne elle-même,
        // pas la liste : il faut remonter d'un cran avec FindAncestor.)
        private static WPF.Data.Binding AncestorBinding(string path)
        {
            return new WPF.Data.Binding(path)
            {
                RelativeSource = new WPF.Data.RelativeSource(
                    WPF.Data.RelativeSourceMode.FindAncestor, typeof(ListBoxCustom), 1)
            };
        }

        #endregion

        #region Template de la liste

        private static Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Bordure externe : c'est elle qui porte le CornerRadius de la
            // liste, plus Background / BorderBrush / BorderThickness.
            // Le Padding de 4 garde les items à l'intérieur des coins arrondis.
            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "listBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });
            border.SetValue(Controls.Border.PaddingProperty, new Thickness(4));

            FrameworkElementFactory scroll = new(typeof(Controls.ScrollViewer));
            scroll.SetValue(Controls.ScrollViewer.VerticalScrollBarVisibilityProperty,
                Controls.ScrollBarVisibility.Auto);
            scroll.SetValue(Controls.ScrollViewer.HorizontalScrollBarVisibilityProperty,
                Controls.ScrollBarVisibility.Disabled);
            scroll.SetValue(Controls.ScrollViewer.CanContentScrollProperty, true);
            scroll.SetValue(FrameworkElement.FocusableProperty, false);

            FrameworkElementFactory presenter = new(typeof(Controls.ItemsPresenter));

            scroll.AppendChild(presenter);
            border.AppendChild(scroll);

            Controls.ControlTemplate template = new(typeof(ListBoxCustom));
            template.VisualTree = border;

            return template;
        }

        #endregion

        #region Style des lignes (items)

        private static Style BuildItemContainerStyle()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            Style style = new(typeof(Controls.ListBoxItem));
            style.Setters.Add(new Setter(Controls.ListBoxItem.BackgroundProperty,
                Media.Brushes.Transparent));
            style.Setters.Add(new Setter(Controls.ListBoxItem.BorderThicknessProperty,
                new Thickness(1)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.PaddingProperty,
                new Thickness(8, 6, 8, 6)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.MarginProperty,
                new Thickness(0, 0, 0, 4)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.HorizontalContentAlignmentProperty,
                WPF.HorizontalAlignment.Stretch));

            // Template d'une ligne : un Border (le fond de la ligne) +
            // un ContentPresenter. ContentTemplate est bindé aussi, sinon
            // un ItemTemplate / DataTemplate défini sur la liste serait ignoré.
            Controls.ControlTemplate itemTemplate = new(typeof(Controls.ListBoxItem));

            FrameworkElementFactory itemBorder = new(typeof(Controls.Border));
            itemBorder.Name = "itemBorder";
            itemBorder.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            itemBorder.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            itemBorder.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            itemBorder.SetBinding(
                Controls.Border.CornerRadiusProperty,
                AncestorBinding(nameof(ItemCornerRadius)));
            itemBorder.SetBinding(
                Controls.Border.PaddingProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.ContentTemplateProperty,
                new WPF.Data.Binding("ContentTemplate") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.ContentTemplateSelectorProperty,
                new WPF.Data.Binding("ContentTemplateSelector") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.HorizontalAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });
            presenter.SetValue(
                Controls.ContentPresenter.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            itemBorder.AppendChild(presenter);
            itemTemplate.VisualTree = itemBorder;

            // Sélectionné : fond + bordure + texte pilotés par les 3
            // propriétés du ListBoxCustom. Sans TargetName, le setter de
            // Foreground s'applique à la ligne elle-même (ListBoxItem) et
            // le texte hérite de sa couleur, sauf s'il définit la sienne.
            Trigger selectedTrigger = new()
            {
                Property = Controls.ListBoxItem.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "itemBorder",
                Value = AncestorBinding(nameof(SelectedItemBackground))
            });
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "itemBorder",
                Value = AncestorBinding(nameof(SelectedItemBorderBrush))
            });
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Control.ForegroundProperty,
                Value = AncestorBinding(nameof(SelectedItemForeground))
            });
            // Survolé : fond bleu très clair, texte inchangé (noir par défaut) : lisible.
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "itemBorder",
                Value = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFE5F1FB"))
            });
            itemTemplate.Triggers.Add(hoverTrigger);

            // La sélection gagne sur le survol : quand on survole un élément
            // choisi, c'est son fond (et son texte) qui restent — sinon un
            // texte blanc deviendrait illisible sur le fond clair du survol.
            itemTemplate.Triggers.Add(selectedTrigger);

            style.Setters.Add(new Setter(Controls.Control.TemplateProperty, itemTemplate));

            return style;
        }

        #endregion
    }

    #endregion

    #region TextBoxCustom

    /// <summary>
    /// TextBox classique avec coins arrondis (CornerRadius) et bordure
    /// qui change au focus (FocusedBorderBrush).
    /// Tout le reste est hérité de TextBox : Text, MaxLength, sélection,
    /// clavier, copier/coller, etc.
    /// </summary>
    public class TextBoxCustom : Controls.TextBox
    {
        #region Dependency Properties

        /// <summary>
        /// Coins arrondis du champ. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(TextBoxCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(TextBoxCustom),
                new PropertyMetadata(null));

        /// <summary>
        /// Texte d'aide affiché quand le champ est vide (ex. "Mot de passe").
        /// Vide (défaut) = pas de texte d'aide.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(TextBoxCustom),
                new PropertyMetadata(""));

        /// <summary>
        /// Couleur du texte d'aide. Défaut : gris #FF808080.
        /// (Couleur pleine, sans opacité : c'est elle qui fait la
        /// différence avec le texte tapé.)
        /// </summary>
        public static readonly DependencyProperty WatermarkColorProperty =
            DependencyProperty.Register(
                nameof(WatermarkColor),
                typeof(Brush),
                typeof(TextBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF808080"))));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(TextBoxCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(TextBoxCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(TextBoxCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public Brush WatermarkColor
        {
            get => (Brush)GetValue(WatermarkColorProperty);
            set => SetValue(WatermarkColorProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Constructeur

        public TextBoxCustom()
        {
            // Défauts neutres posés avec SetCurrentValue (pas d'affectation
            // directe) : un Style qui définit Padding ou
            // VerticalContentAlignment reprend le dessus normalement.
            // Padding 8,2 : le texte ne touche pas les coins arrondis.
            // VerticalContentAlignment Center : le texte est centré
            // verticalement dans un champ haut (ex : 42 px, comme ton login).
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBoxCustom txt)
            {
                txt.RefreshNeon();
            }
        }

        #endregion

        #region Néon (lueur sur la bordure, texte net)

        // La lueur est appliquée sur la bordure du champ ("glowBorder"),
        // PAS sur le contrôle entier : le texte reste net (sinon ClearType
        // désactivé + flou).
        private Border? _glowBorder;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _glowBorder = Template.FindName("glowBorder", this) as Border;
            RefreshNeon();
        }

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Grille racine : DEUX couches superposées.
            // - "glowBorder" : le fond + la bordure du champ (c'est elle
            //   qui porte la lueur néon).
            // - le ScrollViewer PART_ContentHost : le texte, SANS effet
            //   -> reste net et lisible. (Si l'effet était sur la bordure
            //   qui contient le texte, le ClearType serait désactivé.)
            FrameworkElementFactory grid = new(typeof(Controls.Grid));
            grid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            // PART_ContentHost : partie obligatoire de TextBoxBase.
            // C'est ce ScrollViewer qui affiche et édite le texte ;
            // sans lui (ou avec un autre nom), le champ reste vide.
            FrameworkElementFactory scroll = new(typeof(Controls.ScrollViewer));
            scroll.Name = "PART_ContentHost";
            scroll.SetValue(FrameworkElement.FocusableProperty, false);
            // Marge = le Padding du contrôle : le texte ne touche pas
            // les coins arrondis.
            scroll.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });


            // Texte d'aide : visible seulement quand le champ est vide
            // (trigger sur Text/Password = ""). Ne capte pas les clics :
            // on peut cliquer "à travers" pour mettre le focus dans le champ.
            // Taille = celle du texte (FontSize du contrôle), couleur =
            // WatermarkColor, toujours un peu transparente (0,6) : la couleur
            // se choisit, la transparence est fixe (pas de propriété pour ça).
            FrameworkElementFactory watermark = new(typeof(Controls.TextBlock));
            watermark.Name = "watermark";
            watermark.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            watermark.SetValue(UIElement.IsHitTestVisibleProperty, false);
            watermark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            // Toujours un peu transparente (0,6), comme un vrai placeholder :
            // la couleur se choisit avec WatermarkColor, la transparence est fixe.
            watermark.SetValue(UIElement.OpacityProperty, 0.6);
            watermark.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.TextProperty,
                new WPF.Data.Binding(nameof(Watermark)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(WatermarkColor)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            // Aligné avec le curseur : le texte est décalé d'un Padding vers
            // la droite (le champ applique son Padding deux fois : une fois
            // par la marge du modèle, une fois en interne), alors que le
            // watermark n'en a qu'un. On lui ajoute donc un Padding à gauche
            // égal au Padding du champ, sinon le curseur tombe au milieu de
            // la première lettre du texte d'aide.
            watermark.SetBinding(
                Controls.TextBlock.PaddingProperty,
                new WPF.Data.Binding("Padding.Left")
                {
                    RelativeSource = templatedParent,
                    Converter = new DoubleToLeftMarginConverter()
                });

            // Le watermark est DERRIÈRE le texte : le curseur (peint par le
            // ScrollViewer) reste visible, juste devant la première lettre.
            grid.AppendChild(border);
            grid.AppendChild(watermark);
            grid.AppendChild(scroll);

            Controls.ControlTemplate template = new(typeof(TextBoxCustom));
            template.VisualTree = grid;

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Champ vide : le texte d'aide apparaît.
            Trigger emptyTrigger = new()
            {
                Property = TextProperty,
                Value = ""
            };
            emptyTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "watermark",
                Value = Visibility.Visible
            });
            template.Triggers.Add(emptyTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region PasswordBoxCustom

    /// <summary>
    /// PasswordBox classique avec coins arrondis (CornerRadius) et bordure
    /// qui change au focus (FocusedBorderBrush).
    /// NOTE : PasswordBox est une classe SCELLÉE en WPF — on ne peut pas
    /// en hériter (erreur CS0509 : "cannot derive from sealed type").
    /// Ce contrôle est donc un wrapper : il hérite de Control et contient
    /// un vrai PasswordBox dans son template.
    /// Bonus par rapport au PasswordBox classique : Password est exposé
    /// comme DependencyProperty, donc BINDABLE en XAML (le PasswordBox
    /// d'origine ne le permet pas, par sécurité). PasswordChar et MaxLength
    /// sont relayés au champ interne.
    /// </summary>
    public class PasswordBoxCustom : Control
    {
        #region Dependency Properties

        /// <summary>
        /// Le mot de passe. Bindable (contrairement au PasswordBox d'origine).
        /// Synchronisé dans les deux sens avec le champ interne.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(string.Empty, OnPasswordChanged));

        /// <summary>
        /// Coins arrondis du champ. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(null));

        /// <summary>
        /// Texte d'aide affiché quand le champ est vide (ex. "Mot de passe").
        /// Vide (défaut) = pas de texte d'aide.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(""));

        /// <summary>
        /// Couleur du texte d'aide. Défaut : gris #FF808080.
        /// (Couleur pleine, sans opacité : c'est elle qui fait la
        /// différence avec le texte tapé.)
        /// </summary>
        public static readonly DependencyProperty WatermarkColorProperty =
            DependencyProperty.Register(
                nameof(WatermarkColor),
                typeof(Brush),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF808080"))));

        /// <summary>
        /// Caractère affiché à la place de chaque lettre. Défaut : • (rond plein).
        /// </summary>
        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordBoxCustom),
                new PropertyMetadata('●'));

        /// <summary>
        /// Longueur max du mot de passe (0 = illimitée).
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(0));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(PasswordBoxCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public Brush WatermarkColor
        {
            get => (Brush)GetValue(WatermarkColorProperty);
            set => SetValue(WatermarkColorProperty, value);
        }

        public char PasswordChar
        {
            get => (char)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Controls.PasswordBox? _innerPwd;

        // Bordure du champ, pour la lueur néon (voir RefreshNeon).
        private Border? _glowBorder;

        // Garde anti-boucle : quand on copie la valeur dans le champ interne,
        // PasswordChanged se redéclenche — sans ce flag, ça se recopierait
        // en boucle (voir PasswordRevealCustom).
        private bool _isSyncing;

        #endregion

        #region Constructeur

        public PasswordBoxCustom()
        {
            // Le champ interne est l'arrêt Tab (pas le wrapper) : une
            // seule tabulation pour entrer dans le champ, comme un
            // PasswordBox normal. (Affectation directe : c'est du
            // comportement, pas une valeur qu'un Style devrait poser.)
            Focusable = false;

            // Taille de base + bordure + fond comme le PasswordBox d'origine :
            // sans ça, le contrôle apparaît minuscule, sans bordure et sans
            // fond (Control n'a pas de BorderBrush/BorderThickness/Background
            // par défaut). Posés avec SetCurrentValue : un Style ou une valeur
            // en XAML reprend le dessus normalement.
            SetCurrentValue(BackgroundProperty, Media.Brushes.White);
            SetCurrentValue(BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            SetCurrentValue(BorderThicknessProperty, new Thickness(1));
            SetCurrentValue(MinWidthProperty, 200.0);
            SetCurrentValue(MinHeightProperty, 32.0);

            // Mêmes défauts neutres que TextBoxCustom (voir son constructeur).
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBoxCustom c)
            {
                c.SyncPasswordToInner();
            }
        }


        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBoxCustom pwd)
            {
                pwd.RefreshNeon();
            }
        }

        #endregion

        #region Synchronisation

        /// <summary>
        /// Copie Password dans le champ interne (si différent).
        /// Appelé quand Password change en code et au chargement du template.
        /// </summary>
        private void SyncPasswordToInner()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                string pwd = Password ?? string.Empty;
                if (_innerPwd != null && _innerPwd.Password != pwd)
                {
                    _innerPwd.Password = pwd;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void InnerPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (_innerPwd == null) return;
            _isSyncing = true;
            try
            {
                Password = _innerPwd.Password;
            }
            finally
            {
                _isSyncing = false;
            }
        }


        #endregion

        #region Néon (lueur sur la bordure, texte net)

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué).
            if (_innerPwd != null) _innerPwd.PasswordChanged -= InnerPwd_PasswordChanged;

            _innerPwd = Template.FindName("innerPwd", this) as Controls.PasswordBox;
            _glowBorder = Template.FindName("glowBorder", this) as Border;

            if (_innerPwd != null) _innerPwd.PasswordChanged += InnerPwd_PasswordChanged;

            // État initial : la valeur posée en XAML/code avant le
            // chargement doit remplir le champ interne.
            SyncPasswordToInner();
            RefreshNeon();
        }

        /// <summary>
        /// Largeur désirée : si le conteneur ne donne pas de largeur
        /// (Canvas, colonne Auto, StackPanel...), la largeur désirée
        /// serait celle du mot de passe et le champ grandirait à chaque
        /// lettre tapée. On plafonne à MinWidth (200 par défaut) : le
        /// champ garde sa taille et le texte défile dedans, comme un
        /// PasswordBox classique dans une grille.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            if (double.IsInfinity(availableSize.Width))
            {
                size.Width = MinWidth;
            }
            return size;
        }

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Bordure externe : c'est elle qui porte Background,
            // BorderBrush, BorderThickness et CornerRadius. Le champ
            // interne est transparent et sans bordure.
            // Grille racine : "glowBorder" (fond + bordure, porte la
            // lueur néon) derrière le champ interne (texte net).
            FrameworkElementFactory grid = new(typeof(Controls.Grid));
            grid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });


            FrameworkElementFactory innerPwd = new(typeof(Controls.PasswordBox));
            innerPwd.Name = "innerPwd";
            innerPwd.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            innerPwd.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            innerPwd.SetBinding(
                Controls.Control.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.FontFamilyProperty,
                new WPF.Data.Binding(nameof(FontFamily)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.FontStyleProperty,
                new WPF.Data.Binding(nameof(FontStyle)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.FontWeightProperty,
                new WPF.Data.Binding(nameof(FontWeight)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.FontStretchProperty,
                new WPF.Data.Binding(nameof(FontStretch)) { RelativeSource = templatedParent });
            // Marge = le Padding du contrôle (le texte ne touche pas les
            // coins) ; Padding interne à 0 pour ne pas le compter 2 fois.
            innerPwd.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });
            innerPwd.SetValue(Controls.Control.PaddingProperty, new Thickness(0));
            innerPwd.SetBinding(
                Controls.Control.VerticalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(VerticalContentAlignment)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.Control.HorizontalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.PasswordBox.PasswordCharProperty,
                new WPF.Data.Binding(nameof(PasswordChar)) { RelativeSource = templatedParent });
            innerPwd.SetBinding(
                Controls.PasswordBox.MaxLengthProperty,
                new WPF.Data.Binding(nameof(MaxLength)) { RelativeSource = templatedParent });


            // Texte d'aide : visible seulement quand le champ est vide
            // (trigger sur Text/Password = ""). Ne capte pas les clics :
            // on peut cliquer "à travers" pour mettre le focus dans le champ.
            // Taille = celle du texte (FontSize du contrôle), couleur =
            // WatermarkColor, toujours un peu transparente (0,6) : la couleur
            // se choisit, la transparence est fixe (pas de propriété pour ça).
            FrameworkElementFactory watermark = new(typeof(Controls.TextBlock));
            watermark.Name = "watermark";
            watermark.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            watermark.SetValue(UIElement.IsHitTestVisibleProperty, false);
            watermark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            // Toujours un peu transparente (0,6), comme un vrai placeholder :
            // la couleur se choisit avec WatermarkColor, la transparence est fixe.
            watermark.SetValue(UIElement.OpacityProperty, 0.6);
            watermark.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.TextProperty,
                new WPF.Data.Binding(nameof(Watermark)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(WatermarkColor)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            // Aligné avec le curseur : le texte est décalé d'un Padding vers
            // la droite (le champ applique son Padding deux fois : une fois
            // par la marge du modèle, une fois en interne), alors que le
            // watermark n'en a qu'un. On lui ajoute donc un Padding à gauche
            // égal au Padding du champ, sinon le curseur tombe au milieu de
            // la première lettre du texte d'aide.
            watermark.SetBinding(
                Controls.TextBlock.PaddingProperty,
                new WPF.Data.Binding("Padding.Left")
                {
                    RelativeSource = templatedParent,
                    Converter = new DoubleToLeftMarginConverter()
                });

            // Le watermark est DERRIÈRE le champ : les points du mot de passe
            // et le curseur peignent par-dessus, toujours visibles.
            grid.AppendChild(border);
            grid.AppendChild(watermark);
            grid.AppendChild(innerPwd);

            Controls.ControlTemplate template = new(typeof(PasswordBoxCustom));
            template.VisualTree = grid;

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Champ vide : le texte d'aide apparaît.
            Trigger emptyTrigger = new()
            {
                Property = PasswordProperty,
                Value = ""
            };
            emptyTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "watermark",
                Value = Visibility.Visible
            });
            template.Triggers.Add(emptyTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region PasswordRevealCustom

    /// <summary>
    /// Champ mot de passe 2-en-1 : un PasswordBox + un TextBox + un bouton
    /// œil dans un seul contrôle. L'œil (ou IsPasswordRevealed en code)
    /// bascule entre masqué (PasswordBox, caractères •) et visible (TextBox,
    /// texte en clair). Toutes les propriétés visuelles (Background,
    /// Foreground, BorderBrush, CornerRadius, taille de police...) sont
    /// portées par le contrôle lui-même et appliquées aux DEUX champs :
    /// elles restent donc identiques malgré le switch (ex : Background
    /// rouge en mode texte = rouge aussi en mode mot de passe).
    /// Exemple d'usage :
    ///   &lt;custom:PasswordRevealCustom Password="{Binding MotDePasse}"
    ///       IsPasswordRevealed="False" CornerRadius="10" Height="42" /&gt;
    /// </summary>
    public class PasswordRevealCustom : Control
    {
        #region Images intégrées (base64)

        // Les deux icônes œil sont INTÉGRÉES au fichier (base64) : aucun
        // fichier externe, aucun dossier /Image/ requis — le contrôle est
        // autonome, le .cs voyage seul. Les PNG d'origine de Samuel (noirs
        // sur fond transparent) sont encodés tels quels ; RevealImage /
        // HideImage restent modifiables en XAML pour mettre d'autres images.
        // Pour régénérer ces chaînes à partir d'autres PNG, en PowerShell :
        //   [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\chemin\image.png"))

        private const string RevealImageBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAwwAAAIfCAYAAADdSlv6AAAQAElEQVR4Aezdi5XctrIo0LkvQjsDOwM7AskR2BnYGdihvQzu7ZJE" +
            "TU+rP/wARAHYZw3UM90kULVBgCzpyPp/b/5HgAABAgQIECBAgACBBwIKhgcw3ibQn4CICRAgQIAAAQLlBRQM5U31SIAAAQIEjgk4" +
            "mwABAokEFAyJJkMoBAgQIECAAAECYwmMkI2CYYRZlAMBAgQIECBAgACBSgIKhkqwuu1NQLwECBAgQIAAAQL3BBQM91S8R4AAAQL9" +
            "CoicAAECBIoKKBiKcuqMAAECBAgQIECglIB+cggoGHLMgygIECBAgAABAgQIpBRQMKSclt6CEi8BAgQIECBAgMCoAgqGUWdWXgQI" +
            "ENgj4BwCBAgQIHAjoGC4AfEjAQIECBAgQGAEATkQKCWgYCglqR8CBAgQIECAAAECAwooGJpPqgAIECBAgAABAgQI5BVQMOSdG5ER" +
            "INCbgHgJECBAgMCAAgqGASdVSgQIECBAgMAxAWcTIPAuoGB4t/AdAQIECBAgQIAAAQI3Ap0XDDfZ+JEAAQIECBAgQIAAgaICCoai" +
            "nDojQGC3gBMJECBAgACBlAIKhpTTIigCBAgQINCvgMgJEBhLQMEw1nzKhgABAgQIECBAgEApgS/9KBi+MPiFAAECBAgQIECAAIF7" +
            "AgqGeyreI9CbgHgJECBAgAABApUEFAyVYHVLgAABAgT2CDiHAAEC2QQUDNlmRDwECBAgQIAAAQIjCAyTg4JhmKmUCAECBAgQIECA" +
            "AIHyAgqG8qZ67E1AvAQIECBAgAABAg8FFAwPaXxAgAABAr0JiJcAAQIEygsoGMqb6pEAAQIECBAgQOCYgLMTCSgYEk2GUAgQIECA" +
            "AAECBAhkE1AwZJuR3uIRLwECBAgQIECAwNACCoahp1dyBAgQWC/gSAIECBAgcE9AwXBPxXsECBAgQIAAgX4FRE6gqICCoSinzggQ" +
            "IECAAAECBAiMJaBgaDmfxiZAgAABAgQIECCQXEDBkHyChEeAQB8CoiRAgAABAqMKKBhGnVl5ESBAgAABAnsEnEOAwI2AguEGxI8E" +
            "CBAgQIAAAQIECLwL9FswvOfgOwIECBAgQIAAAQIEKgkoGCrB6pYAgfUCjiRAgAABAgTyCigY8s6NyAgQIECAQG8C4iVAYEABBcOA" +
            "kyolAgQIECBAgAABAscE3s9WMLxb+I4AAQIECBAgQIAAgRsBBcMNiB8J9CYgXgIECBAgQIBATQEFQ01dfRMgQIAAgfUCjiRAgEBK" +
            "AQVDymkRFAECBAgQIECAQL8CY0WuYBhrPmVDgAABAgQIECBAoKiAgqEop856ExAvAQIECBAgQIDAcwEFw3MfnxIgQIBAHwKiJECA" +
            "AIFKAgqGSrC6JUCAAAECBAgQ2CPgnGwCCoZsMyIeAgQIECBAgAABAokEFAyJJqO3UMRLgAABAgQIECAwvoCCYfw5liEBAgReCfic" +
            "AAECBAg8FFAwPKTxAQECBAgQIECgNwHxEigvoGAob6pHAgQIECBAgAABAsMIKBgaTaVhCRAgQIAAAQIECPQgoGDoYZbESIBAZgGx" +
            "ESBAgACBoQUUDENPr+QIECBAgACB9QKOJEDgnoCC4Z6K9wgQIECAAAECBAgQ+CLQZcHwJXK/ECBAgAABAgQIECBQXUDBUJ3YAAQI" +
            "PBHwEQECBAgQIJBcQMGQfIKER4AAAQIE+hAQJQECowooGEadWXkRIECAAAECBAgQ2CNwc46C4QbEjwQIECBAgAABAgQIvAsoGN4t" +
            "fEegNwHxEiBAgAABAgSqCygYqhMbgAABAgQIvBLwOQECBPIKKBjyzo3ICBAgQIAAAQIEehMYMF4Fw4CTKiUCBAgQIECAAAECpQQU" +
            "DKUk9dObgHgJECBAgAABAgRWCCgYViA5hAABAgQyC4iNAAECBGoKKBhq6uqbAAECBAgQIEBgvYAjUwooGFJOi6AIECBAgAABAgQI" +
            "5BBQMOSYh96iEC8BAmMJfBorHdkQIECAQEkBBUNJTX0RIECgvMD/Xrqs2N6i788Vxrh06YsAAQIERhBQMIwwi3IgQKBHgXhQX9N6" +
            "zC1iXpNbHBPHagQIlBDQB4FKAgqGSrC6JUBgaoG/L9nHw/CzdjnE10XgmdHy2eUwXwQIECDQSkDBcL68EQkQGEdgeaC9ff1lnBRT" +
            "ZHLru/ycIjhBECBAYHQBBcPoMyw/AgRKCCwPqDevJbrWxwGB2/lYfj7QpVMJECBA4FZAwXAr4mcCBGYWWB44b19nNukx99v5W37u" +
            "MRcxnyVgHAIEHgooGB7S+IAAgQkElgfJ5XWClKdOcZnn5XVqDMkTIEBgrUBvBcPavBxHgACBW4HlIfH69fYYP88lcH0tLN/PJSBb" +
            "AgQIrBBQMKxAcggBAjUEqvYZ/xDZ8gC4vFYdUOfDCCzXy/XrMMlJhAABAnsEFAx71JxDgEBGgesHvPiHyDLGKKY+Ba6vrfhP5vaZ" +
            "Rc2o9U2AwNACCoahp1dyBIYWuH6Ii++HTlZyaQTiP5kb19t1SxOcQAgQIHBU4N75CoZ7Kt4jQCCjwPUDWnyfMUYxzSkQ1+PS5hSQ" +
            "NQECQwsoGIaeXsmNKzBFZrd/D2GKpCXZvcBSOCyv3SckAQIECCgYXAMECGQSWB6y4tXfQ8g0M2LZKxDX8tLu9+FdAgQIJBdQMCSf" +
            "IOERmEBgeZiK1wnSleLEAnGNX7eJKaROYEyBUbNSMIw6s/IikFfg+oEpvs8bqcgI1BWI639pdUfSOwECBA4IKBgO4Dm1VwFxNxBY" +
            "HoritcHwhiSQXiDWxtLSBytAAgTmElAwzDXfsiVwpsDy8BOvZ45rrJkExsw11szSxsxQVgQIdCWgYOhqugRLIL3A8pATr+mDFSCB" +
            "DgRiLS2tg3CFSGC/gDPzCigY8s6NyAj0JOCBpqfZEmuvAss6i9decxA3AQIdCigYOpy0tiEbncAXAf9GwhcGvxBoJhBFw9KaBWFg" +
            "AgTmEFAwzDHPsiRQSmB5QPFvJJQSbdmPsUcRWNbln6MkJA8CBHIJKBhyzYdoCGQUWB5G4jVjfGIiQOCrwG+Xl1in0S7f+ppJQK4E" +
            "agooGGrq6ptAvwLxwLG0frMQOYF5BZb1G6/zKsicAIEiAgqGIoxrO3EcgfQC8XARLX2gAiRAYLVArOloq09wIAECBK4FFAzXGr4n" +
            "MK9APExEm1dga+aOJ9CfQKzxaP1FLmICBJoKKBia8hucQFOBeHBYWtNADE6AwKkCy7qP11MHzjqYuAgQeC6gYHju41MCIwrEQ0K0" +
            "EXOTEwEC2wRiL1jatjMdTYDANAIdFQzTzIlECdQQWB4I4rVG//okQKB/gdgfovWfiQwIECgqoGAoyqkzAukE4uYfLVdgoiFAILNA" +
            "7BnR/LsOmWdJbAROFFAwnIhtKAInCcSNfmknDWkYAgQGFFj17zoMmLeUCBC4EVAw3ID4kUDHAoqEjidP6ASSC9hfkk+Q8AgUEHjY" +
            "hYLhIY0PCHQh8PclSjfyC4IvAgROEVj2m3g9ZUCDECDQXkDB0H4OREBgm8D70XHD/uX9R99NLPA/l9zPbP9cxvNFIPYgCgQITCCg" +
            "YJhgkqU4lEDcoJc2VGKSuSvw3+XdNYXA5bBTv369jJYxrktYfX0NEO2yH8XrAOlIgQCBewIKhnsq3iOQTyBuxtHyRSaiUgL3HsB/" +
            "LtV5o37u5dQoFMOeIBB7VLQThjIEgXQCQwekYBh6eiU3gEDcfKMNkIoUrgRmfpCeOferS2Dob2PPijZ0kpIjMJOAgmGm2Zbr21s/" +
            "BnGzjdZPxCJ9JnD7kPzs2Bk/u/X5PCPCgDnHHhZtwNSkRGAuAQXDXPMt2/wCcXONlj9SET4SuH34jZ8fHev9+wJ/XN4Ot+t2eevj" +
            "l5+6EYg9LVo3AQuUAIGPAgqGjx5+ItBKIG6m0VqNb9xjAh5sj/mtOZvxGqXcx8QeFy13lKJrIWDM5AIKhuQTJLzhBeLmGW34RAdL" +
            "8PrhNb4fLL306YT5dUsfsAA/CMSeF+3Dm34gQCCvgIIh79zki0xEJQXiZhmtZJ/6qivgAbWu75Hezc0RvXbnxh4YrV0ERiZAYJWA" +
            "gmEVk4MIFBOIm2O0Yh3qqLrA8jBafaAzBxh4rGW+4t+wGDjNoVKLPTHaUElJhsBIAgqGkWZTLpkF4mYYLXOMYnsXWB464/X9Xd/1" +
            "JBD/hkXMX7Se4p451tgjo81ssCd35xCoLqBgqE5sgMkF4uYXbXKGbtKPh8to3QQs0FUCMadLW3WCg5oKxJ4ZrWkQBidA4F1AwfBu" +
            "Ufc7vc8mEDe7aLPl3WO+y4NkvPYYv5i3CcQ8R9t2lqNbCMQeGq3F2MYkQOBKQMFwheFbAgUE4uYWrUBXuqgoEA+MS9s8jBOGEFjm" +
            "P16HSGjgJGJPjTZwilIjkFtAwZB7fkTXj0DczKL1E/GckcbDYbQ5s5f1I4G4JqI9+tz7OQRij41WMhp9ESCwQkDBsALJIQSeCMTN" +
            "K9qTQ3yUQCAeBqMlCEUIiQXiGomWOEShXQRiz412+dYXAQJnCPRRMJwhYQwC2wTiZhVt21mOPlsgHv6inT2u8foWiGsmWt9ZjB99" +
            "7MHRxs9UhgQaCygYGk+A4bsUcIM6MG0nnRoPe9FOGs4wgwrENRRt0PSGSSv25D+HyUYiBBIKKBgSToqQ0grETSla2gAF9hYPd9FQ" +
            "ECgpENdUtJJ9jtBXphx+uwRjf74g+CJQQ0DBUENVn6MJxE0o2mh5jZRPPMxFGyknueQTiGssWr7IRLQIxF4dbfnZKwECqwSeH6Rg" +
            "eO7j07kF/r6k78ZzQUj+5QEu+QQNGJ5rLv+kxt79b/4wRUigDwEFQx/zJMrzBeJm88v5wz4f0affBeKBbWnf3/QNgRMFlusvXk8c" +
            "1lAbBH66HBt7+eXFFwECRwQUDEf0nDuiQNxcoo2Y2yg5eUAbZSbHyWPPNTlO9vkziT09Wv5IRUggqYCCIenECOt0gbiZRDt9YAOu" +
            "FoiHsmirT3AggRMF4tqMduKQhtooEHt8tI2nOZzAK4HxP1cwjD/HMnwt4Aby2qjlEfEQFq1lDMYmsFYgrtVoa4933PkCsef7z7Ce" +
            "727EjgUUDB1PntC3Cdw5Om4a0e585K0kAh68kkyEMDYLuHY3k516gv8M66ncButdQMHQ+wyKf49AFAnR9pzrnHME4mEr2jmjGaU3" +
            "gV7ijWs4Wi/xzhhn3AuizZi7nAmsFlAwrKZy4CACbgz5J9IDVv45EuE2Adf0Nq8WR7s3tFD/MqZfehBQMPQwS2IsIRA3g2gl+tJH" +
            "HYF4qIpWp3e9EmgrENd2tLZRGP2ZQNwjoj07xmcEphRQMEw57duT7viM2PyjdZzCFKF7kJpimiV5EXCtXxCSf8U9I1ryMIVH4DwB" +
            "BcN51kY6X8CGf7751hH/uZzgAeqCcOKXodoLxDUfrX0kIngm4B7yTMdnUwkoGKaa7mmSjU0+2jQJd5poPDD92mnswiZQQiDWQIl+" +
            "9FFPIO4l0eqNcKhnU4ZH4AAAEABJREFUJxM4R0DBcI6zUc4RiE092jmjGeWIgAelI3rOHUkg1sJfIyU0aC5xb4k2aHrSIvBcQMHw" +
            "3KfIpzo5RcBGfgrz4UHi4Sja4Y50QGAggd8vuVgXF4QOvtxrOpgkIZYXUDCUN9XjuQKxeUc7d1Sj7REY4YFoT97OIbBWwBpZK9X2" +
            "uLjnRGsbhdEJnCigYDgR21DFBWzYxUmrdehBqBqtjgcTsFZOm9DDA7kHHSbUQS8CCoZeZkqc1wKxSUe7fs/3eQU8AOWdG5HlFLBm" +
            "cs7LvajiXhTt3mfeIzCMQPqCYRhpiZQSsDGXkjynHw8+5zgbZTyBWDvRxstszIzcm8acV1l9E1AwfIPwkl4gNuNo6QMV4BeBeNCJ" +
            "9uWHb794IUBgu4B1tN2s1Rlxj4rWanzjEqgmoGCoRqvjQgKx+UYr1J1uThDwgHMCsiGmEki4pqby35ps3LOibT3P8QTSCigY0k6N" +
            "wC4CNtwLQmdfHmw6mzDhdiNgbXUzVd8DdQ/7TuGb1AIrglMwrEBySBMBG20T9kODeqA5xOdkAi8FYo1Fe3mgA9IIxL0sWpqABEJg" +
            "j4CCYY+ac2oKxMYareYYPfadPWYPMdlnSHwjCVhv/c2m+1p/cybiKwEFwxWGb5sL2FCbT8GuADy87GJz0rwCRTK37oowntpJ3OOi" +
            "nTqowQiUEFAwlFDUx1GB2ECjHe3H+ecKxANLtHNHNRoBAouA9bdI9PXqftfXfD2PdpJPFQyTTHTiNG2ciSfnSWgeVJ7g+IjAiQLW" +
            "4onYBYeKe1+0gl3qikA9AQVDPVs9PxeIjTLa86PKfKqXsgIeUMp66o3AUQFr8qhgu/PdB9vZG3mDgIJhA5ZDiwnYIItRnt6RB5PT" +
            "yQ34UcBPDwSszQcwHbztntjBJM0eooJh9ivg3PxjU4x27qhGKyXggaSUpH4I1BGwRuu4ntFr3BujnTFWnjFE0o2AgqGbqeo+UBth" +
            "31PoQaTv+RP9PALWat9z7V7Z9/wNG72CYdipLZbY0Y5i84t2tB/ntxPwANLO3sgE9ghYs3vU8pwT98xoeSISyfQCCobpL4GqADa8" +
            "qryndO7B4xTmswYxzkQC1m7/k+0e2v8cDpOBgmGYqUyXiI0u3ZRsDsgDx2YyJxBIJWANp5qOXcHEvTTajyd7h8CJAgqGE7EnGSo2" +
            "tmiTpDtsmv8Mm5nECMwloGgYY77dV8eYx26zUDDUnbrZerehjTPjv46TikwITC+gaBjjEnCPHWMeu8xCwdDltKUM2kaWclp2BeXh" +
            "4i5bN2/GWizZ/uwmc4E+E7Cun+n081ms7U/9hCvSUQQUDKPMZLs8YvOK1i4CI5cU8FBRUrNeX7HmHrXSo/526fCssS5D+SJwgkDf" +
            "Q3y+hB9r8vLii8A5AgqGc5xHHcWGNdbMKhZyzmess9uWJdLbuJafs8Qnjo8C1vhHj95/st56n8GO4s9cMHTEOF2oNqnxptyDRJ45" +
            "XdbX8ponsvWRLLHH6/qzHHmGgLV+hvK5Y1hn53pPOZqCYcppP5S0jekQX8qTT3iASJl3lqDi7wjEulpalrhKxbHktbz+W6pj/ewW" +
            "sOZ306U9MdZX2uAE1r+AgqH/OTwzAxvSmdrnjOXB4Rzn21FiLS0t/o7A7ecj//zTJbnI/fLiq6HA/rXfMGhDPxWIdRXt6UE+JLBH" +
            "QMGwR22+c2IDijZf5jImUE4g1tDSyvXaZ08eVnPMm3nIMQ+lo4h9pnSf+htUYG1aCoa1UvMeZ+MZd+49LNSf21g/S6s/Wh8juO76" +
            "mCdR9i0Q+07fGYg+lYCCIdV0pAvGhpNiSqoE4aGtCuv3TmPtRPv+hm++CLjuvjCk+sWcpJqOosHEHhStaKc6m1NAwTDnvL/KOjaY" +
            "aK+O83mfAh4Q6sxb/GXeWDfR6oyg1/4FcmZgT8g5L6WisieVkpy4HwXDxJP/IHUbywOYQd72YFB+IuNfXY11E3+Zt3zvY/QY1120" +
            "MbIZMwvzM+a8LlnFHrV877WQwEzdKBhmmu3XudpQXhs5gsAiEOslWvyrq8t7Xn8U8CD6o0nWd/7KGpi4igjEfhW/wVGkM53MJaBg" +
            "mGu+n2UbG8mzzzv9TNhXAh7crjAOfBtrJdqBLqY51TXX11T/3le4ot0hEL/BYf/aATf7KQqG2a+At7fYOKKRGFvAg9vx+Y11Eu14" +
            "T3P0UP6am8OtdZbmrfUMnDO+vewc52FGUTAMM5W7ErFh7GLr7iQPAMenzFrZZuia2+aV7Wjzl21G6sTTdF+rk5JeawkoGGrJ5u43" +
            "NolouaMUHYH2ArFOorWPpJ8IPGz2M1ciJRD7WzQSBJ4KKBie8gz54YaNYcj8Z0vKw9u+GY91Em3f2fOe5XobZ+7N5ThzuSYT+90a" +
            "pYmPUTDMNfk2hLnm2w1/+3zHGom2/czsZ9SPz/VW3/jsEczp2eJtx7P3tfVPPbqCIfX0FA3ORlCUU2cDClgj+yfVg+V+O2cS2CxQ" +
            "8QT7YEXcnrtWMPQ8e+ti//NymA3ggjDZlwe49RMe6yPa+jMcSWAeAXvJPHO9ZBr7YbTlZ68E3hQM1S6CFB3Hgv8tRSSCOFPADX69" +
            "dqyR9Uc78p6A6+2eyljvmeOx5nNtNvbHtVITHKdgGHeSLfRx51ZmZQTWr5Ey443YiwfJEWdVTgTeBeyT7xZTf6dgGHP6LfAx53VN" +
            "Vh7gXiv9fTnEGrkgHPxyrR0E7Oz0Yea7M/cM4dovM8xC4xgUDI0noMLwFnYF1E66dEN/PVGxPn55fZgjCBC4I2CPuYMyyVuxd0ab" +
            "JF1p3gokLRhuw/TzCoFYyNFWHOoQAlMKWB/lpt2DYzlLPRHoScA+2tNsFYxVwVAQs2FXFnBD/CRD532AywFkjeSYB1H0L2Cv6X8O" +
            "j2ZgPz0q2OH5CoYOJ+0mZAv3BsSPBK4EYn1Eu3rLtwcFPDAeBOz59G+xuwa+QUz8Yl+dbPIVDH1PuAXb9/yVit7N+76k9XHf5ci7" +
            "rrUjes4lMJaAPbbv+dwUvYJhE1eag2ORRksTkECaCXiAu09vfdx38S6BUgL2nlKSffcTe220vrMQ/UsBBcNLonQHWJjppqRiQLre" +
            "I2CN7FF7fY4HxNdGjiAwq4B9d/CZVzD0NcEWZF/zVTvaz7UH6LB/a6TDSZsl5AHzVEQOOKkHUrL/HsDLfqqCIfsMvcdnIb5b+O6r" +
            "wB9fX/z6TcAa+QbhhQABAo0EZtmHG/G2G1bB0M5+y8gW4BatOY71O3sf59ka+ehR+ifXW2nRcfpzbYwzl6Uyif3431Kd6SeHgIIh" +
            "xzw8iyIW3rPPffZIwPuzCFgjs8y0PAkQ6EXgp0ugf1+ar0EEFAx5JzIegqLljVBkrQT8jt67vDXyblHruxTXW63k9FtEwDVShHG4" +
            "Tn65ZGSPviCM8KVgyDmLFljOeRFVLgHrJNd8iGZuAUXD3PP/LPvbvfrZsT5LKqBgyDcxFla+OckUkZvy19mwTr46+JUAAQI9CNiz" +
            "e5ilJzEqGJ7gNPgoz4JqkLwhCawU8P+LXQlV4LB/CvShi3kE/Kee55nrPZl6xtmjluQcBUOSibiEYSFdEHw9FfCnC29vsU7i/xf7" +
            "FMqHHwUO/PTrgXOdOp+A/9TzfHO+NePYw7ee4/gEAgqG9pMQiyda+0hEQIAAAQIEjgn4jY1jfq/OHuHzeOaJNkIu0+SgYGg71RZM" +
            "W/+eRncT/vqnCz3NmVgJECBA4LGAZ6DHNuk+UTDUmJJ1fVoo65wcRSAErJdQ0AgQIDCWgL29k/lUMLSZKAukjXuvo87+pwtN10uv" +
            "F02BuGe/7goQTtuFa2faqd+VuD1+F9u5JykYzvWO0SyMUNAIrBOwXtY5OYoAgdcCjsgrYK/POzdfIlMwfGE47RcL4jTqYQbyO3XD" +
            "TKVECEwlYO+aarqLJOsZqQhjnU7yFQx18mzdayyCaK3jMD6BngSsmZ5mS6wECBA4LmDfP25YpQcFQxVWnRIgEAIHmpvGATynEkgi" +
            "4B/+SzIRnYUR+3+0zsIeO1wFQ935/fPSvYv+guBrl8Csf6Rvzey6XJxEoKrAns79w3971JyzCLgXLBIJXhUM9SYhLvTf6nWvZwIE" +
            "CBAgQIDA0ALxLDV0gm2S2z6qgmG72ZozXOBrlBzzTMCfLjzT8RkBAr0IzLqX9TI/PcTpmSrBLCkYyk+CC7u86ZQ9Tpi0tTPhpEuZ" +
            "AAECKwTcH1Yg1TxEwVBW1wVd1lNvBAi0EfirzbDDjjp7Yv/NDiD/IgKesYow7utEwbDP7d5ZLuR7Kt7bIzDjH+FbP3uulHrn+PtX" +
            "9Wxn7PnnGZOWcxWBBPeKKnml71TBUGaKXMBlHPVCgAABAgQIEHgm4JnrmU6lzxQMx2FduMcNi/fQcYf+dKHjyRM6AQIPBWbc2x5i" +
            "+OCwgGevw4TbOlAwbPO6PdoFeyviZwLbBKyhbV4zHi1nAgQI3BOI+8enex94r7yAgmG/aVyo+892JgECBAgQIEBgKoHiyX4u3qMO" +
            "7wooGO6yvHxTsfCSyAE7BGb7I3vraMdF4hQCHQvMtsd1PFVdhR73kmhdBd1bsAqG7TM2/EW5ncQZBAgQIECAAIGmAv7vSRX5FQzr" +
            "caNQiLb+DEcSIPBIwFp6JFP2fb0RIEBgFoH4vye5t1SabQXDOlgX4DonR+0X8Ef1++2cSYBAPwL2ut1z5cSVAp7ZVkJtOUzB8FrL" +
            "hffayBEEtghYU1u02h1rntrZG5kAgWMC9q9jfj+crWD4geTDG5svuA9n+4EAAQIECBAgQKCFgGe4guoKhseYLrTHNj4hsFegp3W1" +
            "N0fnESDwXMD/Lem5j0/LCbjnFLJUMNyHdIHdd/FuHQE3zzqueiVAgMA3AS8TC3imKzD5CoYfEV1YP5p4h0AJAWurhOK5fZizc72N" +
            "RoBAHQF72UHXVAXDwVxKnO6CKqGoDwIECBAgQIBALgHPeAfmQ8HwjudCerfw3XkCo/7fkW4Fra9bET8TmFtglr1v7lnOl7170c45" +
            "UTB8hXMBfXXwKwECBG4F/r19w8+zCciXwFACnvl2TKeC4e3NhbPjwnEKAQLTCPw0TaYSJUBgFoF5n/12zvDsBYMLZueF4zQCGwSs" +
            "sw1YDiUwkYD/W9JEk50wVfemDZMyc8HgQtlwoTi0isCjm2WVwXRK4ICA/fIAnlMJEEgrYG9bOTWzFgwukJUXiMMIECBA4IiAcwkQ" +
            "SC7gmXDFBM1YMLgwVlwYDiFQSMB6KwSZoBtzmWAShECAQBWBdftblaH76HS2gsEF0cd1KUoCBAgQmEPA/zVzjnnuIUvPiE9maaaC" +
            "wYXw5EIY6KNeUpnhJuk/x9nL1bg+TvvoeitHEiDQn4A97sGczVIwuAAeXADeJlBRwH+OsyLuHF3LkgABAqcLeGa8Qz5DwWDi70y8" +
            "twgQILBTwJ66E85pBKYW6Ct5+9zNfI1eMJjwmwn3I4GTBD6dNI5hCBAgQIBADQHPkFeqIxcMJvpqold+6zACpQQ+l+pIPwI5qnQA" +
            "ABAASURBVCkF7K8pp0VQBAgUFrDXfQMdtWAwwd8m2EtKgRn+wnNK+LmCqp7tn9VHMEDcy6KNLmFPHH2G+85vhjX4coZGLBhM7Mtp" +
            "dwABAgQOC/x2uAcdPBNwL3umM9tn8m0tMP16HK1gmH5CW68o4xO4CFiHF4RJvsx1+YkO02jle9YjAQJHBKZelyMVDK0n8shF6FwC" +
            "BAj0KmDvLTdzjywfvV9uZD0RILBGYNq1OErBMO0Errm6HUOAwFYBx28UsAdvBHM4AQLdCky5341QMEw5cd0uM4ETIECAwD2B2e9l" +
            "9f7i8z1t7xE4JjDdeu29YJhuwo5d385OIDD6TdGaTHCRNQrB3O+HZ7ffzpkEWglMtW6zFAx7JnuqidoD5BwCBAicLGBf3g6+1mzt" +
            "cdsjcAYBAnsFplmXvRYM00zQ3ivYeQTaCRh5cgH78/oLgNV6K0cSyCowxTrusWCYYmKyrgpxESBAYIWAffo1Uh9Gr/NwBAECb2/D" +
            "r+feCobhJ8SqI0CAwCAC9uvHE8nmsY1PCPQqkH5dH4HtqWAYeiKOTKJzCSQRsEaTTESiMOKa+JQongyhhEmGOMRAgEB5gWHXdy8F" +
            "w7ATUP5a1WPfAqInMJzA50tG9vC3t78LOIzu+M/FyBeB3gWGXKc9FAxDwve+GsRPgACBjQLz7eXvQJH7L+8/+u6BwK8P3vc2gd4E" +
            "Ys33FvPTeHsoGJ4m4EMCHQmM/m8wdDQVQm0kMNxNdIXjjDmvYHEIgb4EdkQ71NrPXjAMhb3jYnMKAQIERhOIfT3aaHnd5hM5Rrt9" +
            "388ECMwjMMwekLlgGAZ5nnXROlPjEyDQkUDs8dE6Cnl1qKPmtRrAgQQIfBcYYj/IWjAMgfv9UvENAQIECDwSuL/fPzo69/uRS7Tc" +
            "UYqOAIGzBbrfFzIWDN2jnn0VGo9AAgHrNsEkdBxCXD/Rek0hYo92RvxnjXNGLsaYRECa/QtkKxhshP1fUzIgQIDAXoG4B0Tbe/7Z" +
            "50Ws0c4e13gECPQn0PVekalg6Bqyv+v2NmI/EyBAII1A3A+Wliaob4EsccXrt7e8ECBAYJVAt/tGloKhW8BVl4eDCBAgcKbAWGPF" +
            "/WFprTL78zJw6xguIfgiQGAAgdhLuksjQ8HQJVx3My1gAgQI9C8Q94vrVjOj63F+qzmQvgk8E/DZkAKxv3SVWOuCoTuwrmZXsAQI" +
            "EBhbIO4ht21rxp8uJ9z2ET9f3vZFgACBagJd7TMtC4auoJ5fLj4lQIAAgSQCcW/Z0j4niVsYBAjMJxB7VRdZtywYugASJAECkwlI" +
            "lwABAgQInCfQRdHQqmDoAue8a8VIBAgQIECAQGkB/RHoRCD9c3GLgiE9SicXlzD7Evivr3BFS4BAYoGR76P/k9hdaARqCqRe12cX" +
            "DHcwatrrm0Aagf+fJhKBECDQu8BfvScgfgIE7gqkfU4+s2BIi3B3yrxJgMB2AWcQIHCGwO9nDGIMAgQILAJnFQyKhUXc66wC/oRh" +
            "1pmXN4FOBYRNgEATgZTPzGcUDCkTb3IJGHRmAf/w08yzL3cCBAgQILBeoPSz8/qRHxxZu2BIl/ADB28TqC3wT+0B9E+AAAECBAgM" +
            "I5DqGbpmwZAq0WEuH4n0KvDry8AdQIAAAQIECBB4F0jzLF2rYPj3PVffESBwEUiz6C+x+CJAoLKA7ncLfNp9phMJjCmQ4vmhRsEQ" +
            "if005pzJigABAgQIEKgo8Lli37omsEcgwznxbN00jhoFQ9OEDE6AAAECBAgQIECAQDmB0gVD8wqoHI2euhIQLAECBAgQIEBgXIGm" +
            "z9glC4amiYx7fciMAAECcwnIlgABAgTuCjR71i5VMDRL4C6nNwkQIECAAAECBFoLGL+8QJNn7hIFQ5PAy/vrkQABAgQIECBAgEB6" +
            "gdOfvY8WDKcHnH4KewxQzAQIECBAgAABAj0JnPoMfrRg6AlWrAQI1BP4r17Xet4i4FgCBAgQIFBa4EjBcGplUzpx/REgUFTg56K9" +
            "6YwAAQIECBB4JXDas/jeguG0AF9J+ZwAAQIECBAgQIDApAKnPJPvKRhOCaybSRcoAQIECBA4T+B/zhvKSAQIdCJQ/dl8a8FQPaBO" +
            "JkaYBAgMKCAlAgQIECDQqUDVZ/QtBUPVQDqdHGET2CJgDW3RciwBAgT2CziTwIwC1Z4zthQMM8LLmQABAgQIEDhHoNrDzjnhG4XA" +
            "uAJrCoZPl/TrLOJLx74IECBAgAABAgQIECgiEM/s0Yp0tnSypmD4vBzslQABAo8ELu/7y5gXBF8EKgr8VbFvXRMgQOChwKuCoXiF" +
            "8jASHxAgQIAAAQLPBH5/9mHBz3RFgED/AkWf4Z8VDEUH6t9dBgQIECBAgAABAgS6Efjft7cysT4rGMqMoBcCBAgQIECAAAECBFoI" +
            "xN9FPjzuo4LBny4cptUBgfUCjiRAgAABAgQIVBAo8neR7xUMioUKs6VLAt8ERl9f/uLzt4n2Mq1ArcStrVqy+iUwvsDhZ4/bguFw" +
            "h+Oby5AAAQIECBAoLOD5ozCo7koIDNXHoTV2XTAc6mgoUskQIECAAAECBAgQGEtg97P+UjDs7mAsR9n0KCBmAgQIECBAgACBVQK7" +
            "nvmjYNh14qqQHESAwIwC/r/WM856mZz1cl+gyF9avN+1dwkQmFBg87N/FAwTOkmZAAECBAh0I/BHN5EKlMB3Ad+MJBAFg98NHGlG" +
            "5dKDwObKvoekxEiAAIGdAn/vPM9pBAjsE9j87B8FQwy1+cQ4SetfQAYECBAgQKCxwC+Nxzc8gZkEdj3zLwVDQEUH0eJ7jQABAkcE" +
            "7CVH9Pad66wxBaylMedVVgRaCOzeT64LhiVwf7lqkfBKgAABAgQIEDhdwIAEigvsLhYiknsFQ/zlqkOdRscaAQIECBAgQIAAAQLN" +
            "BQ4/198rGJasDne+dDTqq7wIHBCY4S8+20MOXCBOJXARmGENzbAXXqbSF4EmArGHRDs8+LOCITovMkh0pBEgQCCxgNAIECBAgMBI" +
            "AkWf4V8VDAFXdMDoUCNAYBoB+8c0Uy1RAlkExEFgeoHi9941BUOoFx84OtUIECBAgACBuwLuu3dZvEmAwAuBKnvH2oIhYisaQHSo" +
            "EZhcwP93d/ILQPoEJhf4NHn+0idQWqDas/qWgiGSqhZIdK4RINClwKug7RuvhHxO4KPALGvGf8b947z7icARgar7xtaCIRKpGlAM" +
            "oBEgQIAAAQItBIxJgECHAtWfzfcUDOEYgUWL7zUCBAgQIECAAAECBM4ViGfxaPdHLfju3oJhCeGUIJfBvBIYUGCWv8dgrxjw4pVS" +
            "FYFZ1sose1+Vi0SnBC4Cp+4VRwuGS7xvpwYcA2oEBhKQCgECBAgQIEBgi8Dpz94lCoZI8PTAY1CNAIGuBOwTXU2XYLcLHD7DGjlM" +
            "qAMCwws02SdKFQwxO00SiIE1Ap0L+KP5zidQ+AQIECAwmEDOdJo9a5csGIK2WSIxuEaAQHoBe0T6KRJgI4GZ1obfJGl0kRm2a4Gm" +
            "e0TpgiFmIhKKFt9rBGoK6JsAAQIECBAgMLpA8+fqGgXDMmnNk1sC8UqAQCoBe0Oq6cgSzNRxWBNTT7/kCTwUiL0h2sMDzvqgZsEQ" +
            "OaRIMgLRCCQX8Ef0ySdIeAQIFBH4t0gvOsktILoSAqmeoWsXDAGWKuEISCNAoLmAfaH5FAggicBsa+GnJO7CIJBZIN2+cEbBEBOS" +
            "LvEIavImfQIECBAgQIAAgVwCKZ+ZzyoYYioC4L/4RiNA4K7AbP+3pNgT7kJ4c6uA4zsVmG0NzLbHdXpZCruhQNo94cyCIfx/vvyS" +
            "FuMSmy8CBM4VsB+c6200AgSyC4hvRoG4F0ZLm/vZBcMCkRplCdIrAQIECBCoJOA+WAlWtwQ6E+hiL2hVMMRcdgEUgd5p3iJQS2DG" +
            "P7K3F9S6mvRLII/AjHtbHn2RZBXo5v7XsmCIyQuof+IbjQABAm0EjErgdIG4950+qAEJEEgl0NU+0LpgiJn79fJLV2iXeH0RqCkw" +
            "4+/E2QNqXlH6ziQw47V+3p6WaabFQuC+QOwB0e5/mvTdDAXDQtMd3hK4VwIECBAgQIAAAQIvBLp91m1RMDyz7BbyWVI+I0BglYD1" +
            "v4rJQR0LuMY7njyhEzgo0PX6z1YwxFx0DRoJaAQKCHTyR/gFMv3YhfX/0cNP4wjMem3PupeNc+XKpIRA9+s/Y8EQExOw0eJ7jQCB" +
            "uQT+mitd2RJIIiAMAgRKC8SzbLTS/Z7eX9aCYYEYAnlJxiuBjQKz/s7c7xudHE4gu8Cs97I/s0+M+AhUFGi67kvnlb1giHyHAo+E" +
            "NAIEXgpY9y+JHNCJwMzX8m+dzJEwCZQWGG7d91AwxCQGvH+vISS0gQSk8kIg1v2LQ3xMILWAazj19AiOQBWBIdd9LwVDzKh/ryEU" +
            "tNkEZv2/Jc02z/LtXUD8twL2rlsRP48uEIVCtCHz7KlgWCZg2MlYEvRKgMB3Aev9O4VvOhNw7XY2YcIlsAjseB1+vfdYMMQ8Dj8x" +
            "kaRG4JvA7L9TZ71/uxC8dCMw+zU7+57VzYUq0CICU6z3XguGmOGYoGjxvTadgIQnE7DWJ5vwjtN1rXY8eUInsFFgmvXec8GwzOk0" +
            "k7Uk7HVKAb9j9/Zmrb8N+r9x0nKNvr3Zq8a5nmXyWCDWerTHRwz2yQgFQ0zJVJMWCWsEJhWw1iedeGkTINCHwARRTnkfGqVgiOsz" +
            "JjBafK8RGFHA79yNOKtyGkXA/cefLoxyLcvjvkCs8Wj3Px383ZEKhmWqpp3MBeD1qyMIdC0Qazxa10kIfigB1+NQ0ykZAj8ITL/G" +
            "RywYYpann9hA0IYU8KcMQ07rgaSc2lrA/ebrDNibvjr4dTwBa/wyp6MWDJfUvvwFSZMcEhqBMQWs7zHntaesXIM9zVYHsQoxlUCs" +
            "72ipgmoVzMgFw2JqshcJr6MI+J2895m0vt8tfHeugGvv3due9G7huzEErO+beZyhYIiUC058dKcRIJBIwPpONBmThOKam2SipTml" +
            "gPV9Z9pnKRgi9bgAosX3GoHeBfyO3scZ3Le2P/bhJwJrBFxrH5XsRR89/NSvQKztaP1mUDHymQqGhdHFsEh4JTCWgLU91nxmzMY1" +
            "lnFWvsXkhcABAWv7Bd6MBUOQxIURLb7XCPQq4Hf2fpw56/pHE++UEXBt/ehoD/rRxDv9CVjbK+bs5IJhRUTnHuIiOdfbaOUFPpXv" +
            "svseY11H6z4RCaQRcD2lmQqBECgmEOs6WrEOR+5o9oIh5tbFEgparwKfmwVuYAJzCLhH3J9nf7pw38W7fQhY1xvnScHwFSwunGhf" +
            "f/Irgb4E3Ljvz1es6Wj3P/UugecCce1Ee37UIJ9uTMOesxHM4akErOsd06Fg+IjmIvro4ScCIwhY1yPM4rk5uGbO9TYagTMEYl1H" +
            "O2OslmNUGVvB8CNrXEzRfvzEOwTyCvgdv+dzY00/9/Hpu4Br5d3i3nf2mnsq3ssuYF0fnCEWSAPjAAAQAElEQVQFw2NAF9djG58c" +
            "Fahzvhv5c1dr+rmPT9/eXCNv/kdgKIFY09GGSqpFMgqG5+ousuc+PiXQm0Cs6Wi9xS3e+gK7r4v6oaUZwW9KpJkKgawQsKZXIK09" +
            "RMHwWiouuGivj3QEgfYCbujr5sCaXuc0w1FxLUSbIdcjOdpbjug590yBWM/R9ozpnAcCCoYHMHfedvHdQfEWgY4FrOmOJ69Q6K6B" +
            "QpC6IZBEwJquNBEKhm2wcSFG23aWo8sK6O2VgN8JfCX0/nms52jv7/huBoH/Lkma9wvCyi97ykoohzUVsKYr8isY9uG6KPe5Oes8" +
            "ATf4bdbW9DavYkc36Cjm+ucG4/Y6pL2k15mbJ+5Y09HmybhBpgqG/ehxcUbb34MzCRDIJBDrOVqmmMRSVsD8lvXUG4FrgbO//+sy" +
            "oDV9QTjjS8FwXNnFetxQD3UE/M7gPldrep9b5rNiTqNljjFjbPaQjLMiphCI9fx7fKOdI6BgKOMcF26Znmr3ov/ZBNzw9814rOlo" +
            "+852VhaBmMNoWeLpKQ57R0+zNVes1nSD+VYwlEOPCzhauR71RKCMgBv/fsdY09H291DxTF0/FIg5i/bwAB8QINCdQKzpaN0FPkLA" +
            "Cobys+hiLm+qRwKtBazr1jOwfnxztd7q0ZF+k+GRTL339fxcwLp+7lP9UwVDHeK4sKPV6V2vBLYLeADYbnZ7RqzpaLfv+zmHQMxN" +
            "tBzR9BuFvaLfuRsx8ljT0UbMraucFAxbpmv7sS7y7WbOqCfgQaCMbazraGV608tRgZiLaEf7cf7bmz3CVZBJwLpONBsKhvqTERd8" +
            "tPojGYEAgdUCBQ6MdR2tQFe62CgQ7kvbeKrDCRBILmBtJ5wgBcN5kxIL4LzRjETgvoDfQbzvcuTdWNtLO9KPc18L+BeaXxvtPaLn" +
            "vWFvzs7LJxB7ab6oRPSmYDj3IoiFEO3cUY1G4KOAB4OPHiV/ivUdrWSf+np7C9No/oXmtyr/sydUYdXpBoFY39E2nOLQMwXOKxjO" +
            "zCr/WLEoouWPVISjCnhAqDuzsb6j1R1l/N7DMNr4mbbL0F7Qzt7Ib2+f396+/IbAm//lFlAwtJ0fN8K2/rOPvvtBYXa4DfnHGl/a" +
            "htOmPnTxitepIU5I3h5wArIhHgrEGv/j4ac+SCWgYGg/HbFgorWPRAQECNQUiHW+tJrj9Nj34hKvPcbfc8xiJ3C2QKzzaGePa7wD" +
            "AgqGA3iFT7V4CoPqbpWA32FcxVT8oFjv1634AMk7vM49vk8e7pDhWftDTmv6pKz3qlNUr3MFQz3bPT3HQoq251znENgr4MFhr1y5" +
            "82LdL61cr7l6WvKL11yRzReNNT/fnLfOONZ9tNZxGH+ngIJhJ1zl0yyqysAtu086tgeIPBMT6/+25YluXSS38cfP6850VG0Ba722" +
            "sP5vBaz/W5EOf1Yw5J20WGDR8kYostEEPEjkndHYC27bX0nCvY0rfk4SWvUwehvAGu9txvqON/aCaH1nIfovAgqGLwypf4nFFv/Z" +
            "sdRBCm4YAQ8U/Uzl75dQY3+41y4fffla/qGzOObLGyt+iWOjXR8aPz9q18f5Pq/Av3lDE9lgAvHMEvtFZ2kJ95mAguGZTp7P4j87" +
            "ZvHlmY/RI1E09D/DsV9Eu/6HzuLnNW3J/vrY5T2v/Qr81G/oIu9IIPaNeGbpKGShrhFQMKxRynNMLMRoeSI6ORLDnSagaDiN2kAE" +
            "qgrEWo5WdRCdTy8QzybRpocYFUDB0OfMxqKM1mf0ou5FwENGLzPVZ5yiri9gDdc3NsLbm+eRt/H/p2Doe44t0r7nr4foPXD0MEti" +
            "JPCjgLX7o4l3ygrEM0i0t7e3sh3rLZ+AgiHfnGyNKBZrtK3nOZ7AWgEPHmulHEcgh8CnHGGIYlCBeOaINmh60ronoGC4p9Lney8X" +
            "b59piTqJgKIhyUQIg8ALgVir8V+peXGYjwnsEvCssYut/5MUDP3P4XUGsZCjXb/newKlBOJBpFRf+jkm4GwCBAicKRDPFtHOHNNY" +
            "iQQUDIkmo2AosaijFexSVwS+CCgavjD4hUBKAesz5bS8Cir158u/5ZI6SMHVF1Aw1DduOYKioaX+uGN7KBl3bmXWp0D8o2zWZZ9z" +
            "lznqeIa4/rdcMscqtsoCCoaVwB0fFgs+WscpCD2hgIeThJMipCkF/rxk7R9luyD4KiYQzwzRinWoo/4FFAz9z+HaDGLxR1t7vOMI" +
            "vBLotWh4lZfPCfQiEGvwt16CFWd6gXhGiJY+UAGeL6BgON+89YixGURrHYfxxxCIB5YxMpEFgb4ErL0v8+WXQgKeCwpBjtqNgmHU" +
            "mX2dl83htZEj1gl4cFnn5CgCpQT+LtWRfqYXiGeBaNNDAHgucErB8DwEnzYUiE0iWsMQDD2IgKJhkImURnqBWGu/pI9SgNkF4t4f" +
            "LXuc4ksioGBIMhGNw4hNI1rjMAzfgcCzEONB5tnnPiNA4JiANXbMz9lvb3Gvj/bmfwS2CCgYtmiNf2xsItHGz1SGtQQ80NSS1e/s" +
            "AhXW1uyk0+Xv/j7dlJdLWMFQznKknmwqI83m+bnEg02080c2IoExBaynMef1rKzinh7trPGM00Kg8pgKhsrAHXcfm0u0jlMQemMB" +
            "fzGz8QQYfggBxcIQ09gkibiHR2syuEHHElAwjDWfNbKJzSZajb5n63O2fOMvZnrYmW3W5VtKINZOtFL96WcegbhnR5snY5lWF1Aw" +
            "VCceZoDYfKINk5BEThPw0HMatYHOE6g6kjVTlXfYzuMeHW3YBCXWTkDB0M6+15FjM4rWa/zibiPgAaiNu1H7E7BW+puz1hHHPTla" +
            "6zj6HV/kLwUUDC+JHPBAIDanaA8+9jaBHwTiQSjaDx94gwCBLwLWxxcGv2wQcB/egOXQ/QIKhv12zvwqEJvVf1+/rfqrzscR8FA0" +
            "zlzKpIxArIloZXrTy+gCcc+Ne2+00XOVXxIBBUOSieg8jJ8v8du4Lgi+Vgt4OFpNNeKBcroSsBauMHz7UiDutXHPfXmgAwiUFFAw" +
            "lNTUV2xk0UgQWCMQD0rR1hzrGAKjCcS1H220vORTRyDurdHq9H6kV+dOIaBgmGKaT08yNrVopw9swC4FPDR1OW2CPiDgmj+AN9mp" +
            "cS+NNlna0s0moGDINiN14mnVa2xy0VqNb9x+BOIByj/01s98iXS/QFzr+8925iwCce+MNku+8kwuoGBIPkGDhBebXrRB0pFGJQH/" +
            "0NsqWAd1KhCFQrROwxf2SQJxr4x20nCGIbBOQMGwzslRZQRiE4xWpje9jCoQD1V/jpqcvKYT+HTJOK7py4svAjcC7z/GvTHa+zu+" +
            "I5BIQMGQaDImCiU2xWgTpSzVjQK/XY73kHVB8NW1QFzDn7vOQPC1BeJeGK32OPoncEhAwfCazxH1BGKTjP+edL0R9Ny7QDxwRes9" +
            "D/HPJRDXbLS5spbtVoG4B249x/EEmggoGJqwG/RKIP570rFpRrt627cEPggUevj60KcfCNQQcK3WUB2rz7jfRRsrK9kMLaBgGHp6" +
            "u0suNtBo3QUu4FME4kEs2imDGYTARoG4NqNtPM3huwX6OzHub9H6i1zE0wsoGKa/BFICxIb6T8rIBJVBIB7KomWIRQwE4lqMRoLA" +
            "I4G4p0V79Ln3CaQXqF0wpAcQYFqBXy+RxQYb7fKtLwI/CMRDWrQfPvAGgZMEXH8nQXc4TNy7ltZh+EIm8FFAwfDRw085BWy6KeYl" +
            "bRDx0BYtbYACG04grrdowyUmocMC7leHCXWQUUDBkHFWxPRIwEb8SMb7IRAPcNHie41ADYG4vqLV6PvcPo1WWsD9qbSo/lIJKBhS" +
            "TYdgVgrYmFdCTXpYPNBFmzR9aVcQiOspWoWuddm5gPtR5xM4Qvhn5KBgOEPZGLUEbNS1ZMfoNx7woo2RjSxaCMT1E63F2MbMLeD+" +
            "k3t+RFdYQMFQGFR3TQQ62LibuBj0q0A88P379Vu/ElglENdMtFUHO2gqAfebqaZbsouAgmGR8DqCwLKRx+sI+cihnMBPl67iATDa" +
            "5VtfBH4Q+PvyTlwf0S7fPvny0YwCcV+JNmPucibwpmBwEYwqEBt7tFHzk9d+gXggjLa/B2eOJBDXQrRfRkpKLkUE4h6ytCId6iSf" +
            "gIjWCSgY1jk5ql8Bm32/c1c78nhIjFZ7HP3nFIi5j5YzOlG1FHDfaKlv7JQCCoaU0yKojwJFfnIDKMI4ZCfx0Li0IROU1HeBZZ7j" +
            "9fubviHwTcB94huEFwK3AgqGWxE/jy6w3BDidfRc5bddIB4ko20/0xnrBNocFXMarc3oRs0s8NcluLgfRLt864sAgXsCCoZ7Kt6b" +
            "RcANYpaZ3p5nPFwubfvZzsggsMxfvGaIRwz5BOIe8Hu+sPqJSKTzCCgY5plrmd4XiBvG0u4f4d3ZBeKBc2mzW2TPf5mneM0eq/ja" +
            "CCz7fby2icCoBDoUUDB0OGnbQnb0BoG4gUTbcIpDJxOIB9GlfZos96zpLvMRr1ljFFd7gdjbo7WPRAQEOhRQMHQ4aUKuLhA3laVV" +
            "H8wA3Qp8vkQeD6lLu/xY+Uv3i8BiHq/Le14J3BOwl99T8R6BjQIKho1gDp9OwM1muinfnXA8vC5tdydOvCuwuC6vdw/yJoFvAsu+" +
            "Ha/f3sr3IiICPQkoGHqaLbG2FIgbz9JaxmHsPgSWB9vltY+o80S5uC2veSITSWYBe3Tm2RFb1wIKhqfT50MCdwXclO6yePOJwPLg" +
            "u7w+OXTKjxaX5XVKBEnvElj243jd1YGTCBB4LaBgeG3kCAKPBOIGFe3R597PJJArluXB+PY1V5Tlo7nNd/m5/Eh6HFngv0tysfdG" +
            "u3zriwCB2gIKhtrC+p9BIG5aS5shXznWE1geoG9f641Yp+fb+Jef64ym11kEln3251kSlieBLAIKhiwzIY5RBJYbWryOkpM82gss" +
            "D9yvXmtH+mr85fPaceh/LoHYT6PNlbVsCSQSqFgwJMpSKATaCMQNLlqb0Y06o8DywF7rdUZTObcRiL1zaW0iMCoBAt8FFAzfKXxD" +
            "oJrActOL12qDVO1Y5wQIEKgvEHvk0uqPZgQCBFYLKBhWUzmQQBGB5Wb4V5HedEKAAIGNAgkPX/bFhKEJiQCBEFAwhIJG4HyB3y9D" +
            "ukleEHwRIDClwLL/xeuUAJImUEDgtC4UDKdRG4jAQ4G4YS7t4UE+IECAQOcC/1zit9ddEHwR6E1AwdDbjIm3P4FtES8303jddqaj" +
            "CRAgkE8g9rKl/ZovPBERILBGQMGwRskxBNoILDfZeG0TgVEJEPgg4IdVArFnLW3VCQ4iQCC3gIIh9/yIjsAisNx8Py9veCVAgEAy" +
            "gWWfShaWcAjcFfDmBgEFwwYshxJIIPDHJYblphyvlx99ESBAoJlA7ENLaxaEgQkQqCugYKjrq/ejAs5/JeBG/UrI5wQIlBZY9p14" +
            "Ld23/ggQSCigYEg4KUIisEMgbtxL23G6UwjUFzBC1wLL/hKv9Ko9WAAACLhJREFUXScieAIEtgsoGLabOYNAdoG4oV+37PGKjwCB" +
            "fALXe0h8ny9CEbUUMPZkAgqGySZculMKxM1+aVMCSJoAgVUC8R9VsFesonIQgbkEFAwjz7fcCPwosDwMxOuPn3qHAIHZBGIvWFr8" +
            "RxVmy1++BAisEFAwrEByCIFBBZaHhOV10DTHSEsWBAoJXP8pQqz9Qt3qhgCBkQUUDCPPrtwIbBOIh4elbTvT0QQIZBZY1nW8+lOE" +
            "9jMlAgLdCSgYupsyARM4RSAeLK7bKYMahACBIgLXaze+L9KpTggQmFdAwfBo7r1PgMC1QDx0LO36fd8TIJBDYFmf8ZojIlEQIDCM" +
            "gIJhmKmUCIHTBOKB5LqdNvDegZxHYECB6zUY3w+YopQIEMgioGDIMhPiINCvQDysXLd+MxE5gbwC12ssvs8bad3I9E6AQAMBBUMD" +
            "dEMSGFwgHmau2+DpSo9AFYHrNRTfVxlEpwQIEFgjUKdgWDOyYwgQmEUgHnZu2yy5y5PAGoHb9RE/rznPMQQIEDhFQMFwCrNBCPQr" +
            "UCnyeCC6bpWG0S2BlALX1358nzJIQREgQGARUDAsEl4JEGgpEA9N161lLMYmUFrg+tqO70v3v7Y/xxEgQGCXgIJhF5uTCBCoLBAP" +
            "Vbet8pC6J1BE4Pa6jZ+LdKwTAgQIvAuc+52C4VxvoxEgsF8gHrxu2/7enEnguMDt9Rg/H+9VDwQIEEgmoGBINiHCGUtANtUF4gHt" +
            "Xqs+sAGmEvh8ydZ1dkHwRYDAnAIKhjnnXdYERhe493D31+hJy++wQFwj966dP97e3g53rgMCBAj0KqBg6HXmxE2AwFaB3y8n3HsY" +
            "jPcuH/maSCDm/F6La2QiBqkSmFVA3lsFFAxbxRxPgMCIAvceHpf3Rsx3hpwe/WlBzOsM+cuRAAECxQQUDMUodVRaQH8EkgjEA+aj" +
            "FiHGg2m8aucJ/PdtqEfzEu/704JvSF4IECBwVEDBcFTQ+QQIzCywPJjG66u2POTO6LUl51eO8fnPlw7j9fLiiwABAgRqCygYagvr" +
            "nwABAl8FlofceNBd276e2e+va/O8Pq7fbEVOYAoBSc4ooGCYcdblTIBALwLXD9I9ft+LszgJECBA4ImAguEJTs8fiZ0AAQIECBAg" +
            "QIBACQEFQwlFfRAgQKCegJ4JECBAgEBTAQVDU36DEyBAgAABAvMIyJRAnwIKhj7nTdQECBAgQIAAAQIEThFQMNxh9hYBAgQIECBA" +
            "gAABAl8FFAxfHfxKgMCYArIiQIAAAQIEDgooGA4COp0AAQIECBA4Q8AYBAi0ElAwtJI3LgECBAgQIECAAIEOBIoXDB3kLEQCBAgQ" +
            "IECAAAECBFYKKBhWQjmMwIQCUiZAgAABAgQIvCkYXAQECBAgQGB4AQkSIEBgv4CCYb+dMwkQIECAAAECBAicK9BgNAVDA3RDEiBA" +
            "gAABAgQIEOhFQMHQy0yJszcB8RIgQIAAAQIEhhBQMAwxjZIgQIAAgXoCeiZAgMDcAgqGuedf9gQIECBAgACBeQRkuktAwbCLzUkE" +
            "CBAgQIAAAQIE5hBQMMwxz71lKV4CBAgQIECAAIEkAgqGJBMhDAIECIwpICsCBAgQ6F1AwdD7DIqfAAECBAgQIHCGgDGmFVAwTDv1" +
            "EidAgAABAgQIECDwWkDB8NqotyPES4AAAQIECBAgQKCYgIKhGKWOCBAgUFpAfwQIECBAoL2AgqH9HIiAAAECBAgQGF1AfgQ6FlAw" +
            "dDx5QidAgAABAgQIECBQW0DB8FHYTwQIECBAgAABAgQIXAkoGK4wfEuAwEgCciFAgAABAgRKCCgYSijqgwABAgQIEKgnoGcCBJoK" +
            "KBia8hucAAECBAgQIECAQG6BkgVD7kxFR4AAAQIECBAgQIDAZgEFw2YyJxCYQUCOBAgQIECAAIGvAgqGrw5+JUCAAAECYwrIigAB" +
            "AgcFFAwHAZ1OgAABAgQIECBA4AyBVmMoGFrJG5cAAQIECBAgQIBABwIKhg4mSYi9CYiXAAECBAgQIDCOgIJhnLmUCQECBAiUFtAf" +
            "AQIECLwpGFwEBAgQIECAAAECwwtIcL+AgmG/nTMJECBAgAABAgQIDC+gYBh+intLULwECBAgQIAAAQKZBBQMmWZDLAQIEBhJQC4E" +
            "CBAgMISAgmGIaZQEAQIECBAgQKCegJ7nFlAwzD3/sidAgAABAgQIECDwVEDB8JSntw/FS4AAAQIECBAgQKCsgIKhrKfeCBAgUEZA" +
            "LwQIECBAIImAgiHJRAiDAAECBAgQGFNAVgR6F1Aw9D6D4idAgAABAgQIECBQUUDB8B3XNwQIECBAgAABAgQI3AooGG5F/EyAQP8C" +
            "MiBAgAABAgSKCSgYilHqiAABAgQIECgtoD8CBNoLKBjaz4EICBAgQIAAAQIECKQVKFQwpM1PYAQIECBAgAABAgQIHBBQMBzAcyqB" +
            "IQUkRYAAAQIECBC4ElAwXGH4lgABAgQIjCQgFwIECJQQUDCUUNQHAQIECBAgQIAAgXoCTXtWMDTlNzgBAgQIECBAgACB3AIKhtzz" +
            "I7reBMRLgAABAgQIEBhMQMEw2IRKhwABAgTKCOiFAAECBL4KKBi+OviVAAECBAgQIEBgTAFZHRRQMBwEdDoBAgQIECBAgACBkQUU" +
            "DCPPbm+5iZcAAQIECBAgQCCdgIIh3ZQIiAABAv0LyIAAAQIExhFQMIwzlzIhQIAAAQIECJQW0B+BNwWDi4AAAQIECBAgQIAAgYcC" +
            "CoaHNJ19IFwCBAgQIECAAAECFQQUDBVQdUmAAIEjAs4lQIAAAQKZBBQMmWZDLAQIECBAgMBIAnIhMISAgmGIaZQEAQIECBAgQIAA" +
            "gToCCoZw1QgQIECAAAECBAgQuCugYLjL4k0CBHoVEDcBAgQIECBQVkDBUNZTbwQIECBAgEAZAb0QIJBEQMGQZCKEQYAAAQIECBAg" +
            "QCCjwPGCIWNWYiJAgAABAgQIECBAoIjA/wEAAP//t5hUuwAAAAZJREFUAwBO13yKy4g7EQAAAABJRU5ErkJggg==";

        private const string HideImageBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAwkAAAIfCAYAAAA7Y5C+AAAQAElEQVR4AezdiZnsRNIo0J6xEDwAD8CCAQvAA/AATHse/K8CELe6" +
            "b1WXltwi8/B1UpuUGXFCUim4C/998w+BfgL/d1s6xu3BDwECBAgQIECAwCgCmoRRKrFeHPfNwf3z9SSqZGxSAgQIECBAgMB5AU3C" +
            "eTt7lhXQKJT1NBsBAjMKyIkAAQKNBDQJjaAt869ANAMx/n3j7smz9+828ZQAAQIECBAgMJfAiNloEkasyrwx/bEjNY3CDiSbECBA" +
            "gAABAgRqCmgSauqa+6PANx/fePI6WaPwJAtvEyBAgAABAgSSCmgSkhYuYdhHb/yPbp+QRMgECAwtIDgCBAgsLKBJWLj4jVKP32J0" +
            "9ob/7H6NUrMMAQIECBAgkE1AvPsENAn7nGx1XmDvbzF6toJG4ZmM9wkQIECAAAEClQQ0CZVgTfv2282g1A3+3Ty3Wf0QIECAAAEC" +
            "BAhUFdAkVOVddvL/3TL/7jZK/mgUSmqai8BoAuIhQIAAgaEENAlDlWOKYOJm/qdKmcTclaY2LQECBAgQIFBawHx5BTQJeWs3YuS/" +
            "NAhKo9AA2RIECBAgQIDA2gKahLXr/yL7Qx/HzfsPh/Y4v3GsdX5vexIgQIAAAQIECHwqoEn4lMeHAwtoFAYujtAGFxAeAQIECBB4" +
            "IaBJeAHk45cC8YeUe92w91r3JYoNCBAgQIBAawHrESgpoEkoqbneXNEg1PpDyns1NQp7pWxHgAABAgQIENgpoEnYCVV/s3QrxM15" +
            "7wZhQ4tYtuceCRAgQIAAAQIELgpoEi4C2n0YAY3CMKUQyDsBLwgQIECAQEIBTULConUOOW7GY3QO4+Hyo8b1MFhvEiBAgEBeAZET" +
            "mF1AkzB7hcvmF38GoeyM5WfTKJQ3NSMBAgQIECCwmMCiTcJiVS6Tbtx8j/JnEF5lFLG+2sbnBAgQIECAAAECTwQ0CU9gvJ1eQKOQ" +
            "voQnErALAQIECBAgUERAk1CEcepJ4rcYZb3hzhr31AeU5AgQIHBUwPYECLQX0CS0N8+0YtxkZ/ktRs9cI4dnn3mfAAECBAgQIEDg" +
            "gUCDJuHBqt7KIDDTzfVMuWQ4dsRIgAABAgQIJBfQJCQvoPB3C2gUdlPt3NBmBAgQIECAwLQCmoRpS3s6sbiZjnF6goF3nDWvgcmF" +
            "RoBANgHxEiBAIAQ0CaFgrCSgUVip2nIlQIAAAQIEQuDw0CQcJpt6h1VuoFfJc+qDVXIECBAgQIBAPQFNQj3bbDOvduOcK99sR5N4" +
            "CRAgQIAAgdQCmoTU5SsSfOb/D8JVAI3CVUH7EyBwScDOBAgQGFVAkzBqZdrE9cttmez/H4RbCpd+NAqX+OxMgAABAgQIfBCY4qUm" +
            "YYoynk7ih9N7zrWjRmGuesqGAAECBAgQuCigSbgImHT3325xuzG+Idz9fPG4e9NTAgQIECBAgMCKApqE9aoeN8PfrZf2rozDZteG" +
            "NiJAIJ+AiAkQIEBgv4AmYb/VDFu6CX5dRUavjWxBgAABAgRGERBHJQFNQiXYAad187u/KKz2W9mSAAECBAgQmFBAkzBhUVOlNG6w" +
            "GoVxayMyAgQIECBAoLKAJqEysOlTC2gUUpdP8D0FrE2AAAECuQU0CbnrdyT6/xzZ2Lb/CmgU/qXwhAABAgQWF5D+QgKahIWKfUs1" +
            "GoU/b49+jgloFI552ZoAAQIECBBILqBJSF7AQ+H/vfG3t4doFm4Pfg4IaBQOYNmUAAECBAgQyC2gSchdP9G3FdAotPW22k4BmxEg" +
            "QIAAgdICmoTSonnm86sJ52qlUTjnZi8CBAgQOCZgawJdBTQJXfm7L65ROFcCjcI5N3sRIECAAAECSQQ0CbUKlWdejcK5WmkUzrnZ" +
            "iwABAgQIEEggoElIUKQGIWoUziFrFM65pd5L8AQIECBAYAUBTcIKVd6Xo0Zhn9PHrTQKH0W8JkCAQD4BERMg8EFAk/ABZPGX0Sj8" +
            "vrjBmfQ1CmfU7EOAAAECBAgMKzBHkzAsb8rAvr9FHc3C7cHPAQGNwgEsmxIgQIAAAQJjC2gSxq6P6HIJaBQK18t0BAgQIECAQB8B" +
            "TUIf9wyr+tWEc1XSKJxzsxcBAusIyJQAgQQCmoQEReoYokbhHL5G4ZybvQgQIECAAIFBBI43CYMELoxmAhqFc9QahXNu9iJAgAAB" +
            "AgQGENAkDFCEBCFoFM4VKVWjcC5FexEgQIAAAQIzCmgSZqxqnZw0CudcNQrn3OxFgEAZAbMQIEDglIAm4RTbsjtFo/DnstmfT1yj" +
            "cN7OngQIECBAgMBXAvXf0CTUN55thW9vCf10G36OCWgUjnnZmgABAgQIEOgooEnoiJ946Z8Tx94z9H8bhZ5BWJsAAQIECBAg8EpA" +
            "k/BKyOfPBOK3Hj37zPvPBTQKz218QiC7gPgJECAwjYAmYZpSdklEo3COXaNwzs1eBAgQIECgg8CaS2oS1qx7yaw1Cuc0NQrn3OxF" +
            "gAABAgQINBDQJDRAXmCJoRuFgf01CgMXR2gECBAgQGBlAU3CytUvm7tG4ZynRuGcm70IECBAgACBigKahIq4C06tUThXdI3COTd7" +
            "ESBAgMB0AhIaRUCTMEol5okjGoXf50mnWSYahWbUFiJAgAABAgReCWgSXgn5/JDAPxt//8+jh2MCGoVjXrYmQIAAAQIEKgloEirB" +
            "mvYtfkUBw3EBjcJxM3vUF7ACAQIECCwmoElYrOCN09UonAPXKJxzsxcBAgQIHBKwMYHnApqE5zY+KSMQjcKfZaZaahaNwlLlliwB" +
            "AgQIEBhLQJMwVj0ORZNo428TxTpSqBqFkaohFgIECBAgsJCAJmGhYndONX5FoXMIKZfXKKQs26Wg7UyAAAECBLoLaBK6l2CpADQK" +
            "58qtUTjnttJecYxkHCvVSK7LCwAgkEtAk5CrXjNEG42C/4/C8UrGDeDxvewxg0DU/tXImuervLbPs+YnbgIECKQV0CTsLJ3Nigr4" +
            "/yic44wbpnN72mtUgajpqzFq7C3jemUUn7eMx1oECBCYXkCTMH2Jh00wfkVh2OAGDszNUNnitJot6vZotFp/hXUe+cZ7K+QuRwIE" +
            "CBQX0CQUJzXhAQGNwgGsu03d+NxhDPQ06vJsDBTmcqGoyXIlj4QNAgSuCmgSrgra/6pANAr+PwrHFePG5/he9iglEP4fR6m5zdNG" +
            "4GP94nWbla1CgACBBAJDNgkJ3IRYVsD/R+Gcp5uac25n9grr+3FmDvuML3Bf4+35+FGLkAABAhUENAkVUE15SiB+ReHUjovvFDcy" +
            "WQgyxPnbLcgw/Thub/tZVODjsRCvF6WQNgECKwloElaq9vi5ahTO1chNyzm3ba/w28Z325seCXwisB0v2+P/Ptl2gY+kSIDAjAKa" +
            "hBmrmjunaBR+zZ1Cl+jjZqXLwskWDaePI1kKwh1Q4KdbTPfH1e2lHwIECOQW+G/u8EU/qcCPk+ZVO624Sam9Rsb5w2UbGeMXcz6B" +
            "7XjbHvNlIGICBJYX8CsJyx8CwwLErygMG9zAgcVNSYS38giD+7GyhdzHELg/HuP5GFGJggABAp8IaBI+wfFRdwGNwrkSrHYTEvne" +
            "j3Nq9iLQTuD+eI3nB1a2KQECBNoIaBLaOFvlvIBG4Zzd7Dcekd82zgnZi8A4AtuxHI/jRCUSAgTaCQy4kiZhwKII6SsBjcJXJLve" +
            "mO2GI/LZxi4AGxFIKLAd4/GYMHwhEyAwi4AmYZZKzp/HyI3CyPqZbzQi9vsxsrPYCNQQuD/+43mNNcxJgACBhwKahIcs3hxUQKNw" +
            "rjCZbi4i1m2cy9ZeBIoJDDfRdm7E43DBCYgAgbkENAlz1XOFbDQK56o88k1FxLaNc9nZi8B6Ats5E4/rZS9jAlcE7LtLQJOwi8lG" +
            "gwloFM4VZKSbiYhlG+eysRcBApvAdi7F4/aeRwIECFwS0CRc4rNzB4FtSY3CJnHssedNRKy9jWNR25oAgb0CzrG9UrYjQOBTAU3C" +
            "pzw+HFxAo3CuQHETcW7P43vFWts4vrc9FhKQamGB7byLx8JTm44AgRUENAkrVHnuHDUK5+pb+8Yh5o9xLjp7ESBQUiDOxW2UnNdc" +
            "BF4L2CKtgCYhbekEfiegUbjDOPA0bhoObP5y05hvGy83tgEBAl0EtnM0HrsEYFECBHIIaBJy1KlXlJnW1Sicq1aJG4WYI8a5COxF" +
            "gEAvgThvt9ErBusSIDCogCZh0MII65SARuEU21vcJBzdM/bZxtF9bd9dQAAEvhJwPn9F4g0CawtoEtau/4zZaxTOVTVuEF7tGdts" +
            "49W2PidAIKeAczxn3f6O2r8JFBTQJBTENNUwAhqFc6WIm4NHe8b7MR595j0CBOYUiHN+G3NmKCsCBD4V0CR8ytP0Q4uVFdAonPOM" +
            "m4Jtz3geY3vtkQCBNQXiOhBjzexlTWBRAU3CooVfJG2NwrlCx81AjHN72+uDgJcEphGI68I2pklKIgQIPBbQJDx28e48AhqFeWop" +
            "EwIExhHQLIxTC5EQqCKgSajCatLBBDQKgxVEOAQITCOwNQvxOE1SEiFA4O1t1SZB7dcT+M96KcuYAAECTQWiUYjRdFGLESBQR0CT" +
            "UMfVrGMKaBTGrEvBqExFgMAAAtEoxPhlgFiEQIDASQFNwkk4u6UV0CikLZ3ALwrEsd9yXAzX7hMI/HDLIZqF28PFH7sTINBcQJPQ" +
            "nNyCAwjEjdIAYQiBQDGBOKZfjWKL7ZzoVTzb5zuns1ligWgUtpE4DaETWEugRZOwlqhsswjEDUqWWMVJYBOI4/bR2D7P+DhbPhlr" +
            "0DJmzUJLbWsRuCCgSbiAZ9f0AnFzkj6JfglYubJAHJ8fR+Ulh5n+Y97xepjgBFJEQLNQhNEkBOoJaBLq2Zo5h4Cbjxx1mj3KOA4/" +
            "jtlzPpofn6NiZ7dvu59moa231QjsFtAk7Kay4cQCcfMxcXpSG1Dgz1tMcdxt4/bSz0GBzW57/PXg/jYfS0CzMFY9RDOhwNGUNAlH" +
            "xWw/q0DcaMyam7zGEIhjbBvfjhHSVFH8eMtm843H20s/CQU0CwmLJuQ5BTQJc9ZVVucEBr6xOJeQvboK/H5bPY6pbdxe+mkosLlv" +
            "jw2XtlQBAc1CAURTELgioEm4omffGQXihiJu7mbMTU5tBOIYivF9m+WsslMgahJj5+aNNrPMKwHNwishnxOoJKBJqARr2tQCbu5S" +
            "l69L8HHzuY0uAVh0t8BWp3jcvZMNuwtoFrqXQABHBGbYVpMwQxXlUEPADUQN1bnmjGNkG3Nltk42W/3icZ2sc2eqWchdP9EnEtAk" +
            "JCqWUFsJ/LuOG4d/KTy5E4jjIsbdW55OIBA1jeG3G+YopmYhR51EmVhAk5C4eEJvIhA3DU0WssjQAnEcbGPoQAX3icC+j+K3G6r1" +
            "PqsRtopm4Y8RAhEDgdkENAmzVVQ+NQTihqHGvOYcXyBqH2P8SEVYQyBqv40a85uzjMA3t2miWYhxe+pnNQH51hHQJNRxNet8AnGj" +
            "MF9WMnokELXexqPPjDaqqQAAEABJREFUvbemgGMiR92jUYiRI1pREhhYQJMwcHHWCC1VlnGTkCpgwR4S+Om2tRrfEPx8KhDHSIxP" +
            "N/JhdwGNQvcSCCC7gCYhewXF31rAzUFr8frrRU1j/Fx/qYVWmD/VOGZizJ9p3gyjUYjxS94URE6gn4AmoZ+9lfMKuDHIW7v7yKOO" +
            "Me7f85zAUYE4hmIc3c/27QR+uC0VzcLtwc9VAfuvI6BJWKfWMi0r4KagrGfL2aJ2MVquaa35BeKYijF/pnkzjEYhRt4MRE6goYAm" +
            "oSF2/6VEUFjADUFh0MrTRb1iVF7G9IsLxDEWY3GGodOPRiHG0EEKjkBvAU1C7wpYP7uAm4HxKxg1ijF+pGcjtN+IAo65EavyPqZo" +
            "FH57/5ZXBAhsApqETcIjgeMC8QUT4/ie9mgl4EatlbR1HgnE8beNR597r7/Ad7cQhr2O32LzQ6CbgCahG72FEwvEF0qMxClMH7ob" +
            "s+lLnC7BOCbTBb1QwHFNj7FQylIl8LmAJuFznwuf2nVCgfgCiTFhatOkFDdiMaZJSCJTCcSxGWOqpCZLJq7xMSZLSzoEjgtoEo6b" +
            "2WM9gfjCiLFe5nkyjhuvGPUjtgKB6wJxrMa4PpMZagm45teSNW8aAU1CmlIJtJOAL4pO8AeWdbN1AMumQwk4dgcqx4NQ4vof48FH" +
            "3iIwv4AmYf4ay/CcQHwxxDi3t71aCMQNVowWa1mDQC2BOIZj1JrfvNcF4rvgj+vTmIFALoFJmoRc6KIdXiC+EIYPcvEA3VQtfgBM" +
            "mL5jeuyifnMLz3fDDcHPOgKahHVqLdPXAvEFEOP1lrZoI/B4FTdTj128m18gju0Y+TOZN4P4jogxb4YyI/CPgCbhHwgPSwvEBT/G" +
            "0ggJko+bpxgJQhUigUsC0x/nl3TG2Dm+M2KMEY0oCFQQ0CRUQDVlKgEX+RzlctOUo06iLCcQx/zv5aYzUyUB3yGVYE3bX+BEk9A/" +
            "aBEQKCAQF/YYBaYyBQECBKoIfH+bNZqF24OfgQXiuyTGwCEKjcBxAU3CcTN75BaIC3mM3FnUiH7sOdVs7PqIrq6ARqGub6nZ4zoV" +
            "o9R85iHQVUCT0JXf4o0FXLwbgxdeTv0Kg5oulUA0CjEOB22H5gKuVc3JLVhDQJNQQ9WcownEBTvGaHGJ57iAOh43s8dcAhqFHPWM" +
            "a1WMHNGKMqNA9Zg1CdWJLdBZwEW6cwEqLK+mFVBNmUpAo5CnXK5XeWol0g8CmoQPIF5OIxAX5hhjJiSqqwJqe1XQ/tkFNAp5KhjX" +
            "qxh5IhYpgZuAJuGG4Gc6ARfj6Ur6MCF1fsjizZ4CjdfWKDQGv7ica9ZFQLu3FdAktPW2Wl2BuADHqLuK2UcSUO+RqiGWHgLRKMTo" +
            "sbY1jwvENSvG8T3t0VNgybU1CUuWfbqk44IbY7rEJLRLQO13MdlocgGNQq4Cx3UrRq6oRbuUgCZhqXJPmezri+yUaUvqg4Dj4AOI" +
            "l0sKaBTyld21K1/NlolYk7BMqadLNC6sMaZLTEKnBRwPp+ly7ijqhwIahYcsQ7/p2jV0edYNTpOwbu0zZ+6Cmrl6dWN3bNT1NXsO" +
            "AY1CjjrdRxnXrhj37636XN6DCGgSBimEMHYJxAU0xq6NbbSsgGNk2dJL/E5Ao3CHkeip61eiYs0eqiZh9gq3zq/eei6c9WxnnNnx" +
            "MmNV5XRUIBqFX4/uZPvuAnH9itE9EAGsLaBJWLv+WbJ3scxSqbHidNwUrIep0gr8eIs8moXbg59kAq5hyQo2W7iahNkqOlc+cYGM" +
            "MVdWsmkp4PhpqW2tkQU0CiNX53lscQ2L8XyLa5/Ym8BTAU3CUxofdBZwUexcgImWdyxNVEypXBLQKFzi67qz61hX/jUX1yRkrvuc" +
            "sceFMMac2cmql4Bjqpe8dUcT0CiMVpH98cR1LMb+PWxJ4IKAJuECnl2LC7j4FSc14Z1AmuPrLmZPCdQQ0CjUUG03p2tZO+ulV9Ik" +
            "LF3+YZKPC16MYQISyLQCjrNpSyuxgwIahYNgBTYvOUVcy2KUnNNcBN4JaBLecXjRQcBFrgP64ks65hY/AKT/r4BG4V+KtE9cz9KW" +
            "bvzANQl7a2S70gJxYYtRel7zEdgj4Njbo2SbFQQ0Cvmr7HqWv4ZDZqBJGLIs0wflgjZ9iVMk+NdxmCJSQRIgQOBzgbiexfh8K58S" +
            "OCCgSTiAZdMiAi5iRRhNUkjA8VgI0jSpBWb81YTUBbkQvGvaBTy7vhfQJLz38KqeQFy4YtRbwcwEzgmselxG3iXHOX17jSKgURil" +
            "EtfjiPP6+ixmWF5gzCZh+bJMB+CCNV1Jp0toxmP0j1uVIq9n4/Zx0Z9W6xQN2mTvBDQK7zhSv9jOx9RJCL6vgCahr/8Kq8eFaoU8" +
            "5bhDYPBNMh+rEfvH8c0g3h/jiteDhCaMBwIahQcoid9yviUuXu/QNQm9KzDv+nFhijFvhjKbUSDLMRtx3o9stbiPPZ5ni3/2eI82" +
            "CrN7ZM8vzrEY2fMQf2MBTUJj8EWWczFapNCTpjnq8RtxbWM2+i2v7XG2/DLmo1HIWLXPY47z6/MtfErgTuC/b3cvPCVQQMBFqACi" +
            "KboLjHIcRxzb6I7SMIDIueFylnoioFF4ApP4bedW4uK1Dt2vJLQWn3e9uPDEmDfDZJkJ97JAj+M51rwfl5MwAQECBD4IxDUm/mKD" +
            "D297SeC9gCbhvYdX5wTignNuT3sRGFug1bEd68QYW6NddP4L9nPr1p+oRWvxNuvFX2zgmtPGOu0qmoS0pRsi8LjAxBgiGEEQqCRQ" +
            "6xiPebdRKfSU07opHa9sajJeTUpFFNegUnOZ55LAeDtrEsarSZaIXFiyVEqcJQRKHu8xV4wScc02h5vRcSuqNuPW5mpkcT2KcXUe" +
            "+08moEmYrKCN0nEx+QDt5RICV4/72D/GElgnknQTegKt8S5q1Bi88XKuT43BR19OkzB6hcaLz0VkvJqIqJ3AmeM/9onRLsp8K416" +
            "85lPUsQErgm4Vl3zm2pvTcJU5ayaTFw4YlRdxOQEEgjsPQ9iuxgJUuoaogahK//hxdXrMFm6HRa4bqWrSZeANQld2NMt6oKRrmQC" +
            "rizw2TkRn8WoHMIU07vhzFlGdctZtyNRxzUsxpF9bDuZgCZhsoJWSGe4i0SFHE1J4IzAx3MjXsc4M9eK+7jRzF31X3OHL/qdAq5p" +
            "O6Fm3EyTMGNVy+QUF4YYZWYzy+gCbtjOVSjOkW2cm2HNvR4db2tK5M36x7yhi/ygQFzjfjm4j80nENAkTFDECinEBaHCtKYcVGC7" +
            "YdseBw1TWJMIOM4mKeQtDbW8ISzy88Mtz5P3Brc9/aQU0CSkLFvVoF0EqvION/nHL/mPr4cLWECpBRxfqcv3MHg1fcgy7ZvuEaYt" +
            "7deJaRK+Nln5na9O/pUxFs7dl/7Cxa+YuuOqIq6pCTQUcK/QELvnUpqEnvrjrB0nfIxxIhJJC4HPbto++6xFbNaoK9B6dsdTa/G2" +
            "66lvW+8RVot7ht9GCEQM9QQ0CfVss8wcJ3qWWMVZTmDPl/qebcpFZCYCBDIL/JQ5+Hlib5rJd7fV3EPcEGb90STMWtl9eTm59zmt" +
            "vJVGYeXql8ndMVTGcfRZfh49QPFVE3AvUY2278SahL7+71Zv/MJJ3Rh8oOWO3rQd3X6gVIXSWcCx07kAjZdX78bgAy3nnmKgYpQK" +
            "RZNQSjLXPE7mXPUqGe3ZL/Gz+5WMPetc4iawkoBrxUrVfp+re4v3HulfaRLSl/BQAnECxzi0k40J/CPgy/8fCA+7BBwvu5hslFdA" +
            "5A8E4h7DH2h+AJPxLU1CxqqdizlO3HN72msWgRI3bSXmmMVTHs8Ffn3+kU8WEHCdWKDIn6ToDzR/gpPpo2WbhExFKhCrBqEAoin+" +
            "FXAD8C+FJ08EfnzyvrcJEFhHwL1H8lprEpIXcEf4TtIdSAtsUvrGvvR8pUpgnv4Cjo3+NRghAsfBCFXoH4N7kP41OB2BJuE03fA7" +
            "xokZY/hABVhdoNaXda15q4NYgACBJgK/l1vFTIkF3IskLZ4mIWnhXoTthHwB5ONiAhqFYpRTTOR4mKKMxZL4vthMJsouEPclMbLn" +
            "sVT8TZqEpUT7J+sk7F+DkSJocdPWYo2RTMVCgMB+AX+Ifb/VClu6R0lUZU1ComLtCNXJtwPJJsUE7ifSKNxreE6AwCbgD7FvEh43" +
            "Afcqm8Tgj5qEwQu0M7z4O4mddDuxFtqs9Y176/UWKmWKVNU/RZn2BFl8G8dGcdL0E7pnSVBCTUKCIu0IMf5O4h2b2YRAdQE3A9WJ" +
            "LUCAAIEpBDQKrct4cD1NwkGwwTaPEyzGYGEJZwCBnjfrPdcegH7JENR8ybIfStoxcohrmY3jHibGMglnSlSTkKla72N1Ur33mP1V" +
            "tvzcEGSrmHgJECDQT8A9TT/7pytrEp7SDP2Bk2no8nQPbpQb9FHi6F4QARB4LrDUJ64JS5X7cLLubQ6T1d1Bk1DXt8bsTqIaquas" +
            "JeCmoJbsOPOq8Ti1EAmB7ALz3ONkr8Qtfk3CDSHRj5MnUbE6hTriDduIMXUqj2UJLC/gerD8IfASwL3OS6I2G2gS2jhfXSVOmBhX" +
            "57H/PgFblRdwY1De1IwECBCYVSDueWLMml+KvDQJ45fJSTJ+jUaJcPQb8dHjG6WO4qgmYOJBBFwLBilEgjDcA3UskiahI/6OpZ0c" +
            "O5BskkrAzUGqcr0MVj1fEtmAAIGLAq/vhS4uYPfHApqExy4jvOukGKEKeWLIdLOWKdY8R4BICeQScB3IVa/e0bon6lABTUIH9B1L" +
            "rnQy7OCwyYQCbhAmLKqUCBAgUFHAvVFF3EdTaxIeqfR9z0nQ1z/j6llvuLPGnfEY6RCzJQm8FHANeElkgw8C7pE+gNR8qUmoqXt8" +
            "bgf/cTN75BZwk5C7fqInQGA1gf75uldqVANNQiPoF8vEAR/jxWY+JjClgEZhyrJKigABAtUE3DNVo/0ysSbhi0WvZy0P9F45Wree" +
            "wCw32LPkUa/SZiYwp4Bzf866tsgq7p/+aLHQqmtoEvpWPg7wvhFYncA4Am4WTtfCjgQIEFhS4Jtb1u6lbgg1fjQJNVT3zemg3udk" +
            "q+cCM95Uz5jT8wrm/uTP3OGLfiAB5/2zYnh/r4B7qr1SB7bTJBzAKripg7kgpqmmE3DDkKOk3+YIU5QECCwi4N6qcKE1CYVB76Z7" +
            "9tRB/EzG+wS+CGgUvlh4RoAAAQL7BNxj7XPatZUmYRdTsY0cvMUol59ohZvoQXNc/tgDQKCGgPO9huqac7rXKlR3TUIhyB3TOGh3" +
            "INmEwAcBNw4fQAZ66Zo2UDGEUkDAFDMJuD4VqKYmoQDiiyniQI3xYjMfE9gtsNqN82r57j4QbEhgMgHn+mQF7ZyOe6+LBZilSbjI" +
            "YHcCBAgQIECAAIHJBDQKFwqqSbiAt2NXB+cOJJsQeC7w1yfOo78Y/IsAAQIETgj4DjmBFrtoEkKh/IgDMkb5mc24uoBfjjKAJFYA" +
            "ABAASURBVF/9CJA/gRkEnufgGvfcxifnBdyTnbDTJJxAe7HLLy8+9zEBAvsFXNj3W9mSAAECBJ4L+D55bvPwkzNNwsOJvPmXQByA" +
            "P/z1zL8IECBAgAABAgRGEoj7tJHiGToWTUK58jjwylma6bFA5V+Gf7xox3edUx3xLU2go8Bq17qO1EsuHd8tMZZM/kjSmoQjWs+3" +
            "dbA9t/EJAQIECPQUsDYBAo8E3Ls9Url7T5Nwh3HyqYPsJJzdCHwi4Lz6BGegj9RpoGIIhQCBwwKpr2GHsz24gybhINiHzR1cH0C8" +
            "rCaw0i+/+8P/1Q4jExNII7DSNS9NUSYN1L3ck8JqEp7A7HjbQbUDySbPBLz/iYA//P8Jjo8IECBAoLiAe7oHpJqEByg73nIw7UCy" +
            "CYETAr+d2McuBMYREAkBAlkF3Nt9qJwm4QPIjpcOoh1INikq8GvR2cae7LuxwxPdAwHXxAco3iJAIKXA0+tZymwuBq1JOAbo4Dnm" +
            "ZesyAj+WmcYsBAgQSCXgzyWkKtc0wbrX+6eUmoR/IHY8OGh2II25iaiSCDjHkhRKmAQIEJhcwPfRrcCahBvCjh8Hyw4kmxAgsKxA" +
            "n2vkstwSJ0CggcDy1zVNwuujbPmD5DWRLSoK/FRx7pGm9teejlQNsRAgQKCjwEBLL30PqEn4/Ehc+uD4nManjQR+brRO72X8tae9" +
            "K3B9fdfL64Zm+FrAn0v42sQ7bQWWvbZpEp4faMseFM9J9nxiGwIECBAgQIDAVAJL3hNqEh4fw0seDI8pvEuguoDzrTpxgQVMQYAA" +
            "gbUFlvuu0iR8fcAvdxB8TeCdQQRW+fMIg3ALo4CA62cBRFMQaClgrUMCS13jNAnvj42liv8+da8GFFjlzyMMSC8kAgQGEvh9oFiE" +
            "QmCZe0VNwpeDPWHRvwTvGYGkAs67pIX7JGw1/QTHR6cEvj+1l50I1BNY4jqnSfj7AFqi2H+n6t8ECAwvkD9A19T8NZQBAQKfC0x/" +
            "ndMkvL1NX+TPj3GfEiBAgAABAi0ErDGdwNT3kKs3CVMXd7pTUUKzCTj/Zqvo+3zU972HVwQIzCkw7bVu5SbhYFHnPLJlNayA/4HQ" +
            "sKURGIGhBOK7LMZQQVUIxjWxAqopiwlMeQ6u2iRMWcxih7qJCKwkINeaAq619XTDNka9FcxMgMARgenOxxWbhOmKeOQIti2BQQSc" +
            "h4MUokEYal0emekOU5sQ6CAw1bm5WpMwVfE6HPyWJECAwBkB194zao/3YfnYxbsERhGY5hwdtEmoUudpilZFx6QECBAgMLrAZ99j" +
            "n302el7iIzCbwBTn4ypNwhTFmu0Mks9TgXn/gN7TlH2wgIDr8LUi/3Jtd3sTINBYIP01b4UmIX2RGh/UliNQW8A5WVt43PnV/lxt" +
            "wu2Hc7u22csqBAg8FIhz9+EHGd6cvUlIXZwMB5AYCRAgcFDAdfkYGK8vXn6V9YuFZ3kE0p7D/317y6N8MNK0RTmYp80JECCQTcD1" +
            "uU7FuNZxNSuBqwIpz81ZfyUhZTGuHoH2J/BOwAsCYwu4Tr+uD6PXRrYgkEUg3fk8Y5OQrghZjm5xEigg4PwsgDjRFI6H58V8avN8" +
            "F58QIDC4QKrzerYmIRX+4Aey8AgQINBCwHX7a2UmX5t4h8AsAs/O7+Hym6lJSIM+3FEgIAIECPQViOt3jL5R9F89DGL0j0QEBAjU" +
            "FEhxns/SJKTArnm0mbuzQLnl/e0d5SzNlE9g5Wt5qdxLzZPv6BExgVwCw5+rMzQJwyPnOmZFS4AAga4CQ13TG0hEvjEaLDXFEv5D" +
            "yhRllEQGgRmahAzOYiRAgACB/QJx0xxj/x45t1whx5yVEfXsAqPkN/Q1IHOTELAxRim0OAgQIECgrMCs1/jIK0ZZLbMRIJBRYNhr" +
            "QeYmIeOBIOYSAubIKjDshTAr6CJxx3ETY4Z0f7slMUsut1T8ECBQSGDI60LGJuGPW0GGxLzF5YcAAQIETgq82C2u+zFebDbsxxH7" +
            "d8NGJzACBHoLxDWidwzv1s/WJPxyi/6b2/BDgAABAmsKxBdpjCzZR6wxssQrTgKlBcy3X2Coa0W2JuGH/c62JECAAIGJBeLLNMaI" +
            "KUZc2+gRX6zdY11rEiBwXWCY8zdTkzAM2vX6J5pBqAQIEBhbIL4b7kevaEeIoVfu1iVAoKxAXE/KznhitixNwhBYJ3ztQoAAgSEF" +
            "Jg4qvi/uR81UW61TMwdzEyAwpkBcX7pGlqFJ6I7UtUIWJ0CAAIErAvEd8nEcne/j/tvro/PYnkBtAfPPJRDXmm4Zjd4kdMXpVhUL" +
            "EyBAgEBNgfhuOTJqxmJuAgQIfCYQ16rPPq/22chNQjeUatqvJvY5AQIECBAgQIAAgQEERm0SNAgDHBxCaC7wn+Yrtltw6XO6HbOV" +
            "CBAgQGBCgS7foaM2CRPWV0oECBAgQIDARAJSIdBSoHmjMFqTEAAxWqJbiwABAgQIECBAgMDoAk3vkUdrEtoVx0oECLQUmPm3UrV0" +
            "tBYBAm9vv775h8C6As0ahZGahGZJr3tcyZzA/AIyJEBgeoH/N32GEiTwuUCTe+ZRmoQmyX7u7VMCBAgQIEBgUIH7sH6+f+E5gUUF" +
            "qt87j9AkVE9y0YNH2gQIECBAYEYB9w0zVlVOZwSqngttmoTnaVdN7vmyPiFAoLHAH43XsxwBAgQIEFhB4JdaSfZsEjQItapqXgKN" +
            "BA4s882BbW1KgAABAgQI7BP4Yd9mx7fq1SRoEI7Xyh7zC8x8XvjbjeY/fmU4j4BMCBDIJRD3DzGKRt2jSfitaAYmI0CAAAECBAgQ" +
            "IEDghcCxj3s0Cd8dC9HWBAgQIECAAAECBAi8ECj6qwmtm4Siwb+A8jGBqQQkQ4AAAQIECBB4IVDsXrtlk1As6Bc4PiZAgAABAlkE" +
            "xEmAAIHSAkXuuVs1CUWCLS1oPgIECBAgQIAAAQLlBbrPePneu0WTcDnI7swCIECAAAECeQT8bWJ5aiVSAjUFLt2D124SLgVXU83c" +
            "BD4T8BkBAgQIECBAYGWB2k3CyrZyJ0CAAIGxBERDgACB1QRO/wf7mk3C6aBWq558CSwi4LdALFJoaRIgQKCtgNVeCJy6J6/VJJwK" +
            "5kWCPiZAgAABAgQIECBA4LjA4XvzGk3C4SCO52mPmQTkQoAAAQIECBAgUF3g0D166Sbh0OLVKSxAgAABAr0ErEughoD7jBqq5lxJ" +
            "YPc5VLpJWAlZrgRqCOw+eWssbk4CBAgQIPC5gE9XESjVJMSNTYxV3ORJgAABAgRGFPAXBIxYFTERGEtg1z17qSZhrNRF81TABwQI" +
            "ECBAgAABAssLvGwUSjQJLxdZvgwACBDYBPxXzk2i7KPZCBAgQIDAUYFP7+GvNgmfTn40UtsTIECAAAECBAhsAh4JVBd4ei9/pUn4" +
            "pXrYFiBAgAABAgQIECBAoLnAlSbhh+bRJltQuAQIECBAoKHA7L+d7+l/8WxobCkCMwo8PLfONgkPJ5tRTU4ECBQXyH4jUxzEhAQI" +
            "ECBAoLPAV/f2Z5qErybpnJTlCcwm4BybraLyIUAggYAQCSwv8O7+42iT8G7n5SkBECBAgAABAgQIEJhH4N97/SNNwr87jeggJgIE" +
            "CBAgsLCA38a3cPGlTqCwwP9ivr1NggYhtAwCBEoJ/L5zIpsRIECAAAECbQV+ui33f3ubhNu2fggQIFBM4PtiM5mIAIGEAodD9h8r" +
            "D5PZgcA1gb1Ngl/GvOZsbwJHBfx/SI6K2Z7AugK+o9etvcwJ1BCIX0n4z94mIQL49yIULwwCBKoK+P+QVOU1OQECBAgQIPBAIBqE" +
            "n+P9I01CbK9RCAWDwJwCrbNyPWktbj0CBAgQIPBcIL6X/2oQYpOjTULsExPEo0GAAAECBAgMLyBAAgQIvBT46v7+TJMQq8REMeK5" +
            "QYAAAQIECPQRWOG72B9a7nNsWXV0gXLxPbyOnG0StrAeTrp96JEAgUsCK3wxuoZcOkTsTIAAAQIELgk8/R6+2iREVE8njw8NAgQe" +
            "CniTAAECBAgQINBT4NN7+BJNQiT36SKxgUGAAAECBOYXaJqh796m3BYjMJXAy+tHqSYh1F4uFhsZBAgQ+CDg2vEBxEsCBAgQGExg" +
            "rnB2fe+WbBKCb9eisaFBgAABAgQInBZY5ft2hT+bdfogsCOBEwK7rx2lm4SIdffisbFBoIFA5iVW+YJ03ch8lIqdAAECBDIIHPqu" +
            "rdEkBNKhIGIHgwABAgQIHBNYdmvfscuWXuIETgnENSPGoZ1rNQkRRATzZzwxCBAgQIAAAQIHBVb5ldSDLAtsLsWSAnE/fmq+mk1C" +
            "BPTt7V+ng7vt64cAgbe3/y2C4FqxSKGlSYAAAQJNBC59r9ZuEjaBS0Fuk3hMISDI8gI/lZ/SjAQIJBXwfZq0cMIm0Fjg8rWiVZMQ" +
            "LpeDjUkMAgSmFnCdGLa8AiNAgACBJAJFvktbNgnhWiTomMggQIAAAQKLCaz0HerPI7Q6uK0zm0Cx60TrJiEKUSz4mMwgsIjASl+Y" +
            "fnvVIge1NAkQIECgmEDcX8coNmGPJiGCL5pETLjokDaBGQV+njEpORG4KOB78yKg3QlMLFDl+tCrSYg6RUIx4rlBgACBe4HFrw33" +
            "FJ4TWE7gl+UyljCB8wLVvi97NgkbR7XktgU8EphE4LdJ8pAGAQLHBFb7nvzhGE+irYVKoKxA1WvDCE1CcFVNMhYwCEwg8N0EORxJ" +
            "wXXhiJZtCRAgQGAlgerfkaM0CVHU6snGIheGXQkQIECAQGuB1b4b/2gNbD0CyQR+vcXb5LowUpNwy/mtSdKxkEEgqcBKf8tRlKjB" +
            "NSGWMQgMKbDi3/T1zZCVEBSBMQTiO/HHVqGM1iRE3gEQI54bBAgQIEBgVQF/09eVytuXwFwCze+NR2wStpI2x9gW9kiAwFACrgVD" +
            "lUMwBKoJrPYrpdUgTTydQJfvwVGbhK26XVC2xT0SGFTAF+mghREWgcICzvXCoKYjkFCg273w6E1C1LIbTixuECBQUuD0XK4Dp+ns" +
            "mFxAo5C8gMIncFIgvvdinNz9+m4ZmoTIMpBixHODAIG3txX/Z0OuAY78VQXGbxSuV2aFHK8rmWEVgSG+77I0CdtBMQTaFoxHAh0F" +
            "/M+GOuJbmkAHATfRHdAtSaCDwDD3utEkdMj/0pLD4F3Kws4ECJwRcP6fUbPPLAKzNgqz5jXLcSePdgJDfcdlbBKiVEMhRkAGgQ4C" +
            "L75YO0TUZknnfxtnq4wpsOp5P2Y1REWgjEB8r8UoM1uhWbI2CZF+YMaI5wYBAgQIEFhD4O1No7BKpeW5gsCw97KZm4TtwBkWdwvQ" +
            "IwECxQWc98VJTZhMYJZGYZY8kh0+wh1E4N132SAx/RvGDE1CJDM0cgRoEKgksPIXrPO+0kFl2jQCK5//aYokUAJPBIb/DpulSQj/" +
            "4bEjSGNWAXl1EnDed4K37DACmRuFzLEPcwAIJJ1AfG/FGD7wmZqEwA70GPHcILCKgC/aVSohPK6wAAAQAElEQVQtz/YCOVZ0DchR" +
            "J1ESSHWPOluTsB1+qYqwBe2RAIFTAs73U2x2mkwgW6OQLd7JDhfpdBBI9101a5MQtU9XjAjaIHBSYPUvXOf7yQPHblMJrH4dmKqY" +
            "kplG4NdbJim/o2ZuEm41eYuiRHHiuTGNgEQIPBSI8/3hB94ksJBAhkYhQ4wLHTJSrSgQ30s/Vpy/6tSzNwmBF8WJIsVzg8DMAr54" +
            "3/76DwNv/kkqIOxSAq4FpSTNQ+CcQNx3xji39yB7rdAkbNTpi7Ul4pEAAQIECLwQ0Ci8APJxO4HFVvpzlnxXahKiZtEoxIjnBoEZ" +
            "BdwYvPnVhDf/EPhbYMTrwYgx/a3l3wSuC8Q95rfXpxljhtWahE09irg99/ipgA8JpBRwjqcsm6ArCLgpr4BqSgIPBKb73lm1SYja" +
            "TlfMSMogcBP47Tb8vPkVhbfP/vHZSgKjNAqjxLFS7eVaXyDuJ2PUX6nxCis3CUEdRY0Rzw0Cswh8N0siBfJwfhdANMUUAr1v0Huv" +
            "P0UR9yRhm6YCU3/HrN4kbEfS1EXekvS4lIAv5C/ldn5/sfBsbQHXhbXrL/tyAvG9EqPcjAPOpEn4UpQBiv0lGM8IECgq4Pwuymmy" +
            "xAI9GoUeayYukdAHF1jm+0ST8P5IjML/9P4trwikFfDF/L50cX6/f6fVK+sQGEvAtWGseogmj8BS3yOahK8PzJ9vby11ENzy9UNg" +
            "FQHn9iqVlucrgVaNQqt1XuVb5XOTLiWw3PeHJuH58b3cwfCcwieJBXxBf1085/bXJt5ZU8D1Yc26y/qYQHxnxDi21wRbL9wk7Kpe" +
            "HBQxdm1sIwIE0gg4r9OUSqCVBWo2CjXnrsxiegJ/CSz9XaFJ+OsYePmvpQ+Slzo2GF1grS/q/dVwXu+3suXcAq4Rc9dXdscF/rzt" +
            "svx3hCbhdhTs/Fn+YNnpZLMxBdwEPK5LnNcxHn/qXQLrCJS+RpSe722dUsi0s0B8J3zbOYYhltckHCtDHDgxju1lawIERhdwXo9e" +
            "IfG1ECh1Y19qnhY5W4PAvYDvgjuNRk3C3YpzPHUQzVHH1bLwxf15xZ3Xn/v4dA0B14k16izL9wJx/Y/x/t3FX2kSzh8AcTDFOD+D" +
            "PQlkF5gvfuf0fDWV0XGBK43ClX2PR2oPAtcFXPefGGoSnsAceNvBdQDLpt0FfIG/LoFz+rWRLSYXuKXnWnFD8DO9gOv9JyXWJHyC" +
            "c+CjOMhiHNjFpgS6Cfjyf00f53OM11vagsC8AkevFUe3n1dOZqMLxPU9xuhxlo7v0HyahENcLzd2wL0ksgGBVALO6VTlEmwFgT92" +
            "zqlB2Alls64CcU2P0TWILItrEspXysFX3tSMIVB2+ELf7xnn9O/7N7clgakEvpkqG8msLBDX8pXzP5y7JuEw2a4d4kCMsWtjGxHo" +
            "JKBR2A///W1T5/QNwU95gQQzxrXil0/ijM8/+dhHBLoLuH6fKIEm4QTagV3ioIxxYBebEiAwsIDzeeDiCK2qwA+32R81A4/eu23q" +
            "h8AQAnHNjtEjmPRrahLalNAB2sbZKscFfMEfN4vzOcbxPe1BIL/A//KnIIMFBOIaHWOBVOulqEmoZ/txZgfrR5GRX68Vm0bhXL3j" +
            "nI5xbm97jSCgfser8NPdLq4ddxieDiPgvC5UCk1CIcid08SBG2Pn5jYjQCCBQJzTMRKEKsR/BKJeMeLl9hjPjX0C0RzE2Le1rQi0" +
            "E3A+F7TWJBTEPDCVg/gAlk2bCPjCv87svL5uWHuGqFGMj+s8eu/jNl4TIDCuQJzDMcaNMGFkmoR+RYuDOUa/CKxM4L2ARuG9x5lX" +
            "cU7HOLOvfeoKvKrLq8/rRmd2AgTOCjh3z8q92E+T8AKowccO7gbIltgtoFHYTfXphnFex/h0Ix9WF4j/v0XUIcaexfZut2cu2xAg" +
            "UFcgztcYdVdZeHZNwhjFj4M8xhjRiIIAgVICcV7HKDVfqnk6Bvvrbe1wj/+/xe3poZ/Y79AONiZAoLmA87QBuSahAfKBJRz0B7Bs" +
            "Wk3AryaUp41zO0bcvJaf3Yz3AuH84/0bJ57HHCd2swuBJQR6JhnnZoyeMSyztiZhvFLHwR9jvMhEtJKARqFOtePmNc7v+G0wdVZY" +
            "c9Yw3UYpgZiv1FzmIUDguoBz8rrhoRk0CYe4mm5c52RomoLFkgtoFOoVMH4bTJzjMeqtMvfMYbeNWpnG/LXmNi8BAvsE4jyMsW9r" +
            "WxUT0CQUo6wyUZwUMapMblICOwT831V3IF3cJM7xbZyeaqEdW1vFegvxSpXAUALOv47l0CR0xD+wdJwkfnvCATCbFhO4/7+rFpvU" +
            "RE8F4lzfxtONFvxgM4nHHun3WrdHrtYcS2DVaOKci7Fq/kPkrUkYogy7gth+e8KujW1EoKCA33ZUEPPAVPEFeT8O7Jp+0z9vGYyW" +
            "e8RzC8sPAQIVBeI8i1FxCVPvFdAk7JU6s12dfeLkiVFndrMSeCygUXjs0vLdOO+30XLdFmvFr5RuucXjty0WPbFGxHZiN7sQILBD" +
            "wPm1A6nlJpqEltpl13IylfU022sBjcI/RgM8xPl/P7L91aoR73388SulA7DuCiHi3rWhjQgQ2CUQ51SMXRvbqJ2AJqGddY2V4qSK" +
            "UWNucxJ4JKBReKTS/73tr1aN68H96B/Z29t9PNvziPct8T+RR+Lwhf5EwNvtBZxL7c13r6hJ2E019IZOsqHLIzgC3QTi2vBsbEHd" +
            "/1af7b1XjzHn/R9qj9fPxqu5sn4e+WaNXdwEegvE+ROjdxzW/0RgnibhkyQX+ShOthiLpCvNjgJ+NaEjfsGl43oR4/63+sTrPSPC" +
            "+Pn2r23b29MlfyL/JROXNIGTAnHOxDi5u91aCmgSWmq3WStOvhhtVrPKqgLNGoVVgeWdRsD1Nk2pBNpZwLnSuQBHl9ckHBXLs72T" +
            "MU+tskYajUKMrPGLm0ApAdfb45L2WEcgzo8Y62Q8SaaahEkK+SSNOCljPPnY2wSKCPxSZBaTEMgt4Fqbu36iryPgvKjj2mTWc01C" +
            "k9AsUlAgTtL7P2RYcGpTEXj74c0/BAiEQFxr49EgsLpAnAsxVndInb8mIXX5DgW//SHDQzvZeC2BC9n6bUcX8Ow6lYAbo6nKKZmD" +
            "AnH8xzi4m81HFNAkjFiVujHFyRuj7ipmX1FAo7Bi1eX8SGC0a+yjGL1HoLSA4760aOf5NAmdC9Bx+TiZY3QMwdITCmgUJiyqlE4J" +
            "uL6eYrNTQoE41mMkDD17yHXj1yTU9c0wuxM7Q5VyxahRyFUv0dYTcH2tZ2vm/gJxfMfoH4kIqghoEqqwpps0TvIY6QLPHPDksWsU" +
            "Ji+w9HYLuLbuprJhEoE4pmMkCVeYZwU0CWfl5twvTvoYc2Ynq9YC0SjEaL2u9Qj0FHi0tuvqIxXvZRRwLGes2smYNQkn4SbfzUVg" +
            "8gI3Tk+j0BjcckMKuK4OWRZB7RSI4zfGzs1n3Gy9nDQJ69V8b8ZxMYixd3vbESBAgMDnAq6pn/v4dDyBOGZjjBeZiKoLaBKqE6df" +
            "IC4OMVInIvjuAvGrCTG6ByIAAp0FXE87F8DyuwR+v23lWL0hrPyjSVi5+sdyj4tFXDSO7WVrAu8FNArvPby6JpB177ieZo1d3PML" +
            "xPH5/fxpyvCVgCbhlZDP7wXiohEXj/v3PCdwVECjcFTM9jMKuJbOWNXcOcUxGWOALIQwgoAmYYQq5IshLiIx8kUu4lEENAqjVEIc" +
            "PQVcR3vqW3sTiOMwxvbaI4G/BDQJfzH410mBuKjEeLe7FwR2CmgUdkLZbGoB19Cpyzt0cj/donP83RD8PBbQJDx28e4xgbjIxDi2" +
            "l60JvL1FoxCDxfgCIqwn4PpZz9bMjwXimPv58UfeJfC3gCbhbwf/LiMQF50YZWYzy0oCGoWVqi3XRwKunY9UvFdaII6zGHfzekrg" +
            "sYAm4bGLd68JuABd81t1b43CqpWX9ybg2rlJeCwtEMdWjNLzmm9iAU1C8uIOHH5cjGIMHKLQBhTQKAxYFCE1FXDdbMo9/WJxPMWY" +
            "PlEJlhfQJJQ3NeN7gbg4xXj/rlcEngtEoxDj+RbzfyLDtQVcM9euf4ns4xiKUWIucywqoElYtPAd0o6LVYwOS1syqYBGIWnhhF1E" +
            "wPWyCONok1SPJ46bGNUXssD8ApqE+Ws8WoZx8YoxWlziGVMgGoUYY0YnKgIECIwj4Lt1nFpMEYkm4UAZbVpUIC5mMYpOarJpBTQK" +
            "05ZWYk8EHPNPYLz9lUB8l8b46gNvELgioEm4omffEgJxYYtRYi5zzC1Q66ZpbjXZZROI4zxGtrjF214gvjtjtF/ZiksIaBKWKHOK" +
            "JONCFyNFsILsJhA3TzG6BWBhApUE/neb17F9Qyj3M+1M8V0ZY9oEJTaGgCZhjDqI4otAXPhifHnHMwJfC7iZ+trEO3kF4nj+KW/4" +
            "Im8kEN+NMRotZ5nVBYZtElYvjPzf4kIYAwWBZwJxYxXj2efeJzC6QBy/MUaPU3x9BeK7MEbfKKy+nIAmYbmSp0vYhTFdyT4NuMaH" +
            "cZMVo8bc5iRQS8AxW0t2nnnj+y/GPBnJJJWAJiFVuZYNNi6SMZYFkPguATddu5hs1FkgjtMYncMovbz5CgrE912MglOaisBxAU3C" +
            "cTN79BOIi2aMfhFYeXSBuPmKMXqc4ltTwLG5Zt33Zh3fbzH2bm87AlUF/moSqq5gcgLlBeIiGqP8zGacRSBuxmLMko88cgvEsRgj" +
            "dxairyUQ32cxas1vXgKnBDQJp9jsNIhAXFRjDBLOcGEI6O0tbsx+A0Ggk0AcfzE6LW/ZwQXi+yvG4GEKb1UBTcKqlZ8r77jIbmOu" +
            "zGRTQuC72yRxo6ZZuEH4aSIQx1uMSouZNrmA76vkBVwlfE3CKpVeJ08X33VqfTTTrVk4up/tCewViMYgxt7tbbeWgO+ntep9PNvB" +
            "9tAkDFYQ4RQTcDEuRjndRHETF2O6xCTUTeCX28qOqRuCn4cCvo8esnhzdAFNwugVEt9VgVYX56tx2r+9QNzUxWi/shVnEYjjJ8YP" +
            "syQkj2IC23dPPBab1EQEWgpoElpqW6unQFyoY/SMwdpjCsRNXowxoxNVZ4GHy8fxEuPhh95cWiC+Z2IsjSD5OQQ0CXPUURb7BeLi" +
            "HWP/HrZcRSBu+mKskq88jwvE8RHj+J72mF0gvldizJ7nPPnJ5KWAJuElkQ0mFYiL+TYmTVFaJwXiJnAbJ6ew20QC27EQjxOlJZVC" +
            "Ar5HCkGaZjwBTcJ4NRHRa4HSW7jIlxadZ764MYwxT0Yy2SsQdY+xd3vbrSPw+y1V3xs3BD9zC2gS5q6v7I4JuOgf81pp67hZ3MZK" +
            "eTfOdYjl1HmIMgwZxPYd8f2Q0QmKQGEBTUJhUNNNIbB9EcTjFAlJoqiAm8iinENM9sctCnW9Ifh5KuD74CnNjg9sklJAk5CybIJu" +
            "KOCLoSF2sqW2m8p4TBa6cP8RiNrF+Oaf1x4IECBA4B8BTcI/RxQ65AAADptJREFUEB6eCvjg7S0ahW28+YfAA4G40YwR/0X6wcfe" +
            "Gkgg6rSNgcISyuACjpnBCyS88gKahPKmZpxbQLMwd32vZhf/RXq7mRi8Ybiaapr9t3psj2kCFygBAgR6CmgSeupbO7OAZiFz9drE" +
            "ft8wxA1qm1WtEgL/u/0rzGPcnvohUExg/GOqWKomWl1Ak7D6ESD/qwJbsxCPV+ey/9wCcXNxP+bOtn1297Y/tV/eigsJbMfaQilL" +
            "dUUBTcJYVRdNboFoFGK4Qcldx1bRbzca22OrdWdZZ3PbHmfJSx55BOLYyxOtSAkcFNAkHASzOYEdAj/ftolmIcbtqR8CuwTihuN+" +
            "7NppoY3ubeL5QqlLdWABx+LAxRHaNQFNwjU/exN4JRCNQoxX2/mcwEeBuPl4ND5uN9vrRznHe7PlKZ8ZBP7OwfH5t4N/TyagSZis" +
            "oNIZViAahW0MG6TAUgjEDcmjkSL4uyAf5RDv3W3iKYE0Ao7dNKUS6F6BlZuEvUa2I1BaYGsWfi09sfmWFoiblD2jJtKe9bdtasZh" +
            "bgI9BOLY7rGuNQlUEdAkVGE1KYFdAj/ettoahttTP2UEzPJCIG5kao0XS/uYwPQCcW5Nn6QE1xDQJKxRZ1mOL7A1C/5mpPFrJUIC" +
            "BHoI5FlTo5CnViL9RECT8AmOjwh0EPA3I3VAtyQBAgQKC2gUCoOarr1AqyahfWZWJJBfYPvVhXjMn40MCBAgsJaARmGtek+XrSZh" +
            "upJKaFKBaBS2MVCKQiFAgACBTwQ0Cp/g+GhsAU3C2PURHYFHAluzEI+PPvceAQIErgnYu6SARqGkprmaCWgSmlFbiEAVgWgUtlFl" +
            "AZMSIECAwGUBjcJlQhOUEDgyhybhiJZtCYwtoFkYuz6iI0BgbQGNwtr1T5e9JiFdyQS8rsDuzLdmIR5372RDAgQIEKguoFGoTmyB" +
            "UgKahFKS5iEwpkA0CtsYM0JREVhdQP6rCWgUVqt40nw1CUkLJ2wCJwS2ZiEeT+xuFwIECBAoJKBRKAQ58jTZY9MkZK+g+AmcE4hG" +
            "4X6cm8VeBAisKnB//YjnqzpcyVujcEXPvtUFNAnViS2QU2C5qONLfhvLJS9hAgR2CWzXiHj8uMOj9z5u4/XXAhqFr028M4iAJmGQ" +
            "QgiDwEAC8WW/jYHCEgqBAgKmOCqwXQvi8dW+e7Z5NceKn2sUVqx6gpw1CQmKJEQCHQXiS38bv3aMw9IECLQR2M737fHoqrHf0X1s" +
            "//amUbh4FNi9vIAmobypGQnMKvDjLbG4AdjG7aUfAgQmENjO6XgskU6peUrEkmkOjUKmai0QqyZhgSKPn6IIkwrEjcA2/CpD0iIK" +
            "e0mB7bzdHmsgxNw15p19To3C7BVOlJ8mIVGxhEpgYAG/yjBwcbqFZuGRBOKmfRut4or1Wq010zoahZmqmTgXTULi4gmdwMACcXNw" +
            "PwYOVWgEphS4P//iea8ke67dK+cS6w7dKJRI0BzjC2gSxq+RCAnMIBA3CvdjhpzkQGAkgfvzK56PFttI8WSJRaOQpVKTxqlJmLSw" +
            "z9PyCYEhBOIm5n4MEZQgCCQR+PMW5/35E89vbw39kyHGEQE1CiNWZZGYNAmLFFqaBAYXiBuI+zF4uAOGJ6SZBX66JXd/fnx7e53x" +
            "J3LIGHfvmDUKvSuw6PqahEULL20CgwvEzcTHMXjIwiNQTODjsf9zsZn7TxS59Y8iUQT/hKpR+AfCQzsBTUI7aysRIHBNIG4u7se1" +
            "2exNYByB++M6no8TWZ1IVsixhpxGoYaqOZ8KaBKe0pT4wBwECFQUiBuNj6PicqYmUETg4zEbr4tMnGySVfO+WiaNwlVB++8W0CTs" +
            "prIhAQIJBOLG4+MoH7YZCewT+Hgsxut9e66xFY9zddYonHOz10EBTcJBMJsTIJBOIG5EHo10iQh4WIFHx1e8N2zAAwU2lNNALq9C" +
            "0Si8EvL5ZQFNwmVCExAgkFQgbk4+jvirJZOmI+xGAh+PmXjdaOlpl2F4rrQahXNu9topMFGTsDNjmxEgQOC5QPzVknHD8mg838sn" +
            "swk8qv/23my5jpJP+I4SS6Y4NAqZqpUsVk1CsoIJl8ByAuMkHDcxj4ZffRinRkcieVTL7b0j89i2nED4l5ttnZk0CuvUummmmoSm" +
            "3BYjQGBCgc9+9WG76dkeJ0x/2JSieQv3Z2PYwFcJ7EmeUa8nH3n7EwGNwic4PjonoEk452YvAgQI7BXYbnri8dX4fe+kC2/36uZ/" +
            "M47mbWGm1KlHDVMn0Cl4jUIn+FmXPdkkzMohLwIECHQV+P62etwgHRm3XVL//HSL/ki+bv5vYAv8xDGxQJrFU9QoFCddd0JNwrq1" +
            "lzmBrwW8k1EgbqYyj58zoou5iUAc100WmmwRjcJkBe2Vjiahl7x1CRAgQIBAI4HEy2gUzhVPo3DOzV53ApqEOwxPCRAgQIAAgeEE" +
            "NArnSqJROOeWaa+qsWoSqvKanAABAgQIECggoFE4h6hROOdmr5uAJuGG4IdAFwGLEiBAgMARAY3CEa0v22oUvlh4dkBAk3AAy6YE" +
            "CBAgQOCVgM+rCmgUzvFqFM65Lb2XJmHp8kueAAECBAikE9AonCuZRuGc27bXco+ahOVKLmECBAgQIJBeQKNwroQahXNuS+6lSViy" +
            "7AsmLWUCBAgQmE1Ao3CuohqFc27L7aVJWK7kEiZAgMA8AjJZXkCjcO4Q0Cicc1tqL03CUuWWLAECBAgQmE5Ao3CupCM3CucysldR" +
            "AU1CUU6TESBAgAABAh0ENArn0DUK59yW2EuTsESZGydpOQIECBAg0F5Ao3DOXKNwzm36vTQJ05dYggQIECgjYBYCCQQ0CueKpFE4" +
            "5zb1XpqEqcsrOQIECBAgsJyARuFYybetNQqbhMe/BDQJfzH4FwECBAgQIDCRgEbhXDE1CufcptxLk5C9rOInQIAAAQIEHgloFB6p" +
            "vH5Po/DaaIktNAlLlFmSBAhkExAvAQJFBDQK5xg1CufcptpLkzBVOSVDgAABAgQIfBAYqVH4ENrQLzUKQ5enfnCahPrGViBAgAAB" +
            "AgT6CmgUzvlrFM65TbGXJuFIGW1LgAABAgQIZBXQKJyrnEbhnFv6vTQJ6UsoAQIErgrYnwCBZQQ0CudKrVE455Z6L01C6vIJngAB" +
            "AgQIEHgi8OxtjcIzmc/f1yh87jPdp5qE6UoqIQIECBAgQOCFgEbhBdCTjzUKT2BmfHvcJmFGbTkRIECAAAECowhoFM5VQqNwzi3d" +
            "XpqEdCUTMIHcAqInQIDAQAIahXPF0Cicc0u1lyYhVbkES4AAAQIEhhTIHJRG4Vz1NArn3NLspUlIUyqBEiBAgAABApUENArnYDUK" +
            "59xS7PV3k5AiVEESIECAAAECBKoJaBTO0WoUzrkNv5cmYfgSCZDAeQF7EiBAgMAhAY3CIa5/N9Yo/EsxzxNNwjy1lAkBAgQIrCEg" +
            "y7oCGoVzvhqFc27D7qVJGLY0AiNAgAABAgQ6CWgUzsFrFM65/bPXWA+ahLHqIRoCBAgQIEBgDAGNwrk6aBTOuQ23lyZhuJIIKKuA" +
            "uAkQIEBgOgGNwrmSahTOuQ21lyZhqHIIhgABAgQGExAOAY2CY2BJAU3CkmWXNAECBAgQIHBAQKNwAOu2aQKvW5R+PhXQJHzK40MC" +
            "BAgQIECAwF8Cbnz/Ynj5L04viXJsoEnIUSdRfhDwkgABAgQIdBBwA/w5Op/PfVJ9qklIVS7BEiBAYGoByRHIIOBG+HGVuDx2Sfuu" +
            "JiFt6QROgAABAgQIdBJwQ/we/oXH+429yiGgSchRJ1ESIECAAAECYwm4Mf67Hhz+dpju35qE6UpaPiEzEiBAgAABAg8FVr9BXj3/" +
            "hwfFLG9qEmappDwIECBwTMDWBAiUEVj1RnnVvMscNQlm0SQkKJIQCRAgQIAAgaEFBrphbuK0Wr5NUEdbRJMwWkXEQ4AAAQIECGQU" +
            "WOXGeZU8Mx6DRWPWJBTlvD6ZGQgQIECAAIG0ArPfQM+eX9oDr0bgmoQaquYkQIDAewGvCBBYR2DWG+lZ81rnyDyYqSbhIJjNCRAg" +
            "QIAAAQJ/Czz992w31LPl87RwPvgioEn4YuEZAQIECBAgQKCUwCw31rPkUaquy8yzdJOwTJUlSoAAAQIECPQQyH6DnT3+HjWfZk1N" +
            "wjSllAgBAv8IeCBAgMBIAllvtLPGPVLtU8eiSUhdPsETIECAAIFVBFLnme2GO1u8qQ+OUYPXJIxaGXERIECAAAECMwlkufHOEudM" +
            "x8aQuTRrEobMXlAECBAgQIAAgXYCo9+Ajx5fu0pZ6U2T4CAgQOCKgH0JECBA4JjAqDfio8Z1TNfWxQQ0CcUoTUSAAAECBGYRkEdl" +
            "gdFuyEeLpzK/6fcIaBL2KNmGAAECBAgQIFBWYJQb81HiKKtrtscCB97VJBzAsikBAgQIECBAoKBA7xv03usXpDRVaQFNQmlR8xGo" +
            "J2BmAgQIEJhPoNeNeq9156vgpBlpEiYtrLQIECBAIIuAOAm8tb5hb72eEicU0CQkLJqQCRAgQIAAgekEWt24t1pnugIdTij5DpqE" +
            "5AUUPgECBAgQIDCNQO0b+NrzT1MIibz5/yQ4CAg8EfA2AQIECBDoIfCfSovWmrdSuKbtLeBXEnpXwPoECBAg0FDAUgRSCJS+oS89" +
            "XwpEQV4T0CRc87M3AQIECBAgQKCGQKkb+1Lz1Mix3JxmKi6gSShOakICBAgQIECAQBGBqzf4V/cvkoRJcgpoEnLWbbao5UOAAAEC" +
            "BAg8Fjh7o392v8dReHc5AU3CciWXMAECBFoJWIcAgUICR2/4j25fKEzTzCSgSZipmnIhQIAAAQIEZhXYe+O/d7vzTvZcQkCTsESZ" +
            "JUmAAAECBAhMIPCqAXj1+QQEUmgloEloJT3OOiIhQIAAAQIE8go8awSevZ83U5F3FdAkdOW3OAECBEoJmIcAgYUEPjYEH18vRCHV" +
            "WgKahFqy5iVAgAABAgQIXBV4vv/WGGyPz7f0CYETAv8fAAD//ypJK10AAAAGSURBVAMAmNkxmHRHsF4AAAAASUVORK5CYII=";

        /// <summary>
        /// Décode une image intégrée (base64) en ImageSource.
        /// CacheOption.OnLoad : l'image est lue tout de suite, le flux
        /// peut être fermé. Freeze : l'image est partageable et peut
        /// servir de valeur par défaut partagée entre les contrôles.
        /// </summary>
        private static ImageSource EmbeddedEyeImage(string base64)
        {
            byte[] bytes = System.Convert.FromBase64String(base64);
            using System.IO.MemoryStream stream = new(bytes);
            Media.Imaging.BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = Media.Imaging.BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Le mot de passe. Synchronisé dans les deux sens avec le champ
        /// visible : taper dans le champ met Password à jour, et poser
        /// Password en code remplit le champ.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(string.Empty, OnPasswordChanged));

        /// <summary>
        /// True = mot de passe visible (mode texte), False = masqué
        /// (mode mot de passe). Lecture/écriture : l'œil la bascule,
        /// et on peut aussi la basculer en code (get/set).
        /// </summary>
        public static readonly DependencyProperty IsPasswordRevealedProperty =
            DependencyProperty.Register(
                nameof(IsPasswordRevealed),
                typeof(bool),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(false, OnIsPasswordRevealedChanged));

        /// <summary>
        /// Coins arrondis du champ. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(null));

        /// <summary>
        /// Texte d'aide affiché quand le champ est vide (ex. "Mot de passe").
        /// Vide (défaut) = pas de texte d'aide.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(""));

        /// <summary>
        /// Couleur du texte d'aide. Défaut : gris #FF808080.
        /// (Couleur pleine, sans opacité : c'est elle qui fait la
        /// différence avec le texte tapé.)
        /// </summary>
        public static readonly DependencyProperty WatermarkColorProperty =
            DependencyProperty.Register(
                nameof(WatermarkColor),
                typeof(Brush),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF808080"))));

        /// <summary>
        /// Caractère affiché à la place de chaque lettre en mode masqué.
        /// Défaut : • (rond plein).
        /// </summary>
        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordRevealCustom),
                new PropertyMetadata('●'));

        /// <summary>
        /// Longueur max du mot de passe (0 = illimitée). Appliquée aux
        /// deux champs.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(0));

        /// <summary>
        /// Image de l'œil affichée quand le mot de passe est VISIBLE.
        /// Défaut : l'icône œil intégrée au fichier (base64, aucune
        /// image externe requise).
        /// </summary>
        public static readonly DependencyProperty RevealImageProperty =
            DependencyProperty.Register(
                nameof(RevealImage),
                typeof(ImageSource),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(EmbeddedEyeImage(RevealImageBase64), OnEyeImageChanged));

        /// <summary>
        /// Image de l'œil affichée quand le mot de passe est MASQUÉ.
        /// Défaut : l'icône œil intégrée au fichier (base64, aucune
        /// image externe requise).
        /// </summary>
        public static readonly DependencyProperty HideImageProperty =
            DependencyProperty.Register(
                nameof(HideImage),
                typeof(ImageSource),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(EmbeddedEyeImage(HideImageBase64), OnEyeImageChanged));

        /// <summary>
        /// Couleur de l'œil. null (défaut) = l'image telle quelle (noire).
        /// Si définie (couleur unie), l'image de l'œil est recolorée avec
        /// cette couleur (même taille, même position que l'œil noir).
        /// </summary>
        public static readonly DependencyProperty EyeColorProperty =
            DependencyProperty.Register(
                nameof(EyeColor),
                typeof(Brush),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(null, OnEyeColorChanged));

        /// <summary>
        /// Couleur de l'œil quand la souris le survole. null (défaut) =
        /// pas de changement au survol (l'œil garde EyeColor).
        /// </summary>
        public static readonly DependencyProperty EyeHoverColorProperty =
            DependencyProperty.Register(
                nameof(EyeHoverColor),
                typeof(Brush),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(null, OnEyeColorChanged));

        /// <summary>
        /// Largeur (en px) du bouton œil. Défaut : 45.
        /// </summary>
        public static readonly DependencyProperty EyeWidthProperty =
            DependencyProperty.Register(
                nameof(EyeWidth),
                typeof(double),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(45.0));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(PasswordRevealCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool IsPasswordRevealed
        {
            get => (bool)GetValue(IsPasswordRevealedProperty);
            set => SetValue(IsPasswordRevealedProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public Brush WatermarkColor
        {
            get => (Brush)GetValue(WatermarkColorProperty);
            set => SetValue(WatermarkColorProperty, value);
        }

        public char PasswordChar
        {
            get => (char)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public ImageSource RevealImage
        {
            get => (ImageSource)GetValue(RevealImageProperty);
            set => SetValue(RevealImageProperty, value);
        }

        public ImageSource HideImage
        {
            get => (ImageSource)GetValue(HideImageProperty);
            set => SetValue(HideImageProperty, value);
        }

        public Brush EyeColor
        {
            get => (Brush)GetValue(EyeColorProperty);
            set => SetValue(EyeColorProperty, value);
        }

        public Brush EyeHoverColor
        {
            get => (Brush)GetValue(EyeHoverColorProperty);
            set => SetValue(EyeHoverColorProperty, value);
        }

        public double EyeWidth
        {
            get => (double)GetValue(EyeWidthProperty);
            set => SetValue(EyeWidthProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Controls.PasswordBox? _pwdBox;
        private Controls.TextBox? _txtBox;
        private ToggleButton? _revealButton;
        private Image? _eyeImage;

        // Vrai quand la souris survole le bouton œil (pour EyeHoverColor).
        private bool _isEyeHovered;

        // Bordure du champ, pour la lueur néon (voir RefreshNeon).
        private Border? _glowBorder;

        // Garde anti-boucle : quand on copie la valeur d'un champ à l'autre,
        // les événements (PasswordChanged / TextChanged) se redéclenchent —
        // sans ce flag, ils se recopieraient en boucle.
        private bool _isSyncing;

        #endregion

        #region Constructeur

        public PasswordRevealCustom()
        {
            // Taille de base + bordure + fond comme le PasswordBox d'origine :
            // sans ça, le contrôle apparaît minuscule, sans bordure et sans
            // fond (Control n'a pas de BorderBrush/BorderThickness/Background
            // par défaut). Posés avec SetCurrentValue : un Style ou une valeur
            // en XAML reprend le dessus normalement.
            SetCurrentValue(BackgroundProperty, Media.Brushes.White);
            SetCurrentValue(BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            SetCurrentValue(BorderThicknessProperty, new Thickness(1));
            SetCurrentValue(MinWidthProperty, 200.0);
            SetCurrentValue(MinHeightProperty, 32.0);

            // Mêmes défauts neutres que TextBoxCustom (voir son constructeur).
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordRevealCustom c)
            {
                c.SyncPasswordToBoxes();
            }
        }

        private static void OnIsPasswordRevealedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordRevealCustom c)
            {
                c.SyncPasswordToBoxes();
                c.RefreshEyeImage();
            }
        }

        private static void OnEyeImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordRevealCustom c)
            {
                c.RefreshEyeImage();
            }
        }

        private static void OnEyeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordRevealCustom c)
            {
                c.RefreshEyeImage();
            }
        }


        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordRevealCustom c)
            {
                c.RefreshNeon();
            }
        }

        #endregion

        #region Synchronisation

        /// <summary>
        /// Copie Password dans les deux champs internes (si différent).
        /// Appelé quand Password change en code, quand le mode bascule
        /// et au chargement du template.
        /// </summary>
        private void SyncPasswordToBoxes()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                string pwd = Password ?? string.Empty;
                if (_pwdBox != null && _pwdBox.Password != pwd)
                {
                    _pwdBox.Password = pwd;
                }
                if (_txtBox != null && _txtBox.Text != pwd)
                {
                    _txtBox.Text = pwd;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (_pwdBox == null) return;
            _isSyncing = true;
            try
            {
                Password = _pwdBox.Password;
                if (_txtBox != null && _txtBox.Text != _pwdBox.Password)
                {
                    _txtBox.Text = _pwdBox.Password;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (_txtBox == null) return;
            _isSyncing = true;
            try
            {
                Password = _txtBox.Text;
                if (_pwdBox != null && _pwdBox.Password != _txtBox.Text)
                {
                    _pwdBox.Password = _txtBox.Text;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Choisit l'image de l'œil : l'image brute (pas de couleur) ou
        /// l'image recolorée (EyeColor, ou EyeHoverColor quand la souris
        /// survole l'œil). L'œil est TOUJOURS rendu par le même élément
        /// (eyeImage) : teinté ou non, il a exactement la même taille
        /// et la même position (l'ancien masque sur un Border donnait un
        /// œil plus petit et décalé). Appelé au chargement, quand l'œil
        /// ou sa couleur change, quand le mode bascule et au survol.
        /// </summary>
        private void RefreshEyeImage()
        {
            if (_eyeImage == null) return;

            ImageSource current = IsPasswordRevealed ? RevealImage : HideImage;
            if (current == null)
            {
                _eyeImage.Source = null;
                return;
            }

            // Couleur effective : le survol gagne sur la couleur de base.
            Brush? color = (_isEyeHovered && EyeHoverColor != null)
                ? EyeHoverColor
                : EyeColor;

            _eyeImage.Source = (color is SolidColorBrush solid)
                ? TintedEyeImage(current, solid.Color)
                : current;
        }

        /// <summary>
        /// Recolore une image d'œil (noire sur transparent) avec une
        /// couleur unie : chaque pixel opaque prend la couleur, l'alpha
        /// (donc la forme et les bords adoucis) est gardé tel quel.
        /// L'image retournée a exactement la même taille que l'originale.
        /// </summary>
        private static ImageSource TintedEyeImage(ImageSource source, Media.Color color)
        {
            if (source is not Media.Imaging.BitmapSource bitmap) return source;

            // Bgra32 : on manipule les pixels (B, G, R, A) directement.
            Media.Imaging.FormatConvertedBitmap converted = new(bitmap, Media.PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i + 3] > 0) // pixel visible : on le teinte, alpha gardé
                {
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                }
            }

            Media.Imaging.BitmapSource tinted = Media.Imaging.BitmapSource.Create(
                width, height, converted.DpiX, converted.DpiY,
                Media.PixelFormats.Bgra32, null, pixels, stride);
            tinted.Freeze();
            return tinted;
        }

        /// <summary>
        /// Donne le focus au champ actuellement visible (le TextBox si
        /// révélé, sinon le PasswordBox). Ne fait rien si le champ
        /// visé a déjà le focus.
        /// </summary>
        private void FocusVisibleBox()
        {
            UIElement? target = IsPasswordRevealed
                ? (UIElement?)_txtBox
                : (UIElement?)_pwdBox;

            if (target != null && !target.IsKeyboardFocused)
            {
                target.Focus();
            }
        }

        #endregion

        #region Template et focus

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué).
            if (_pwdBox != null) _pwdBox.PasswordChanged -= PwdBox_PasswordChanged;
            if (_txtBox != null) _txtBox.TextChanged -= TxtBox_TextChanged;
            if (_revealButton != null)
            {
                _revealButton.Click -= RevealButton_Click;
                _revealButton.MouseEnter -= RevealButton_MouseEnter;
                _revealButton.MouseLeave -= RevealButton_MouseLeave;
            }

            _pwdBox = Template.FindName("pwdBox", this) as Controls.PasswordBox;
            _txtBox = Template.FindName("txtBox", this) as Controls.TextBox;
            _revealButton = Template.FindName("revealButton", this) as ToggleButton;
            _eyeImage = Template.FindName("eyeImage", this) as Image;
            _glowBorder = Template.FindName("glowBorder", this) as Border;

            if (_pwdBox != null) _pwdBox.PasswordChanged += PwdBox_PasswordChanged;
            if (_txtBox != null) _txtBox.TextChanged += TxtBox_TextChanged;
            if (_revealButton != null)
            {
                _revealButton.Click += RevealButton_Click;
                _revealButton.MouseEnter += RevealButton_MouseEnter;
                _revealButton.MouseLeave += RevealButton_MouseLeave;
            }

            // État initial : les triggers du template gèrent la visibilité
            // des champs, mais la VALEUR (Password) et l'image de l'œil
            // doivent être posées en code au chargement.
            SyncPasswordToBoxes();
            RefreshEyeImage();
            RefreshNeon();
        }

        /// <summary>
        /// Largeur désirée : si le conteneur ne donne pas de largeur
        /// (Canvas, colonne Auto, StackPanel...), la largeur désirée
        /// serait celle du texte et le champ grandirait à chaque lettre
        /// tapée. On plafonne à MinWidth (200 par défaut) : le champ
        /// garde sa taille et le texte défile dedans, comme un TextBox
        /// classique dans une grille.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            if (double.IsInfinity(availableSize.Width))
            {
                size.Width = MinWidth;
            }
            return size;
        }

        private void RevealButton_Click(object sender, RoutedEventArgs e)
        {
            // Le clic a déjà basculé IsChecked -> IsPasswordRevealed :
            // on remet le focus dans le champ qui vient d'apparaître
            // pour pouvoir continuer à taper sans re-cliquer.
            FocusVisibleBox();
        }

        private void RevealButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _isEyeHovered = true;
            RefreshEyeImage();
        }

        private void RevealButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _isEyeHovered = false;
            RefreshEyeImage();
        }


        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            // Tabulation : le focus arrive sur le contrôle lui-même
            // (les champs internes ne sont pas des arrêts Tab) — on le
            // transfère au champ visible pour pouvoir taper tout de suite.
            // Sans danger de boucle : si le champ visé a déjà le focus,
            // FocusVisibleBox ne fait rien.
            FocusVisibleBox();
        }


        #endregion

        #region Néon (lueur sur la bordure, texte net)

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Bordure externe : c'est elle qui porte Background,
            // BorderBrush, BorderThickness et CornerRadius. Les deux
            // champs internes sont transparents et sans bordure : le fond
            // et la bordure sont donc IDENTIQUES dans les deux modes.
            // Grille racine : "glowBorder" (fond + bordure, porte la
            // lueur néon) derrière la grille de contenu (texte + œil nets).
            FrameworkElementFactory rootGrid = new(typeof(Controls.Grid));
            rootGrid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            FrameworkElementFactory grid = new(typeof(Controls.Grid));
            // Marge = le Padding du contrôle (le texte et l'œil ne
            // touchent pas les coins arrondis).
            grid.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            // Colonne 0 = le champ (prend toute la place), colonne 1 = l'œil.
            FrameworkElementFactory colText = new(typeof(ColumnDefinition));
            colText.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory colEye = new(typeof(ColumnDefinition));
            colEye.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(colText);
            grid.AppendChild(colEye);

            // Champ masqué (visible par défaut). Les deux champs partagent
            // les mêmes bindings : ils ont donc exactement les mêmes
            // couleurs, police, marges et alignements, quel que soit le mode.
            FrameworkElementFactory pwdBox = new(typeof(Controls.PasswordBox));
            pwdBox.Name = "pwdBox";
            pwdBox.SetValue(Controls.Grid.ColumnProperty, 0);
            pwdBox.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            pwdBox.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            pwdBox.SetValue(KeyboardNavigation.IsTabStopProperty, false);
            pwdBox.SetBinding(
                Controls.Control.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.FontFamilyProperty,
                new WPF.Data.Binding(nameof(FontFamily)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.FontStyleProperty,
                new WPF.Data.Binding(nameof(FontStyle)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.FontWeightProperty,
                new WPF.Data.Binding(nameof(FontWeight)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.FontStretchProperty,
                new WPF.Data.Binding(nameof(FontStretch)) { RelativeSource = templatedParent });
            // Padding interne à 0 : c'est la marge de la grille qui
            // éloigne le texte du bord.
            pwdBox.SetValue(Controls.Control.PaddingProperty, new Thickness(0));
            pwdBox.SetBinding(
                Controls.Control.VerticalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(VerticalContentAlignment)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.Control.HorizontalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.PasswordBox.PasswordCharProperty,
                new WPF.Data.Binding(nameof(PasswordChar)) { RelativeSource = templatedParent });
            pwdBox.SetBinding(
                Controls.PasswordBox.MaxLengthProperty,
                new WPF.Data.Binding(nameof(MaxLength)) { RelativeSource = templatedParent });

            // Champ visible (caché par défaut, même bindings que pwdBox).
            FrameworkElementFactory txtBox = new(typeof(Controls.TextBox));
            txtBox.Name = "txtBox";
            txtBox.SetValue(Controls.Grid.ColumnProperty, 0);
            txtBox.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            txtBox.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            txtBox.SetValue(KeyboardNavigation.IsTabStopProperty, false);
            txtBox.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            txtBox.SetBinding(
                Controls.Control.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.FontFamilyProperty,
                new WPF.Data.Binding(nameof(FontFamily)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.FontStyleProperty,
                new WPF.Data.Binding(nameof(FontStyle)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.FontWeightProperty,
                new WPF.Data.Binding(nameof(FontWeight)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.FontStretchProperty,
                new WPF.Data.Binding(nameof(FontStretch)) { RelativeSource = templatedParent });
            txtBox.SetValue(Controls.Control.PaddingProperty, new Thickness(0));
            txtBox.SetBinding(
                Controls.Control.VerticalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(VerticalContentAlignment)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.Control.HorizontalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });
            txtBox.SetBinding(
                Controls.TextBox.MaxLengthProperty,
                new WPF.Data.Binding(nameof(MaxLength)) { RelativeSource = templatedParent });

            // Bouton œil : un ToggleButton (coché = révélé) sans focus
            // (le focus reste dans le champ quand on clique dessus).
            FrameworkElementFactory revealButton = new(typeof(ToggleButton));
            revealButton.Name = "revealButton";
            revealButton.SetValue(Controls.Grid.ColumnProperty, 1);
            revealButton.SetValue(FrameworkElement.FocusableProperty, false);
            revealButton.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            revealButton.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            revealButton.SetBinding(
                ToggleButton.IsCheckedProperty,
                new WPF.Data.Binding(nameof(IsPasswordRevealed))
                {
                    RelativeSource = templatedParent,
                    Mode = WPF.Data.BindingMode.TwoWay
                });
            revealButton.SetBinding(
                FrameworkElement.WidthProperty,
                new WPF.Data.Binding(nameof(EyeWidth)) { RelativeSource = templatedParent });
            revealButton.SetValue(Controls.Control.TemplateProperty, BuildEyeButtonTemplate());
            // (Pas de Contenu : l'œil est dessiné à côté, voir ci-dessous.
            // Le bouton reste juste la zone cliquable transparente.)

            // La grille de l'œil : l'image et la teinte superposées, en
            // enfant DIRECT de la grille de contenu (colonne 1).
            // IMPORTANT : avant, cette grille était le Contenu du bouton —
            // et là, le template ne la retrouvait pas : ni Template.FindName
            // (EyeColor ne s'appliquait pas), ni le trigger (l'œil ne
            // changeait pas au clic). En enfant direct, FindName et les
            // triggers la voient, comme pwdBox/txtBox.
            // IsHitTestVisible=false : les clics traversent l'œil et
            // atteignent le bouton (juste en dessous).
            FrameworkElementFactory eyeGrid = new(typeof(Controls.Grid));
            eyeGrid.SetValue(Controls.Grid.ColumnProperty, 1);
            eyeGrid.SetValue(FrameworkElement.IsHitTestVisibleProperty, false);
            eyeGrid.SetBinding(
                FrameworkElement.WidthProperty,
                new WPF.Data.Binding(nameof(EyeWidth)) { RelativeSource = templatedParent });

            FrameworkElementFactory eyeImage = new(typeof(Controls.Image));
            eyeImage.Name = "eyeImage";
            eyeImage.SetValue(FrameworkElement.MarginProperty, new Thickness(6));
            eyeImage.SetValue(Controls.Image.StretchProperty, Stretch.Uniform);
            // Rendu net ("full 4K") : tes PNG font ~780 px de large pour un
            // bouton de 45 px — c'est déjà ~17x la taille affichée. Ce qui
            // fait la netteté, c'est le filtre de redimensionnement :
            // HighQuality (Fant) au lieu du bilinéaire par défaut.
            eyeImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            // Source par défaut : œil "masqué" ; le trigger sur
            // IsPasswordRevealed la remplace par RevealImage.
            eyeImage.SetBinding(
                Controls.Image.SourceProperty,
                new WPF.Data.Binding(nameof(HideImage)) { RelativeSource = templatedParent });

            // (Plus de couche teintée séparée : la couleur est appliquée en
            // recolorant l'image elle-même, en code — l'œil teinté a donc
            // exactement la même taille et la même position que l'œil noir.)
            eyeGrid.AppendChild(eyeImage);


            // Texte d'aide : visible seulement quand le champ est vide
            // (trigger sur Text/Password = ""). Ne capte pas les clics :
            // on peut cliquer "à travers" pour mettre le focus dans le champ.
            // Taille = celle du texte (FontSize du contrôle), couleur =
            // WatermarkColor, toujours un peu transparente (0,6) : la couleur
            // se choisit, la transparence est fixe (pas de propriété pour ça).
            FrameworkElementFactory watermark = new(typeof(Controls.TextBlock));
            watermark.Name = "watermark";
            watermark.SetValue(Controls.Grid.ColumnProperty, 0);
            watermark.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            watermark.SetValue(UIElement.IsHitTestVisibleProperty, false);
            watermark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            // Toujours un peu transparente (0,6), comme un vrai placeholder :
            // la couleur se choisit avec WatermarkColor, la transparence est fixe.
            watermark.SetValue(UIElement.OpacityProperty, 0.6);
            watermark.SetBinding(
                Controls.TextBlock.TextProperty,
                new WPF.Data.Binding(nameof(Watermark)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(WatermarkColor)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            // Aligné avec le curseur : le texte est décalé d'un Padding vers
            // la droite (le champ applique son Padding deux fois : une fois
            // par la marge du modèle, une fois en interne), alors que le
            // watermark n'en a qu'un. On lui ajoute donc un Padding à gauche
            // égal au Padding du champ, sinon le curseur tombe au milieu de
            // la première lettre du texte d'aide.
            watermark.SetBinding(
                Controls.TextBlock.PaddingProperty,
                new WPF.Data.Binding("Padding.Left")
                {
                    RelativeSource = templatedParent,
                    Converter = new DoubleToLeftMarginConverter()
                });

            // Le watermark est DERRIÈRE les deux champs : les points / le
            // texte et le curseur peignent par-dessus, toujours visibles.
            grid.AppendChild(watermark);
            grid.AppendChild(pwdBox);
            grid.AppendChild(txtBox);
            grid.AppendChild(revealButton);
            grid.AppendChild(eyeGrid);
            rootGrid.AppendChild(border);
            rootGrid.AppendChild(grid);

            Controls.ControlTemplate template = new(typeof(PasswordRevealCustom));
            template.VisualTree = rootGrid;

            // Révélé : le TextBox remplace le PasswordBox
            // (l'image de l'œil, elle, est posée en code).
            Trigger revealedTrigger = new()
            {
                Property = IsPasswordRevealedProperty,
                Value = true
            };
            revealedTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "pwdBox",
                Value = Visibility.Collapsed
            });
            revealedTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "txtBox",
                Value = Visibility.Visible
            });
            // (La Source de l'œil n'est plus changée ici : c'est le code
            // (RefreshEyeImage) qui la pose — image brute ou recolorée.)
            template.Triggers.Add(revealedTrigger);

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Champ vide : le texte d'aide apparaît.
            Trigger emptyTrigger = new()
            {
                Property = PasswordProperty,
                Value = ""
            };
            emptyTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "watermark",
                Value = Visibility.Visible
            });
            template.Triggers.Add(emptyTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Template du bouton œil : un fond transparent qui ne se voit
        /// qu'au survol, avec le contenu (l'œil) centré. Sans ça, le
        /// ToggleButton aurait l'apparence grise par défaut de WPF.
        /// </summary>
        private static Controls.ControlTemplate BuildEyeButtonTemplate()
        {
            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "eyeButtonBorder";
            border.SetValue(Controls.Border.BackgroundProperty, Media.Brushes.Transparent);

            // NOTE : "Content" en chaîne, pas nameof(Content) : la classe
            // PasswordRevealCustom n'a pas de propriété Content (c'est le
            // ToggleButton, le TemplatedParent de ce template, qui l'a).
            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content")
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(ToggleButton));
            template.VisualTree = border;

            // Survol : léger voile gris (visible sur fond clair : le champ est blanc par défaut).
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "eyeButtonBorder",
                Value = new SolidColorBrush(Color.FromArgb(0x1F, 0x00, 0x00, 0x00))
            });
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region SearchBoxCustom

    /// <summary>
    /// Champ de recherche : un TextBox avec une loupe à gauche, un bouton
    /// effacer (X) à droite qui n'apparaît que quand il y a du texte, et
    /// un texte d'aide (Watermark, ex. "Rechercher...") quand le champ
    /// est vide. La touche Entrée déclenche l'événement Search.
    /// La loupe et le X sont des images intégrées en base64 dans le .cs
    /// (comme les yeux) : le fichier voyage seul, sans dossier d'images.
    /// IconColor / ClearIconColor les teintent, SearchIcon / ClearIcon
    /// les remplacent par tes propres images.
    /// </summary>
    public class SearchBoxCustom : Controls.TextBox
    {
        #region Dependency Properties

        /// <summary>
        /// Coins arrondis du champ. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SearchBoxCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(null));

        /// <summary>
        /// Couleur de la loupe (teinte de l'image intégrée). null
        /// (défaut) = noire.
        /// </summary>
        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register(
                nameof(IconColor),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(null, OnIconColorChanged));

        /// <summary>
        /// Couleur de la loupe quand la souris est dessus. null
        /// (défaut) = IconColor (pas de changement au survol).
        /// </summary>
        public static readonly DependencyProperty IconHoverColorProperty =
            DependencyProperty.Register(
                nameof(IconHoverColor),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(null, OnIconColorChanged));

        /// <summary>
        /// Couleur du X (effacer), teinte de l'image intégrée.
        /// null (défaut) = noire.
        /// </summary>
        public static readonly DependencyProperty ClearIconColorProperty =
            DependencyProperty.Register(
                nameof(ClearIconColor),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(null, OnClearIconColorChanged));

        /// <summary>
        /// Couleur du X quand la souris est dessus. null
        /// (défaut) = ClearIconColor (pas de changement au survol).
        /// </summary>
        public static readonly DependencyProperty ClearIconHoverColorProperty =
            DependencyProperty.Register(
                nameof(ClearIconHoverColor),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(null, OnClearIconColorChanged));

        /// <summary>
        /// Image de la loupe. Défaut = la loupe noire intégrée en base64
        /// (voir la région "Images intégrées"). Mets ton propre PNG pour
        /// la remplacer. Teintée par IconColor (couleur pleine seulement ;
        /// une image non bitmap s'affiche telle quelle, non teintée).
        /// null = pas de loupe du tout.
        /// </summary>
        public static readonly DependencyProperty SearchIconProperty =
            DependencyProperty.Register(
                nameof(SearchIcon),
                typeof(Media.ImageSource),
                typeof(SearchBoxCustom),
                new PropertyMetadata(EmbeddedIcon(SearchIconBase64), OnSearchIconChanged));

        /// <summary>
        /// Image du X (effacer). Défaut = le X noir intégré en base64
        /// (voir la région "Images intégrées"). Mets ton propre PNG pour
        /// le remplacer. Teinté par ClearIconColor (couleur pleine
        /// seulement ; une image non bitmap s'affiche telle quelle).
        /// null = pas de X du tout.
        /// </summary>
        public static readonly DependencyProperty ClearIconProperty =
            DependencyProperty.Register(
                nameof(ClearIcon),
                typeof(Media.ImageSource),
                typeof(SearchBoxCustom),
                new PropertyMetadata(EmbeddedIcon(ClearIconBase64), OnClearIconChanged));

        /// <summary>
        /// Texte d'aide affiché quand le champ est vide (ex. "Rechercher...").
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(SearchBoxCustom),
                new PropertyMetadata("Rechercher..."));

        /// <summary>
        /// Couleur du texte d'aide. Défaut : gris #FF808080.
        /// </summary>
        public static readonly DependencyProperty WatermarkColorProperty =
            DependencyProperty.Register(
                nameof(WatermarkColor),
                typeof(Brush),
                typeof(SearchBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF808080"))));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(SearchBoxCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(SearchBoxCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(SearchBoxCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public Brush IconColor
        {
            get => (Brush)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        public Brush IconHoverColor
        {
            get => (Brush)GetValue(IconHoverColorProperty);
            set => SetValue(IconHoverColorProperty, value);
        }

        public Brush ClearIconColor
        {
            get => (Brush)GetValue(ClearIconColorProperty);
            set => SetValue(ClearIconColorProperty, value);
        }

        public Brush ClearIconHoverColor
        {
            get => (Brush)GetValue(ClearIconHoverColorProperty);
            set => SetValue(ClearIconHoverColorProperty, value);
        }

        public Media.ImageSource SearchIcon
        {
            get => (Media.ImageSource)GetValue(SearchIconProperty);
            set => SetValue(SearchIconProperty, value);
        }

        public Media.ImageSource ClearIcon
        {
            get => (Media.ImageSource)GetValue(ClearIconProperty);
            set => SetValue(ClearIconProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public Brush WatermarkColor
        {
            get => (Brush)GetValue(WatermarkColorProperty);
            set => SetValue(WatermarkColorProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Controls.Button? _clearButton;

        private Controls.Image? _searchImage;
        private Controls.Image? _clearImage;
        private bool _isSearchIconHovered;
        private bool _isClearIconHovered;

        #endregion


        #region Images intégrées (base64)

        // Loupe + X noirs (512x512, fond transparent), intégrés dans le
        // .cs pour que le fichier voyage seul, sans dossier d'images —
        // exactement comme les yeux dans PasswordRevealCustom.
        // Pour régénérer une icône (après l'avoir redessinée en noir
        // sur fond transparent) :
        //   [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\...\loupe.png"))
        // puis remplacer le contenu de la constante (découpé en
        // morceaux de 100 caractères).
        private const string SearchIconBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAgAAAAIACAYAAAD0eNT6AAARUUlEQVR4nO3cbXobya2AUcVP9r/lyQ+PY1sixSa6vgCcs4B7KTUK" +
            "9bKtzMcHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwBj/2f0B4ED/TPy/7cwBR7CM" +
            "6Gbm5T6KcwlMZ9FQVYaL/l3OKzCMhUIFFS/7q5xhIMTyIKPOF/4rzjRwiWVBBi78OGcceMhy4FQu/fGcd+D/LARO4tJfx9mH5iwB" +
            "dnPp72cPQEMOPju49M9lJ0ATDjsrufjzsBugOIec2Vz6+dkTUJCDzSwu/nrsCyjEgWY0F3999gYU4CAziou/H/sDEnOAucvFjz0C" +
            "CTm4RLn4+cw+gUQcWN7l4ucVewUScFC5ysXPu+wXOJgDyisufu6yZ+BADibPuPgZzb6Bg/zY/QE4ksufGcwVHESR8ycLmlXsHtjM" +
            "GwB+cfmzknmDzVQ4FjG72UOwgTcAvbn8OYE5hA2Ud08WLqeyk2ARbwD6cflzMvMJiwiAXixXMjCnsIDXbT1YqGRlR8Ek3gDU5/In" +
            "M/MLkwiA2ixPKjDHMIHXazVZmFRlZ8Eg3gDU4/KnMvMNgwiAWixHOjDnMIAAqMNSpBPzDjcJgBosQzoy93CDAMjPEqQz8w9B/qI2" +
            "L4sP/mafwRu8AcjJ5Q9fORfwBgGQjyUHzzkfcJEAyMVyg9ecE7hAAORhqcF1zgu8IABysMzgfc4NfEMAnM8SgzjnB54QAGezvOA+" +
            "5wgeEADnsrRgHOcJPhEAANCQ/3LWmXxbOcPI8+GZnsHOg385DOdxUax1whnwzNc64ZnDdg7CWVwEc2Wad7MwV6ZZgCkcgnNY+ONV" +
            "mm/zMV6l+YC3OQBnsNzH6DTPZmaMTjMDfzH8+1nk95hhM3SXGaKl/+7+ABBkaf/263chBIDLLNG9LOz3mNfrzNZ7zBbtGPp9LOjr" +
            "zGmcObvOnNGKgd/DUr7GfI5j5q4xc7ThbwA4kSU8nr8TAP5i0a5nAT9nHtcxh8+ZQ1ow6GtZuo+Zw33M5GNmkvIM+ToW7Vfm7xzm" +
            "8yvzSWn+BoAdLNbz+BsBaObH7g/QhKX6m8v/bJ7Pb84tpTns81kiP5m1fMzuT2aXkrwBYAULNCfPDQoTAHP5BuUSyc7zc44pyuGe" +
            "p/vSMFv1mGkoxBsAZrAoa/JcoRABMEfnb0ouido6P9/O55qCOh/mWbouCbPUj1mHxLwBYAQLsSfPHRITAGN1/EbkEuit4/PveM4p" +
            "SABwR8flz1fmABISAON0+1Zg6fOnbvPQ7bxTkAAgotuy5xpzAYkIgDE6fRuw5PlOp/nodO4pSADwjk7LnThzAgkIgPu6fAuw1HlH" +
            "l3npcv4pSABwRZdlzljmBg4mAO7pUP+WOHd0mJ8Oe4CCBADf6bC8mc8cwYEEQFz16re0Gan6PFXfBxQkAACgIQEQU732q39bY4/q" +
            "c1V9L1CMAOCz6kuavcwXHEIAvK9y5VvOrFB5zirvB4oRAADQkAB4T+W6r/ytjPNUnrfKe4JCBAAfH7WXMecyd7CRAMASZifzB5sI" +
            "gOu81gOusi84ngDozbcvTmAOYQMBcE3Fmrd0OUnFeay4NyhEAABAQwKgp4rftsjPXMJCAuA1r/GAKPuDYwmAfnzL4mTmExYRAADQ" +
            "kAD4XrXXd75dkUG1Oa22RyhCAABAQwKgj2rfqqjNvMJkAuA5r+2AUewTjiMAevBtiozMLUwkAACgIQHwWKXXdb5FkVml+a20VyhA" +
            "AABAQwKgtkrfnujLHMMEAgAAGhIAX/l3OmAW+4VjCIC6vDalEvMMgwkAAGhIANTk2xIVmWsYSAD8zb/PAbPZMxxBAABAQwKgHq9J" +
            "qcx8wyACAAAaEgAA0JAA+M0f5gCr2DdsJwBq8e+jdGDOYQABAAANCQAAaEgAAEBDAqAO/y5KJ+YdbhIAP/mLXGA1e4etBAAANCQA" +
            "AKAhAQAADQmAGvxBFB2Ze7hBAABAQwIAABoSAADQkAAAgIYEgP8YB7CP/cM2AgAAGhIA+fmfQtGZ+YcgAQAADQkAAGhIAABAQwIA" +
            "ABoSAADQkAAAgIYEAAA0JAAAoCEBAAANCQAAaEgAAEBDAgAAGhIAANCQAACAhgQAADQkAACgIQEAAA0JAABoSADk98/uDwAbmX8I" +
            "EgAA0JAA+Pj4z+4PALRl/7CNAACAhgQAADQkAACgIQEAAA0JgBr8T6HoyNzDDQIAABoSAADQkAAAgIYEwE/+YxzAavYOWwmAOvxB" +
            "FJ2Yd7hJAABAQwIAABoSAADQkACoxb+L0oE5hwEEwG/+IhdYxb5hOwEAAA0JAABoSADU499Hqcx8wyACAAAaEgB/84c5wGz2DEcQ" +
            "ADV5TUpF5hoGEgAA0JAAqMu3JSoxzzCYAPjKv88Bs9gvHEMAAEBDAqA2r02pwBzDBAIAABoSAI9V+nc6357IrNL8VtorFCAAAKAh" +
            "AdBDpW9R9GFuYSIB8JzXdcAo9gnHEQB9+DZFJuYVJhMAANCQAPhetdd2vlWRQbU5rbZHKEIAAEBDAqCfat+uqMV8wiIC4DWv74Ao" +
            "+4NjCYCefMviROYSFhIAANCQALim4ms837Y4ScV5rLg3KEQA9FZx6ZKPOYQNBMB1ah64yr7geAIA377YyfzBJgKAjw9LmD3MHWwk" +
            "AN5T+bWeZcxKleet8p6gEAEAAA0JgPdVrvvK38o4R+U5q7wfKEYA8Fnl5cx+5gsOIQBiqle+Jc0M1eeq+l6gGAEAAA0JgLjqtV/9" +
            "2xprVZ+n6vuAggQA36m+tFnDHMGBBMA9Harf8uaODvPTYQ9QkADgig5LnPHMDRxMANzXpf4tc97RZV66nH8KEgC8o8tS5x5zAgkI" +
            "gDE6fQuw3PlOp/nodO4pSAAQ0WnJc525gEQEwDjdvg1Y9vyp2zx0O+8UJAC4o9vS5zFzAAkJgLE6fiuw/Hvr+Pw7nnMKEgCM0PES" +
            "wHOH1JTsHJ0Xo5mqz3xDAd4AzNF5SXS+HDro/Hw7n2sKEgDM0PmSqMxzhUIU7VwWphmrwBybYwryBmAuS8PlkZ3n5xxTlABgBZdI" +
            "Tp4bFKZs17BIfzNz5zOvfzOzlOQNwBoWyG8ul7N5Pl/5nVCSi2kdS+Qr83cO8/maeaUUA72WJfuYOdzHTL7HrFKGYV7Pwn3OPK5j" +
            "DuPMKSUY5D0s3++Zy3nM3hhmlPQM8R6W8DXmcxwzN575JDUDvI+FfJ05jTNnc5lN0jK8e1nO7zGv15mtdcwlKRnc/SzqGLP7lVna" +
            "xzySjqHdz9K+xwyboVOYRVIxsGewwMfoNM9m5kydZpDkDOs5LPTxKs23+cij0txRmEE9iyU/V6Z5Nwu5ZZo1mjKk57H41zrhDHjm" +
            "NZ0wW/CUAT2TC+EMI8+HZ9qTHcuxDOe5XBhQgz3LkX7s/gAAxYl5jiQAzuVbA9QhAjiOADibCIA6RABHEQDnEwFQhwjgGAIgBxEA" +
            "dYgAjiAA8hABUIcIYDsBkIsIgDpEAFsJgHxEANQhAthGAOQkAqAOEcAWLpL8LA+owT5mKW8A8rM0oAYxz1ICoAYRADWIAJYRAHWI" +
            "AKhBBLCEAKhFBEANIoDpBEA9IgBqEAFM5bKozQKB/OxppvAGoDaLA/IT8kwhAOoTAZCfCGA4l0MvlgjkZmczjDcAvVgekJuIZxgB" +
            "0I8IgNxEAEO4DHqzSCAv+5tbvAHozQKBvAQ8t7gA+MUyYbU/94/5i7PHCfEGgF8sEVb6PG/mL048EeLQ8YiFwiyvdo7Zi7PPeYs3" +
            "ADxikTDDlbkye3Hiibc4bLxiqXBXZM+Yuzh7nUsMCldZyLzr7n4xc3F2Oy8ZEt5lKfPKyL1i3uLsd75lQIiymPls1j4xa3F2PE8Z" +
            "Du6ynFmxR8xZnD3PQwaDUSzoflbvDzMWZ9fzhaFgNEu6vp17w3zF2ff8xUAwi0Vdzyn7wmzFnfIMOYBhYDbLOr8T94S5ijvxebKB" +
            "QWAlSzuPDLvBPMVleL5MZgjYweI+V7adYJbisj1rBjMA7GaB75d9D5ihuOzPnhs8fE5ika9T7eybnbhqs8BFHjynstDHq37ezUxc" +
            "9dngAQ+dDCz2uG5n3KzEdZuV9jxwMrLkn3Omzccd5qcRD5sKOi98Z/ixzjNxl5lqwoOmqooXgPP6noozsIpZa8BDppsMl4JzOU6G" +
            "530qc1icBwxfzbw0nLn1RECceS3MwwU6EAFx7omifuz+AAALuMTixFNRAgDoQgTEiYCCBADQiQiIEwHFCACgGxEQJwIKEQBARyIg" +
            "TgQUIQCArkRAnAgoQAAAnYmAOBGQnAAAuhMBcSIgMQEAIALuEAFJCQCAn0RAnAhISAAA/CYC4kRAMgIA4G8iIE4EJCIAAL4SAXEi" +
            "IAkBAPCYCIgTAQkIAIDnRECcCDicAAD4ngiIEwEHEwAAr4mAOBFwKAEAcI0IiBMBBxIAANeJgDgRcBgBAPAeERAnAg4iAADeJwLi" +
            "RMAhBABAjAiIEwEHEAAAcSIgTgRsJgAA7hEBcSJgIwEAcJ8IiBMBmwgAgDFEQJwI2EAAAIwjAuJEwGICAGAsERAnAhYSAADjiYA4" +
            "EbCIAACYQwTEiYAFBADAPCIgTgRMJgAA5hIBcSJgIgEAMJ8IiBMBkwgAgDVEQJwImEAAAKwjAuJEwGACAGAtERAnAgYSAADriYA4" +
            "ETCIAADYQwTEiYABBADAPiIgTgTcJAAA9hIBcSLgBgEAsJ8IiBMBQQIA4AwiIE4EBAgAgHOIgDgR8CYBAHAWERAnAt4gAADOIwLi" +
            "RMBFAgDgTCIgTgRcIAAAziUC4kTACwIA4GwiIE4EfEMAAJxPBMSJgCcEAEAOIiBOBDwgAADyEAFxIuATAQCQiwiIEwF/EAAA+YiA" +
            "OBHwLwEAkJMIiBMBHwIAIDMRENc+AgQAQG4iIK51BAgAgPxEQFzbCBAAADWIgLiWESAAAOoQAXHtIkAAANQiAuJaRYAAAKhHBMS1" +
            "iQABAFCTCIhrEQECAKAuERBXPgIEAEBtIiCudAQIAID6REBc2QgQAAA9iIC4khEgAAD6EAFx5SJAAAD0IgLiSkWAAADoRwTElYkA" +
            "AQDQkwiIKxEBAgCgLxEQlz4CBABAbyIgLnUECAAAREBc2ggQAAB8fIiAO1JGgAAA4BcREJcuAgQAAH8SAXGpIkAAAPCZCIhJ9XsT" +
            "AAA8kuoyO0C635cAAOCZdJfaJil/TwIAgO+kvNwWSvv7EQAAvJL2kpss9e9FAABwRerLboL0vw8BAMBV6S+9QUr8HgQAAO8ocfnd" +
            "UObnFwAAvKvMJfimUj+3AAAgotRleEG5n1cAABBV7lJ8ouTPKQAAuKPk5fiHsj+fAADgrqqXZNWf6+PjQwAAMEa1y7Laz/OFAABg" +
            "lCqXZpWf41sCAICRsl+e2T//ZQIAgNGyXqJZP3eIAABghmyXabbPe5sAAGCWLJdqls85lAAAYKbTL9fTP980AgCA2U69ZE/9XEsI" +
            "AABWOO2yPe3zLCcAAFjllEv3lM+xlQAAYKXdl+/u///HEAAArLbrEnb5/0EAALDD6svY5f+JAABgl1WXssv/AQEAwE6zL2eX/xMC" +
            "AIDdZl3SLv9vCAAATjD6snb5vyAAADjFqEvb5X+BAADgJHcvb5f/RQIAgNNEL3GX/xsEAAAnevcyd/m/SQAAcKqrl7rLP0AAAHCy" +
            "V5e7yz9IAABwumeXvMv/BgEAQAafL3uXPwA08s/uDwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAg/wPKaeaDtnQAzAAAAAASUVORK5CYII=";

        private const string ClearIconBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAgAAAAIACAYAAAD0eNT6AAAS7UlEQVR4nO3Y23JjRw4EQHr//59nH2a91sgSRda5NIDOjHDMow67" +
            "G4UKPx4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA9fxa/QHU8J/VH0A5wgHm+vXpXzamAPCRcIC5Ps+1Od+cAsDf" +
            "hAPM9d08m/ONKQA8HsIBJvtpjs35phQAhAPM9er8mvMNKQB7Ew4w17tza843owDsSzjAXOm8mvONKAB7Eg4w19E5NeebUAD2Ixxg" +
            "rrPm05xvQAHYi3CAuc6eS3M+nAKwD+EAc101j+Z8MAVgD8IB5rp6Ds35UArAfMIB5rpr/sz5QArAbMIB5rp77sz5MArAXMIB5lo1" +
            "b+Z8EAVgJuEAc62es9V/n5MoAPOsHs7Vfx8mqzJfVb6DAxSAWaoMZZXvgEmqzVW17+FNCsAc1Yax2vdAZ1Xnqep38QIFYIaqQ1j1" +
            "u6CT6nNU/fv4hgLQX/Xhq/59UFmX+enynXygAPTWZei6fCdU0m1uun3v9hSAvroNW7fvhZW6zkvX796SAtBT1yHr+t1wp+5z0v37" +
            "t6EA9NN9uLp/P1xpynxM+R2jKQC9TBmqKb8DzjRtLqb9nnEUgD6mDdO03wNHTJ2Hqb9rBAWgh6lDNPV3wTumz8H039eWAlDf9OGZ" +
            "/vvgmV3e/y6/sxUFoLZdhmaX3wkf7fbud/u95SkAde02LLv9Xva263vf9XeXpADUtOuQ7Pq72cvu73z331+GAlDP7sOx++9nNu/7" +
            "N+dQgAJQi6H4zTkwkXf9J+exmAJQh2H4k/NgEu/5a85lIQWgBkPwNefCBN7xc85nEQVgPY//OedDZ97va5zTAgrAWh79a5wTHXm3" +
            "73FeN1MA1vHY3+O86MR7zTi3GykAa3jkGedGB95p7q/VH7ATBWANjzwnXKnM+8zJxZspAOt47DkhS0XeZU4eLqAArOXR54QtlXiP" +
            "OTm4iAKwnsefE7pU4B3m5N9CCkANhiAnfFnJ+8vJvcUUgDoMQ04Is4J3l5N3BSgAtRiKnDDmTt5bTs4VoQDUYzhyQpk7eGc5+VaI" +
            "AlCTIckJZ67kfeXkWjEKQF2GJSekuYJ3lZNnBSkAtRmanLDmTN5TTo4VpQDUZ3hyQpszeEc5+VWYAtCDIcoJb47wfnJyqzgFoA/D" +
            "lBPiJLybnLxqQAHoxVDlhDnv8F5ycqoJBaAfw5UT6rzCO8nJp0YUgJ4MWU6484z3kZNLzSgAfRm2nJDnK95FTh41pAD0Zuhywp6P" +
            "vIecHGpKAejP8OWEPo+Hd3CE/GlMAZjBEOaE/97cf07uNKcAzGEYc5bAntx7Tt4MoADMYihzlsFe3HdOzgyhAMxjOHOWwh7cc06+" +
            "DKIAzGRIc5bDbO43J1eGUQDmMqw5S2Im95qTJwMpALMZ2pxlMYv7zMmRoRSA+QxvztKYwT3m5MdgCsAeDHHO8ujN/eXkxnAKwD4M" +
            "c84S6cm95eTFBhSAvRjqnGXSi/vKyYlNKAD7Mdw5S6UH95STDxtRAPZkyHOWS23uJycXNqMA7Muw5yyZmtxLTh5sSAHYm6HPWTa1" +
            "uI+cHNiUAoDhz1k6NbiHnPnfmALA4yEEjrB81nL+OXO/OQWAvwmDnCW0hnPPmXcUAP4gFHKW0b2cd86c83g8FAD+TTjkLKV7OOec" +
            "+eb/FAC+IiRyltO1nG/OXPMHBYDvCIucJXUN55ozz/yLAsAzQiNnWZ3LeebMMV9SAPiJ8MhZWudwjjnzy7cUAF4hRHKW1zHOL2du" +
            "eUoB4FXCJGeJZZxbzrzyIwWAdwiVnGX2HueVM6e8RAHgXcIlZ6m9xjnlzCcvUwBICJmc5fac88mZS96iAJASNjlL7mvOJWceeZsC" +
            "wBFCJ2fZ/cl55MwhEQWAo4RPztL7zTnkzB8xBYAzCKHc7stv999/hLnjEAWAswij3K5LcNfffQbzxmEKAGcSSrndluFuv/dM5oxT" +
            "KACcTTjldlmKu/zOK5gvTqMAcAUhlZu+HKf/viuZK06lAHAVYZWbuiSn/q47mCdOpwBwJaGVm7Ysp/2eO5kjLqEAcDXhlZuyNKf8" +
            "jhXMD5dRALiDEMt1X57dv38lc8OlFADuIsxyXZdo1++uwLxwOQWAOwm1XLdl2u17KzEn3EIB4G7CLddlqXb5zorMB7dRAFhByOWq" +
            "L9fq31eZueBWCgCrCLtc1SVb9bs6MA/cTgFgJaGXq7Zsq31PJ+aAJRQAVhN+uSpLt8p3dOT9s4wCQAVCMLd6+a7++5159yylAFCF" +
            "MMytWsKWf857ZzkFgEqEYu7uZWz557xzSlAAqEY45u5aypZ/zvumDAWAioRk7urlbPnnvGtKUQCoSljmrlrSln/Oe6YcBYDKhGbu" +
            "7GVt+ee8Y0pSAKhOeObOWtqWf877pSwFgA6EaO7o8rb8c94tpSkAdCFMc+kSt/xz3ivlKQB0IlRz7y5zyz/nndKCAkA3wjX36lK3" +
            "/HPeJ20oAHQkZHM/LXfLP+dd0ooCQFfCNvfdkrf8c94j7SgAdCZ0c5+XveWf8w5pSQGgO+Gb+/XpX97n/dGWx8sUlhh3k5+05v8A" +
            "MIUw5k7eG+0pAEwilLmDd8YICgDTCGeu5H0xhgLAREKaK3hXjKIAMJWw5kzeE+MoAEwmtDmDd8RICgDTCW+O8H4YSwFgB0KchHfD" +
            "aAoAuxDmvMN7YTwFgJ0IdV7hnbAFBYDdCHee8T7YhgLAjoQ8X/Eu2IoCwK6EPR95D2xHAWBnQp/HwztgUwoAuxP+e3P/bEsBAEtg" +
            "V+6drSkA8JtlsBf3zfYUAPiHpbAH9wwPBQA+sxxmc7/wPwoA/JslMZN7hQ8UAPiaZTGL+4RPFAD4nqUxg3uELygA8Jzl0Zv7g28o" +
            "APAzS6Qn9wZPKADwGsukF/cFP1AA4HWWSg/uCV6gAMB7LJfa3A+8SAGA91kyNbkXeIMCABnLphb3AW9SACBn6dTgHiCgAMAxls9a" +
            "zh9CCgAcZwmt4dzhAAUAzmEZ3ct5w0EKAJzHUrqHc4YTKABwLsvpWs4XTqIAwPksqWs4VziRAgDXsKzO5TzhZAoAXMfSOodzhAso" +
            "AHAty+sY5wcXUQDgWr9Wf0Bzzg8uogDAdSyvczhHuIACANewtM7lPOFkCgCcz7K6hnOFEykAcC5L6lrOF06iAMB5LKd7OGc4gQIA" +
            "57CU7uW84SAFAI6zjNZw7nCAAgDHWEJrOX8IKQCQs3xqcA8QUAAgY+nU4j7gTQoAvM+yqcm9wBsUAHiPJVOb+4EXKQDwOsulB/cE" +
            "L1AA4DWWSi/uC36gAMDPLJOe3Bs8oQDAc5ZIb+4PvqEAwPcsjxncI3xBAYCvWRqzuE/4RAGAf7MsZnKv8IECAH+yJGZzv/A/CgD8" +
            "w3LYg3uGhwIAf7MU9uK+2Z4CAJbBrtw7W1MA2J0lsDf3z7YUAHYm/Hk8vAM2pQCwK6HPR94D21EA2JGw5yveBVtRANiNkOcZ74Nt" +
            "KADsRLjzCu+ELSgA7EKo8w7vhfEUAHYgzEl4N4ymADCdEOcI74exFAAmE96cwTtiJAWAqYQ2Z/KeGEcBYCJhzRW8K0ZRAJhGSHMl" +
            "74sxFAAmEc7cwTtjBAWAKYQyd/LeaE8BYAJhnPtr9Qc05t3RmgJAd0I499enf3mf90dbCgCdCd/c56WvBOS8Q1pSAOhK6Oa+W/ZK" +
            "QM57pB0FgI6Ebe6nJa8E5LxLWlEA6EbI5l5d7kpAzvukDQWAToRr7t2lrgTkvFNaUADoQqjm0mWuBOS8V8pTAOhAmOaOLnElIOfd" +
            "UpoCQHVCNHfW8lYCct4vZSkAVCY8c2cvbSUg5x1TkgJAVUIzd9WyVgJy3jPlKABUJCxzVy9pJSDnXVOKAkA1QjJ313JWAnLeN2Uo" +
            "AFQiHHN3L2UlIOedU4ICQBVCMbdqGSsBOe+d5RQAKhCGudVLePXf78y7ZykFgNWEYK7K8q3yHR15/yyjALCS8MtVW7rVvqcTc8AS" +
            "CgCrCL1c1WVb9bs6MA/cTgFgBWGXq75kq39fZeaCWykA3E3I5bos1y7fWZH54DYKAHcSbrluS7Xb91ZiTriFAsBdhFqu6zLt+t0V" +
            "mBcupwBwB2GW675Eu3//SuaGSykAXE2I5aYszym/YwXzw2UUAK4kvHLTlua033Mnc8QlFACuIrRyU5fl1N91B/PE6RQAriCsctOX" +
            "5PTfdyVzxakUAM4mpHK7LMddfucVzBenUQA4k3DK7bYUd/u9ZzJnnEIB4CxCKbfrMtz1d5/BvHGYAsAZhFFu9yW4++8/wtxxiALA" +
            "UUIoZ/n95hxy5o+YAsARwidn6f3JeeTMIREFgJTQyVl2X3MuOfPI2xQAEsImZ8k953xy5pK3KAC8S8jkLLfXOKec+eRlCgDvEC45" +
            "S+09zitnTnmJAsCrhErOMss4t5x55UcKAK8QJjlL7BjnlzO3PKUA8BMhkrO8zuEcc+aXbykAPCM8cpbWuZxnzhzzJQWA7wiNnGV1" +
            "DeeaM8/8iwLAV4RFzpK6lvPNmWv+oADwmZDIWU73cM45883/KQB8JBxyltK9nHfOnPN4PBQA/iEUcpbRGs49Z95RAHg8HsLgCEto" +
            "LeefM/ebUwAQAjnLpwb3kDP/G1MA9mb4c5ZOLe4jJwc2pQDsy9DnLJua3EtOHmxIAdiTYc9ZMrW5n5xc2IwCsB9DnrNcenBPOfmw" +
            "EQVgL4Y7Z6n04r5ycmITCsA+DHXOMunJveXkxQYUgD0Y5pwl0pv7y8mN4RSA+QxxzvKYwT3m5MdgCsBshjdnacziPnNyZCgFYC5D" +
            "m7MsZnKvOXkykAIwk2HNWRKzud+cXBlGAZjHkOYshz2455x8GUQBmMVw5iyFvbjvnJwZQgGYw1DmLIM9ufecvBlAAZjBMOYsgb25" +
            "/5zcaU4B6M8Q5oQ/j4d3cIT8aUwB6M3w5YQ+H3kPOTnUlALQl6HLCXu+4l3k5FFDCkBPhi0n5HnG+8jJpWYUgH4MWU648wrvJCef" +
            "GlEAejFcOaHOO7yXnJxqQgHow1DlhDkJ7yYnrxpQAHowTDkhzhHeT05uFacA1GeIcsKbM3hHOflVmAJQm+HJCW3O5D3l5FhRCkBd" +
            "hiYnrLmCd5WTZwUpADUZlpyQ5kreV06uFaMA1GNIcsKZO3hnOflWiAJQi+HICWXu5L3l5FwRCkAdhiInjFnBu8vJuwIUgBoMQ04I" +
            "s5L3l5N7iykA6xmCnPClAu8wJ/8WUgDW8vhzQpdKvMecHFxEAVjHo88JWyryLnPycAEFYA2PPSdkqcz7zMnFmykAawiJjHOjA+80" +
            "pwTcSAFYR0i8x3nRifeacW43UgDW8thf45zoyLt9j/O6mQKwnkf/nPOhM+/3Nc5pAQWgBo//a86FCbzj55zPIgpAHYbgT86DSbzn" +
            "rzmXhRSAWgzDb86BibzrPzmPxRSAenYfit1/P7N53785hwIUgJp2HY5dfzd72f2d7/77y1AA6tptSHb7vext1/e+6+8uSQGobZdh" +
            "2eV3wke7vfvdfm95CkB904dm+u+DZ3Z5/7v8zlYUgB6mDs/U3wXvmD4H039fWwpAH9OGaNrvgSOmzsPU3zWCAtDLlGGa8jvgTNPm" +
            "YtrvGUcB6Kf7UHX/frjSlPmY8jtGUwB66jpcXb8b7tR9Trp//zYUgL66DVm374WVus5L1+/ekgLQW5dh6/KdUEm3uen2vdtTAPqr" +
            "PnTVvw8q6zI/Xb6TDxSAGaoOX9Xvgk6qz1H17+MbCsAc1Yaw2vdAZ1Xnqep38QIFYJYqw1jlO2CSanNV7Xt4kwIwz+qhXP33YbIq" +
            "81XlOzhAAZhp1XAKBbje6jlb/fc5iQIw191DKhTgPko+hykAs901rEIB7qfkc4gCMN/VQysUYB0ln5gCsIerhlcowHpKPhEFYB9n" +
            "D7FQgDqUfN6mAOzlrGEWClCPks9bFID9HB1qoQB1Kfm8TAHYUzrcQgHqU/J5iQKwr3eHXChAH0o+P1IA9vbqsAsF6EfJ5ykFgJ+G" +
            "XihAX0o+31IAeDy+H36hAP0p+XxJAeBvn0NAKMAcSj7wo1+rPwC4zK8P/wEAG7H8AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAgM7+C00hSJwYqkBDAAAAAElFTkSuQmCC";

        /// <summary>
        /// Construit une ImageSource à partir d'une image en base64.
        /// (Même principe que EmbeddedEyeImage dans PasswordRevealCustom.)
        /// </summary>
        private static Media.ImageSource EmbeddedIcon(string base64)
        {
            byte[] bytes = System.Convert.FromBase64String(base64);
            using System.IO.MemoryStream stream = new(bytes);
            Media.Imaging.BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = Media.Imaging.BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        #endregion

        #region Constructeur

        public SearchBoxCustom()
        {
            // Mêmes défauts classiques que les mots de passe (fond blanc,
            // bordure grise, taille de base) : un TextBox n'a pas de
            // Background/BorderBrush par défaut quand on remplace son
            // template. Posés avec SetCurrentValue : un Style ou une
            // valeur en XAML reprend le dessus normalement.
            SetCurrentValue(BackgroundProperty, Media.Brushes.White);
            SetCurrentValue(BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            SetCurrentValue(BorderThicknessProperty, new Thickness(1));
            SetCurrentValue(MinWidthProperty, 200.0);
            SetCurrentValue(MinHeightProperty, 32.0);
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBoxCustom box)
            {
                box.RefreshNeon();
            }
        }

        private static void OnSearchIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBoxCustom box)
            {
                box.RefreshSearchIcon();
            }
        }

        private static void OnClearIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBoxCustom box)
            {
                box.RefreshClearIcon();
            }
        }

        private static void OnIconColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBoxCustom box)
            {
                box.RefreshSearchIcon();
            }
        }

        private static void OnClearIconColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBoxCustom box)
            {
                box.RefreshClearIcon();
            }
        }

        private void SearchImage_MouseEnter(object sender, MouseEventArgs e)
        {
            _isSearchIconHovered = true;
            RefreshSearchIcon();
        }

        private void SearchImage_MouseLeave(object sender, MouseEventArgs e)
        {
            _isSearchIconHovered = false;
            RefreshSearchIcon();
        }

        private void ClearButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _isClearIconHovered = true;
            RefreshClearIcon();
        }

        private void ClearButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _isClearIconHovered = false;
            RefreshClearIcon();
        }

        #endregion

        #region Néon (lueur sur la bordure, texte net)

        // La lueur est appliquée sur la bordure du champ ("glowBorder"),
        // PAS sur le contrôle entier : le texte reste net (sinon ClearType
        // désactivé + flou).
        private Border? _glowBorder;

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Icônes intégrées (comme les yeux)

        /// <summary>
        /// Recolore une icône : les pixels opaques prennent la couleur,
        /// l'alpha est gardé (la forme et les bords adoucis restent
        /// identiques). Ne marche qu'avec une couleur pleine
        /// (SolidColorBrush) ; sinon l'image d'origine est gardée.
        /// Une image qui n'est pas un bitmap (ex. DrawingImage) est
        /// affichée telle quelle (non teintée).
        /// (Même principe que l'œil teinté dans PasswordRevealCustom.)
        /// </summary>
        private static Media.ImageSource? TintedIconImage(Media.ImageSource? source, Brush? brush)
        {
            if (source == null) return null;
            if (source is not Media.Imaging.BitmapSource bitmap) return source;
            if (brush is not SolidColorBrush solid) return bitmap;

            Media.Imaging.FormatConvertedBitmap converted =
                new(bitmap, Media.PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            Media.Color tint = solid.Color;
            for (int n = 0; n < pixels.Length; n += 4)
            {
                if (pixels[n + 3] > 0)
                {
                    pixels[n] = tint.B;
                    pixels[n + 1] = tint.G;
                    pixels[n + 2] = tint.R;
                }
            }

            Media.Imaging.BitmapSource tinted = Media.Imaging.BitmapSource.Create(
                width, height, converted.DpiX, converted.DpiY,
                Media.PixelFormats.Bgra32, null, pixels, stride);
            tinted.Freeze();
            return tinted;
        }

        /// <summary>
        /// Loupe : l'image (intégrée par défaut, ou ton image via
        /// SearchIcon), teintée par IconColor (noire si non définie).
        /// </summary>
        private void RefreshSearchIcon()
        {
            if (_searchImage == null) return;
            // Au survol, IconHoverColor gagne sur IconColor (comme EyeHoverColor).
            Brush? couleur = _isSearchIconHovered && IconHoverColor != null ? IconHoverColor : IconColor;
            _searchImage.Source = TintedIconImage(SearchIcon, couleur);
            _searchImage.Visibility = SearchIcon == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// X : l'image (intégrée par défaut, ou ton image via ClearIcon),
        /// teintée par ClearIconColor (noire si non définie).
        /// </summary>
        private void RefreshClearIcon()
        {
            if (_clearImage == null) return;
            // Au survol, ClearIconHoverColor gagne sur ClearIconColor.
            Brush? couleur = _isClearIconHovered && ClearIconHoverColor != null ? ClearIconHoverColor : ClearIconColor;
            _clearImage.Source = TintedIconImage(ClearIcon, couleur);
            _clearImage.Visibility = ClearIcon == null ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Template et événements

        /// <summary>
        /// Recherche lancée (touche Entrée). Le texte courant est dans Text.
        /// </summary>
        public static readonly RoutedEvent SearchEvent = EventManager.RegisterRoutedEvent(
            nameof(Search), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBoxCustom));

        public event RoutedEventHandler Search
        {
            add => AddHandler(SearchEvent, value);
            remove => RemoveHandler(SearchEvent, value);
        }

        /// <summary>
        /// Événement levé quand on clique sur la loupe (à gauche).
        /// Sa propre action, séparée de Search (Entrée) : l'utilisateur
        /// y attache ce qu'il veut (ex. : lancer la recherche).
        /// </summary>
        public static readonly RoutedEvent SearchIconClickedEvent = EventManager.RegisterRoutedEvent(
            nameof(SearchIconClicked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBoxCustom));

        public event RoutedEventHandler SearchIconClicked
        {
            add => AddHandler(SearchIconClickedEvent, value);
            remove => RemoveHandler(SearchIconClickedEvent, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué).
            if (_clearButton != null) _clearButton.Click -= ClearButton_Click;
            if (_searchImage != null) { _searchImage.MouseEnter -= SearchImage_MouseEnter; _searchImage.MouseLeave -= SearchImage_MouseLeave; }
            if (_searchImage != null) _searchImage.MouseLeftButtonUp -= SearchImage_MouseLeftButtonUp;
            if (_clearButton != null) { _clearButton.MouseEnter -= ClearButton_MouseEnter; _clearButton.MouseLeave -= ClearButton_MouseLeave; }

            _clearButton = Template.FindName("clearButton", this) as Controls.Button;
            _glowBorder = Template.FindName("glowBorder", this) as Border;
            _searchImage = Template.FindName("searchImage", this) as Controls.Image;
            _clearImage = Template.FindName("clearImage", this) as Controls.Image;

            if (_clearButton != null) _clearButton.Click += ClearButton_Click;
            if (_searchImage != null) { _searchImage.MouseEnter += SearchImage_MouseEnter; _searchImage.MouseLeave += SearchImage_MouseLeave; }
            if (_searchImage != null)
            {
                _searchImage.MouseLeftButtonUp += SearchImage_MouseLeftButtonUp;
                // La loupe est cliquable : on le montre avec la main.
                _searchImage.Cursor = System.Windows.Input.Cursors.Hand;
            }
            if (_clearButton != null) { _clearButton.MouseEnter += ClearButton_MouseEnter; _clearButton.MouseLeave += ClearButton_MouseLeave; }

            RefreshSearchIcon();
            RefreshClearIcon();
            RefreshNeon();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Text = string.Empty;
            Focus();
        }

        // Clic sur la loupe : l'utilisateur y attache sa propre action
        // (ex. : lancer la recherche), comme un Click de bouton.
        private void SearchImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SearchIconClickedEvent, this));
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                RaiseEvent(new RoutedEventArgs(SearchEvent, this));
            }
        }

        /// <summary>
        /// Largeur désirée : si le conteneur ne donne pas de largeur
        /// (Canvas, colonne Auto, StackPanel...), la largeur désirée
        /// serait celle du texte et le champ grandirait à chaque lettre
        /// tapée. On plafonne à MinWidth (200 par défaut) : le champ
        /// garde sa taille et le texte défile dedans, comme un TextBox
        /// classique dans une grille.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            if (double.IsInfinity(availableSize.Width))
            {
                size.Width = MinWidth;
            }
            return size;
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Grille racine : DEUX couches superposées.
            // - "glowBorder" : le fond + la bordure du champ (c'est elle
            //   qui porte la lueur néon).
            // - "grid" : le contenu (loupe + texte + X), SANS effet
            //   -> reste net et lisible.
            FrameworkElementFactory rootGrid = new(typeof(Controls.Grid));
            rootGrid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            // Grille de contenu : [loupe | texte | X].
            // Marge = le Padding du contrôle (rien ne touche les coins arrondis).
            FrameworkElementFactory grid = new(typeof(Controls.Grid));
            grid.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            FrameworkElementFactory colIcon = new(typeof(ColumnDefinition));
            colIcon.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            FrameworkElementFactory colText = new(typeof(ColumnDefinition));
            colText.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory colClear = new(typeof(ColumnDefinition));
            colClear.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(colIcon);
            grid.AppendChild(colText);
            grid.AppendChild(colClear);

            // Loupe : l'image intégrée en base64 (noire, 512x512, teintée
            // en code par IconColor — voir RefreshSearchIcon). Rendu net :
            // HighQuality, comme l'œil.
            FrameworkElementFactory searchImage = new(typeof(Controls.Image));
            searchImage.Name = "searchImage";
            searchImage.SetValue(Controls.Grid.ColumnProperty, 0);
            searchImage.SetValue(FrameworkElement.WidthProperty, 16.0);
            searchImage.SetValue(FrameworkElement.HeightProperty, 16.0);
            searchImage.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            searchImage.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 0, 6, 0));
            searchImage.SetValue(Controls.Image.StretchProperty, Stretch.Uniform);
            searchImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            // Texte d'aide : visible seulement quand le champ est vide
            // (trigger sur Text = ""). Ne capte pas les clics : on peut
            // cliquer "à travers" pour mettre le focus dans le champ.
            FrameworkElementFactory watermark = new(typeof(Controls.TextBlock));
            watermark.Name = "watermark";
            watermark.SetValue(Controls.Grid.ColumnProperty, 1);
            watermark.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            watermark.SetValue(UIElement.IsHitTestVisibleProperty, false);
            watermark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            // Toujours un peu transparente (0,6), comme un vrai placeholder :
            // la couleur se choisit avec WatermarkColor, la transparence est fixe.
            watermark.SetValue(UIElement.OpacityProperty, 0.6);
            watermark.SetBinding(
                Controls.TextBlock.TextProperty,
                new WPF.Data.Binding(nameof(Watermark)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(WatermarkColor)) { RelativeSource = templatedParent });
            watermark.SetBinding(
                Controls.TextBlock.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            // Aligné avec le curseur : le texte est décalé d'un Padding vers
            // la droite (le champ applique son Padding deux fois : une fois
            // par la marge du modèle, une fois en interne), alors que le
            // watermark n'en a qu'un. On lui ajoute donc un Padding à gauche
            // égal au Padding du champ, sinon le curseur tombe au milieu de
            // la première lettre du texte d'aide.
            watermark.SetBinding(
                Controls.TextBlock.PaddingProperty,
                new WPF.Data.Binding("Padding.Left")
                {
                    RelativeSource = templatedParent,
                    Converter = new DoubleToLeftMarginConverter()
                });

            // PART_ContentHost : partie obligatoire de TextBoxBase.
            // C'est ce ScrollViewer qui affiche et édite le texte.
            FrameworkElementFactory scroll = new(typeof(Controls.ScrollViewer));
            scroll.Name = "PART_ContentHost";
            scroll.SetValue(Controls.Grid.ColumnProperty, 1);
            scroll.SetValue(FrameworkElement.FocusableProperty, false);

            // Bouton effacer : un X, sans focus (le focus reste dans le champ).
            FrameworkElementFactory clearButton = new(typeof(Controls.Button));
            clearButton.Name = "clearButton";
            clearButton.SetValue(Controls.Grid.ColumnProperty, 2);
            clearButton.SetValue(FrameworkElement.FocusableProperty, false);
            clearButton.SetValue(FrameworkElement.WidthProperty, 28.0);
            clearButton.SetValue(Controls.Control.TemplateProperty, BuildClearButtonTemplate());
            // Contenu du bouton : l'image du X intégrée en base64 (noire,
            // 512x512, teintée en code par ClearIconColor — voir
            // RefreshClearIcon). Rendu net : HighQuality, comme l'œil.
            FrameworkElementFactory clearImage = new(typeof(Controls.Image));
            clearImage.Name = "clearImage";
            clearImage.SetValue(FrameworkElement.WidthProperty, 14.0);
            clearImage.SetValue(FrameworkElement.HeightProperty, 14.0);
            clearImage.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            clearImage.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            clearImage.SetValue(Controls.Image.StretchProperty, Stretch.Uniform);
            clearImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            clearButton.AppendChild(clearImage);

            grid.AppendChild(searchImage);
            grid.AppendChild(watermark);
            grid.AppendChild(scroll);
            grid.AppendChild(clearButton);
            rootGrid.AppendChild(border);
            rootGrid.AppendChild(grid);

            Controls.ControlTemplate template = new(typeof(SearchBoxCustom));
            template.VisualTree = rootGrid;

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Champ vide : le X disparaît, le texte d'aide apparaît.
            Trigger emptyTrigger = new()
            {
                Property = TextProperty,
                Value = ""
            };
            emptyTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "clearButton",
                Value = Visibility.Collapsed
            });
            emptyTrigger.Setters.Add(new Setter
            {
                Property = UIElement.VisibilityProperty,
                TargetName = "watermark",
                Value = Visibility.Visible
            });
            template.Triggers.Add(emptyTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Template du bouton effacer : fond transparent, léger voile
        /// blanc au survol (comme le bouton œil). Le X est le Contenu
        /// du bouton, affiché par le ContentPresenter.
        /// </summary>
        private static Controls.ControlTemplate BuildClearButtonTemplate()
        {
            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "clearButtonBorder";
            border.SetValue(Controls.Border.BackgroundProperty, Media.Brushes.Transparent);

            // NOTE : "Content" en chaîne, pas nameof(Content) : la classe
            // SearchBoxCustom n'a pas de propriété Content (c'est le Button,
            // le TemplatedParent de ce template, qui l'a).
            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content")
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(Controls.Button));
            template.VisualTree = border;

            // Survol : léger voile gris (visible sur fond clair : le champ est blanc par défaut).
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "clearButtonBorder",
                Value = new SolidColorBrush(Color.FromArgb(0x1F, 0x00, 0x00, 0x00))
            });
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region NumericUpDownCustom

    /// <summary>
    /// Champ nombre avec boutons + / − (maintenir le bouton = répétition)
    /// et molette de la souris. Value est un double (avec Increment = 1
    /// par défaut, il se comporte comme un entier). Le texte est validé
    /// à la sortie du champ ou sur Entrée : un texte non numérique
    /// restaure la dernière valeur. Les flèches Haut/Bas du clavier
    /// incrémentent / décrémentent aussi.
    /// </summary>
    public class NumericUpDownCustom : Control
    {
        #region Dependency Properties

        /// <summary>
        /// Valeur courante. Bornée entre Minimum et Maximum.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(0.0, OnValueChanged));

        /// <summary>
        /// Valeur minimale. Défaut : 0.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(0.0, OnRangeChanged));

        /// <summary>
        /// Valeur maximale. Défaut : 100.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(100.0, OnRangeChanged));

        /// <summary>
        /// Pas des boutons + / −, de la molette et des flèches. Défaut : 1.
        /// </summary>
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(
                nameof(Increment),
                typeof(double),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Format d'affichage de la valeur (ex. : "0.00" pour 2 décimales,
        /// "C2" pour un prix, "0' %'" pour un pourcentage). Null = nombre brut.
        /// C'est de l'affichage seulement : quand on tape, on tape le nombre
        /// brut (ex. : 9,99, pas 9,99 €).
        /// </summary>
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(
                nameof(Format),
                typeof(string),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(null, OnFormatChanged));

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDownCustom control) control.SyncTextFromValue();
        }

        /// <summary>
        /// Coins arrondis du champ. 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(NumericUpDownCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public string? Format
        {
            get => (string?)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Controls.TextBox? _valueBox;
        private RepeatButton? _upButton;
        private RepeatButton? _downButton;

        // Bordure du champ, pour la lueur néon (voir RefreshNeon).
        private Border? _glowBorder;

        #endregion

        #region Constructeur

        public NumericUpDownCustom()
        {
            // Mêmes défauts classiques que les mots de passe (fond blanc,
            // bordure grise, taille de base). Posés avec SetCurrentValue :
            // un Style ou une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(BackgroundProperty, Media.Brushes.White);
            SetCurrentValue(BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            SetCurrentValue(BorderThicknessProperty, new Thickness(1));
            SetCurrentValue(MinWidthProperty, 200.0);
            SetCurrentValue(MinHeightProperty, 32.0);
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDownCustom c)
            {
                c.ClampValue();
                c.SyncTextFromValue();
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDownCustom c)
            {
                c.ClampValue();
            }
        }

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDownCustom c)
            {
                c.RefreshNeon();
            }
        }

        #endregion

        #region Synchronisation

        // Borne une valeur entre Minimum et Maximum (si les bornes sont
        // inversées, on ne borne pas).
        private double Clamp(double value)
        {
            return (Minimum <= Maximum) ? Math.Clamp(value, Minimum, Maximum) : value;
        }

        private void ClampValue()
        {
            double clamped = Clamp(Value);
            if (clamped != Value)
            {
                Value = clamped; // redéclenche OnValueChanged avec la valeur bornée
            }
        }

        private void SyncTextFromValue()
        {
            if (_valueBox == null) return;
            _valueBox.Text = Format != null
                ? Value.ToString(Format, CultureInfo.CurrentCulture)
                : Value.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Valide le texte tapé : nombre accepté (borné), texte invalide
        /// = retour à la dernière valeur.
        /// </summary>
        private void CommitText()
        {
            if (_valueBox == null) return;
            if (double.TryParse(_valueBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed))
            {
                Value = Clamp(parsed);
            }
            SyncTextFromValue();
        }

        #endregion

        #region Néon (lueur sur la bordure, texte net)

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template et focus

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué).
            if (_valueBox != null)
            {
                _valueBox.LostFocus -= ValueBox_LostFocus;
                _valueBox.KeyDown -= ValueBox_KeyDown;
            }
            if (_upButton != null) _upButton.Click -= UpButton_Click;
            if (_downButton != null) _downButton.Click -= DownButton_Click;

            _valueBox = Template.FindName("valueBox", this) as Controls.TextBox;
            _upButton = Template.FindName("upButton", this) as RepeatButton;
            _downButton = Template.FindName("downButton", this) as RepeatButton;
            _glowBorder = Template.FindName("glowBorder", this) as Border;

            if (_valueBox != null)
            {
                _valueBox.LostFocus += ValueBox_LostFocus;
                _valueBox.KeyDown += ValueBox_KeyDown;
            }
            if (_upButton != null) _upButton.Click += UpButton_Click;
            if (_downButton != null) _downButton.Click += DownButton_Click;

            // État initial : la valeur posée en XAML/code avant le
            // chargement doit remplir le champ.
            SyncTextFromValue();
            RefreshNeon();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Clamp(Value + Increment);
            FocusValueBox();
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Clamp(Value - Increment);
            FocusValueBox();
        }

        private void ValueBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitText();
        }

        private void ValueBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitText();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                Value = Clamp(Value + Increment);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Value = Clamp(Value - Increment);
                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Value = Clamp(Value + (e.Delta > 0 ? Increment : -Increment));
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            // Tabulation : le focus arrive sur le contrôle lui-même
            // (le champ interne n'est pas un arrêt Tab) — on le
            // transfère au champ pour pouvoir taper tout de suite.
            FocusValueBox();
        }

        private void FocusValueBox()
        {
            if (_valueBox != null && !_valueBox.IsKeyboardFocused)
            {
                _valueBox.Focus();
            }
        }

        /// <summary>
        /// Largeur désirée : si le conteneur ne donne pas de largeur
        /// (Canvas, colonne Auto, StackPanel...), la largeur désirée
        /// serait celle du texte et le champ grandirait à chaque chiffre
        /// tapé. On plafonne à MinWidth (200 par défaut) : le champ
        /// garde sa taille et le texte défile dedans, comme un TextBox
        /// classique dans une grille.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            if (double.IsInfinity(availableSize.Width))
            {
                size.Width = MinWidth;
            }
            return size;
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Grille racine : DEUX couches superposées.
            // - "glowBorder" : le fond + la bordure du champ (c'est elle
            //   qui porte la lueur néon).
            // - "grid" : le contenu (champ + boutons), SANS effet
            //   -> reste net et lisible.
            FrameworkElementFactory rootGrid = new(typeof(Controls.Grid));
            rootGrid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            // Grille de contenu : [champ | boutons].
            // Marge = le Padding du contrôle (rien ne touche les coins arrondis).
            FrameworkElementFactory grid = new(typeof(Controls.Grid));
            grid.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            FrameworkElementFactory colText = new(typeof(ColumnDefinition));
            colText.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory colButtons = new(typeof(ColumnDefinition));
            colButtons.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(colText);
            grid.AppendChild(colButtons);

            // Champ nombre (vrai TextBox, transparent, sans bordure).
            // Le texte n'est PAS bindé : c'est le code qui le pose
            // (SyncTextFromValue) et le valide (CommitText).
            FrameworkElementFactory valueBox = new(typeof(Controls.TextBox));
            valueBox.Name = "valueBox";
            valueBox.SetValue(Controls.Grid.ColumnProperty, 0);
            valueBox.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            valueBox.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            valueBox.SetValue(Controls.Control.PaddingProperty, new Thickness(0));
            valueBox.SetValue(KeyboardNavigation.IsTabStopProperty, false);
            valueBox.SetBinding(
                Controls.Control.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.FontFamilyProperty,
                new WPF.Data.Binding(nameof(FontFamily)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.FontSizeProperty,
                new WPF.Data.Binding(nameof(FontSize)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.FontStyleProperty,
                new WPF.Data.Binding(nameof(FontStyle)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.FontWeightProperty,
                new WPF.Data.Binding(nameof(FontWeight)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.FontStretchProperty,
                new WPF.Data.Binding(nameof(FontStretch)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.VerticalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(VerticalContentAlignment)) { RelativeSource = templatedParent });
            valueBox.SetBinding(
                Controls.Control.HorizontalContentAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });

            // Colonne des boutons : + en haut, − en bas.
            FrameworkElementFactory buttonsGrid = new(typeof(Controls.Grid));
            buttonsGrid.SetValue(Controls.Grid.ColumnProperty, 1);
            buttonsGrid.SetValue(FrameworkElement.WidthProperty, 28.0);

            FrameworkElementFactory rowUp = new(typeof(RowDefinition));
            rowUp.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory rowDown = new(typeof(RowDefinition));
            rowDown.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            buttonsGrid.AppendChild(rowUp);
            buttonsGrid.AppendChild(rowDown);

            // RepeatButton : maintenir le bouton répète le clic.
            FrameworkElementFactory upButton = new(typeof(RepeatButton));
            upButton.Name = "upButton";
            upButton.SetValue(Controls.Grid.RowProperty, 0);
            upButton.SetValue(FrameworkElement.FocusableProperty, false);
            upButton.SetValue(Controls.Control.TemplateProperty, BuildStepButtonTemplate());
            FrameworkElementFactory upText = new(typeof(Controls.TextBlock));
            upText.SetValue(Controls.TextBlock.TextProperty, "▲");
            upText.SetValue(Controls.TextBlock.FontSizeProperty, 9.0);
            upText.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            upText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            upText.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            upButton.AppendChild(upText);

            FrameworkElementFactory downButton = new(typeof(RepeatButton));
            downButton.Name = "downButton";
            downButton.SetValue(Controls.Grid.RowProperty, 1);
            downButton.SetValue(FrameworkElement.FocusableProperty, false);
            downButton.SetValue(Controls.Control.TemplateProperty, BuildStepButtonTemplate());
            FrameworkElementFactory downText = new(typeof(Controls.TextBlock));
            downText.SetValue(Controls.TextBlock.TextProperty, "▼");
            downText.SetValue(Controls.TextBlock.FontSizeProperty, 9.0);
            downText.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            downText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            downText.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            downButton.AppendChild(downText);

            buttonsGrid.AppendChild(upButton);
            buttonsGrid.AppendChild(downButton);

            grid.AppendChild(valueBox);
            grid.AppendChild(buttonsGrid);
            rootGrid.AppendChild(border);
            rootGrid.AppendChild(grid);

            Controls.ControlTemplate template = new(typeof(NumericUpDownCustom));
            template.VisualTree = rootGrid;

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Template des boutons + / − : fond transparent, léger voile
        /// blanc au survol (comme le bouton œil). La flèche est le
        /// Contenu du bouton, affichée par le ContentPresenter.
        /// </summary>
        private static Controls.ControlTemplate BuildStepButtonTemplate()
        {
            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "stepButtonBorder";
            border.SetValue(Controls.Border.BackgroundProperty, Media.Brushes.Transparent);

            // NOTE : "Content" en chaîne, pas nameof(Content) : la classe
            // NumericUpDownCustom n'a pas de propriété Content (c'est le
            // RepeatButton, le TemplatedParent de ce template, qui l'a).
            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content")
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(RepeatButton));
            template.VisualTree = border;

            // Survol : léger voile gris (visible sur fond clair : le champ est blanc par défaut).
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "stepButtonBorder",
                Value = new SolidColorBrush(Color.FromArgb(0x1F, 0x00, 0x00, 0x00))
            });
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        #endregion
    }

    #endregion

    #region ComboBoxCustom

    /// <summary>
    /// Liste déroulante dans le style de la librairie : un champ qui montre
    /// la valeur choisie (ou le Placeholder si rien n'est choisi), une
    /// flèche ▼, et une liste qui se déroule en dessous (Popup).
    /// ItemsSource = la liste, SelectedItem = l'élément choisi (bindable),
    /// DisplayMemberPath = la propriété à afficher (sinon ToString()).
    /// La liste se ferme sur un clic sur un élément, sur Entrée/Échap,
    /// ou sur un clic ailleurs (Popup StaysOpen=false).
    /// </summary>
    public class ComboBoxCustom : Control
    {
        #region Dependency Properties

        /// <summary>
        /// La liste des éléments à proposer.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(System.Collections.IEnumerable),
                typeof(ComboBoxCustom),
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// L'élément choisi (null = rien). Bindable dans les deux sens.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(ComboBoxCustom),
                new PropertyMetadata(null, OnSelectedItemChanged));

        /// <summary>
        /// Couleur de fond de l'élément choisi dans la liste déroulante.
        /// Défaut : cyan (comme ListBoxCustom), lisible sur fond blanc.
        /// </summary>
        public static readonly DependencyProperty SelectedItemBackgroundProperty =
            DependencyProperty.Register(
                nameof(SelectedItemBackground),
                typeof(Brush),
                typeof(ComboBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        /// <summary>
        /// Couleur du texte de l'élément choisi dans la liste déroulante.
        /// Défaut : noir (lisible sur le fond cyan).
        /// </summary>
        public static readonly DependencyProperty SelectedItemForegroundProperty =
            DependencyProperty.Register(
                nameof(SelectedItemForeground),
                typeof(Brush),
                typeof(ComboBoxCustom),
                new PropertyMetadata(Media.Brushes.Black));

        /// <summary>
        /// Couleur de la bordure de l'élément choisi dans la liste déroulante.
        /// Défaut : cyan (comme ListBoxCustom).
        /// </summary>
        public static readonly DependencyProperty SelectedItemBorderBrushProperty =
            DependencyProperty.Register(
                nameof(SelectedItemBorderBrush),
                typeof(Brush),
                typeof(ComboBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF00D1FF"))));

        /// <summary>
        /// Nom de la propriété à afficher pour chaque élément
        /// (ex. "Nom"). null (défaut) = ToString() de l'élément.
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(
                nameof(DisplayMemberPath),
                typeof(string),
                typeof(ComboBoxCustom),
                new PropertyMetadata(null, OnDisplayMemberPathChanged));

        /// <summary>
        /// La liste est-elle déroulée ? (lecture/écriture en code/XAML).
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(ComboBoxCustom),
                new PropertyMetadata(false));

        /// <summary>
        /// Texte affiché quand rien n'est choisi (ex. "Choisir...").
        /// </summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(ComboBoxCustom),
                new PropertyMetadata("Choisir..."));

        /// <summary>
        /// Couleur du Placeholder. Défaut : gris #FF808080.
        /// </summary>
        public static readonly DependencyProperty PlaceholderColorProperty =
            DependencyProperty.Register(
                nameof(PlaceholderColor),
                typeof(Brush),
                typeof(ComboBoxCustom),
                new PropertyMetadata(new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF808080"))));

        /// <summary>
        /// Hauteur max de la liste déroulée (au-delà, elle défile).
        /// Défaut : 200.
        /// </summary>
        public static readonly DependencyProperty MaxDropDownHeightProperty =
            DependencyProperty.Register(
                nameof(MaxDropDownHeight),
                typeof(double),
                typeof(ComboBoxCustom),
                new PropertyMetadata(200.0));

        /// <summary>
        /// Coins arrondis du champ (et de la liste). 0 = rectangle vif.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ComboBoxCustom),
                new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Couleur de la bordure quand le champ a le focus (clavier).
        /// null (défaut) = garde BorderBrush.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusedBorderBrush),
                typeof(Brush),
                typeof(ComboBoxCustom),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NeonProperty =
            DependencyProperty.Register(
                nameof(Neon),
                typeof(bool),
                typeof(ComboBoxCustom),
                new PropertyMetadata(false, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonRadiusProperty =
            DependencyProperty.Register(
                nameof(NeonRadius),
                typeof(double),
                typeof(ComboBoxCustom),
                new PropertyMetadata(20.0, OnNeonPropertyChanged));

        public static readonly DependencyProperty NeonColorProperty =
            DependencyProperty.Register(
                nameof(NeonColor),
                typeof(Media.Color),
                typeof(ComboBoxCustom),
                new PropertyMetadata(Media.Colors.Cyan, OnNeonPropertyChanged));
        #endregion

        #region Propriétés

        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public Brush SelectedItemBackground
        {
            get => (Brush)GetValue(SelectedItemBackgroundProperty);
            set => SetValue(SelectedItemBackgroundProperty, value);
        }

        public Brush SelectedItemForeground
        {
            get => (Brush)GetValue(SelectedItemForegroundProperty);
            set => SetValue(SelectedItemForegroundProperty, value);
        }

        public Brush SelectedItemBorderBrush
        {
            get => (Brush)GetValue(SelectedItemBorderBrushProperty);
            set => SetValue(SelectedItemBorderBrushProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Brush PlaceholderColor
        {
            get => (Brush)GetValue(PlaceholderColorProperty);
            set => SetValue(PlaceholderColorProperty, value);
        }

        public double MaxDropDownHeight
        {
            get => (double)GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusedBorderBrush
        {
            get => (Brush)GetValue(FocusedBorderBrushProperty);
            set => SetValue(FocusedBorderBrushProperty, value);
        }

        public bool Neon
        {
            get => (bool)GetValue(NeonProperty);
            set => SetValue(NeonProperty, value);
        }

        public Media.Color NeonColor
        {
            get => (Media.Color)GetValue(NeonColorProperty);
            set => SetValue(NeonColorProperty, value);
        }

        public double NeonRadius
        {
            get => (double)GetValue(NeonRadiusProperty);
            set => SetValue(NeonRadiusProperty, value);
        }

        #endregion

        #region Champs

        private Controls.Button? _fieldButton;
        private Popup? _popup;
        private Controls.ListBox? _listBox;
        private Controls.TextBlock? _displayText;

        // Bordure du champ, pour la lueur néon (voir RefreshNeon).
        private Border? _glowBorder;

        // Dernière position du contrôle à l'écran (pour faire suivre la liste).
        private Point _dernierePosition;

        #endregion

        #region Constructeur

        public ComboBoxCustom()
        {
            // Mêmes défauts classiques que les autres champs (fond blanc,
            // bordure grise, taille de base). Posés avec SetCurrentValue :
            // un Style ou une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(BackgroundProperty, Media.Brushes.White);
            SetCurrentValue(BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            SetCurrentValue(BorderThicknessProperty, new Thickness(1));
            SetCurrentValue(MinWidthProperty, 200.0);
            SetCurrentValue(MinHeightProperty, 32.0);
            SetCurrentValue(PaddingProperty, new Thickness(8, 2, 8, 2));
            SetCurrentValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            // Texte noir par défaut : le fond du champ est blanc par défaut,
            // donc le texte (et le curseur, et les points du mot de passe)
            // doit être sombre, sinon il est invisible dans une fenêtre
            // sombre (Foreground clair hérité sur fond blanc). Un Style ou
            // une valeur en XAML reprend le dessus normalement.
            SetCurrentValue(ForegroundProperty, Media.Brushes.Black);

            Template = BuildTemplate();
            RefreshNeon(); // fallback sur le contrôle tant que le template n'est pas appliqué
        }

        #endregion

        #region Callbacks

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxCustom c)
            {
                c.RefreshDisplayText();
            }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxCustom c)
            {
                c.RefreshDisplayText();
            }
        }

        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxCustom c)
            {
                c.RefreshDisplayText();
            }
        }

        private static void OnNeonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBoxCustom c)
            {
                c.RefreshNeon();
            }
        }

        #endregion

        #region Affichage

        /// <summary>
        /// Texte à afficher pour un élément : la propriété DisplayMemberPath
        /// si elle est définie, sinon ToString().
        /// </summary>
        private string GetDisplayText(object? item)
        {
            if (item == null) return string.Empty;
            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                object? value = item.GetType().GetProperty(DisplayMemberPath)?.GetValue(item);
                return value?.ToString() ?? string.Empty;
            }
            return item.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Met à jour le texte du champ : le Placeholder (en gris) si rien
        /// n'est choisi, sinon le texte de l'élément choisi.
        /// </summary>
        private void RefreshDisplayText()
        {
            if (_displayText == null) return;
            if (SelectedItem == null)
            {
                _displayText.Text = Placeholder;
                _displayText.Foreground = PlaceholderColor;
            }
            else
            {
                _displayText.Text = GetDisplayText(SelectedItem);
                _displayText.Foreground = Foreground;
            }
        }

        #endregion

        #region Néon (lueur sur la bordure, texte net)

        private void RefreshNeon()
        {
            if (_glowBorder != null)
            {
                NeonHelper.ApplyEffect(_glowBorder, Neon, NeonColor, NeonRadius);
                Effect = null; // pas de double lueur : elle est sur la couche interne
            }
            else
            {
                // Template pas encore appliqué (constructeur) : fallback temporaire
                NeonHelper.ApplyEffect(this, Neon, NeonColor, NeonRadius);
            }
        }

        #endregion

        #region Template et événements

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // On se désabonne d'abord (sécurité si le template est réappliqué).
            if (_fieldButton != null) _fieldButton.Click -= FieldButton_Click;
            if (_popup != null) _popup.Opened -= Popup_Opened;
            if (_popup != null) _popup.Closed -= Popup_Closed;
            if (_listBox != null)
            {
                _listBox.PreviewMouseLeftButtonUp -= ListBox_PreviewMouseLeftButtonUp;
                _listBox.KeyDown -= ListBox_KeyDown;
            }

            _fieldButton = Template.FindName("fieldButton", this) as Controls.Button;
            _popup = Template.FindName("popup", this) as Popup;
            _listBox = Template.FindName("listBox", this) as Controls.ListBox;
            _displayText = Template.FindName("displayText", this) as Controls.TextBlock;
            _glowBorder = Template.FindName("glowBorder", this) as Border;

            if (_fieldButton != null) _fieldButton.Click += FieldButton_Click;
            if (_popup != null)
            {
                _popup.PlacementTarget = this;
                _popup.Opened += Popup_Opened;
                _popup.Closed += Popup_Closed;
            }
            if (_listBox != null)
            {
                _listBox.PreviewMouseLeftButtonUp += ListBox_PreviewMouseLeftButtonUp;
                _listBox.KeyDown += ListBox_KeyDown;
                // Style des lignes : couleurs de sélection lisibles (voir
                // BuildDropDownItemStyle) — posé ici, pas dans le template,
                // parce que les bindings ont besoin de ce ComboBox-ci.
                _listBox.ItemContainerStyle = BuildDropDownItemStyle();
            }

            RefreshDisplayText();
            RefreshNeon();
        }

        private void FieldButton_Click(object sender, RoutedEventArgs e)
        {
            IsDropDownOpen = !IsDropDownOpen;
        }

        // La liste vient de s'ouvrir : on donne le focus à la ListBox
        // pour que les flèches naviguent tout de suite.
        private void Popup_Opened(object? sender, EventArgs e)
        {
            _listBox?.Focus();

            // La liste doit suivre le champ quand on défile (un Popup ne le
            // fait pas tout seul : c'est une fenêtre à part). On surveille
            // la position tant que la liste est ouverte.
            _dernierePosition = PointToScreen(new Point(0, 0));
            LayoutUpdated += SuivreCible;
        }

        private void Popup_Closed(object? sender, EventArgs e)
        {
            LayoutUpdated -= SuivreCible;
        }

        private void SuivreCible(object? sender, EventArgs e)
        {
            if (_popup == null || !_popup.IsOpen) return;

            Point position = PointToScreen(new Point(0, 0));
            if (position != _dernierePosition)
            {
                _dernierePosition = position;
                // Force le Popup à se repositionner sous son champ : on
                // bouge le décalage de 1 px puis on le remet (sans clignotement).
                double decalage = _popup.HorizontalOffset;
                _popup.HorizontalOffset = decalage + 1;
                _popup.HorizontalOffset = decalage;
            }
        }

        // Style des lignes de la liste déroulante : même principe que
        // ListBoxCustom (Border + ContentPresenter + trigger de sélection),
        // mais les couleurs viennent de CE ComboBox-ci (Source = this) :
        // la liste est dans un Popup, un FindAncestor ne traverserait pas
        // sa frontière, et les couleurs par défaut de WPF (bleu clair du
        // système) ne sont pas lisibles sur fond blanc.
        private Style BuildDropDownItemStyle()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            Style style = new(typeof(Controls.ListBoxItem));
            style.Setters.Add(new Setter(Controls.ListBoxItem.BackgroundProperty,
                Media.Brushes.Transparent));
            style.Setters.Add(new Setter(Controls.ListBoxItem.BorderThicknessProperty,
                new Thickness(1)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.PaddingProperty,
                new Thickness(8, 6, 8, 6)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.MarginProperty,
                new Thickness(0, 0, 0, 4)));
            style.Setters.Add(new Setter(Controls.ListBoxItem.HorizontalContentAlignmentProperty,
                WPF.HorizontalAlignment.Stretch));

            Controls.ControlTemplate itemTemplate = new(typeof(Controls.ListBoxItem));

            FrameworkElementFactory itemBorder = new(typeof(Controls.Border));
            itemBorder.Name = "itemBorder";
            itemBorder.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            itemBorder.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            itemBorder.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            // Les lignes suivent le CornerRadius du ComboBox (comme le cadre
            // de la liste) : fini le rectangle dans le rectangle.
            itemBorder.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { Source = this });
            itemBorder.SetBinding(
                Controls.Border.PaddingProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.ContentTemplateProperty,
                new WPF.Data.Binding("ContentTemplate") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.ContentTemplateSelectorProperty,
                new WPF.Data.Binding("ContentTemplateSelector") { RelativeSource = templatedParent });
            presenter.SetBinding(
                Controls.ContentPresenter.HorizontalAlignmentProperty,
                new WPF.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = templatedParent });
            presenter.SetValue(
                Controls.ContentPresenter.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            itemBorder.AppendChild(presenter);
            itemTemplate.VisualTree = itemBorder;

            // Sélectionné : fond + bordure + texte pilotés par les 3
            // propriétés du ComboBox (cyan + texte noir par défaut).
            Trigger selectedTrigger = new()
            {
                Property = Controls.ListBoxItem.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "itemBorder",
                Value = new WPF.Data.Binding(nameof(SelectedItemBackground)) { Source = this }
            });
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "itemBorder",
                Value = new WPF.Data.Binding(nameof(SelectedItemBorderBrush)) { Source = this }
            });
            selectedTrigger.Setters.Add(new Setter
            {
                Property = Controls.Control.ForegroundProperty,
                Value = new WPF.Data.Binding(nameof(SelectedItemForeground)) { Source = this }
            });
            // Survolé : fond bleu très clair, texte inchangé (noir par défaut) : lisible.
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "itemBorder",
                Value = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFE5F1FB"))
            });
            itemTemplate.Triggers.Add(hoverTrigger);

            // La sélection gagne sur le survol : quand on survole un élément
            // choisi, c'est son fond (et son texte) qui restent — sinon un
            // texte blanc deviendrait illisible sur le fond clair du survol.
            itemTemplate.Triggers.Add(selectedTrigger);

            style.Setters.Add(new Setter(Controls.Control.TemplateProperty, itemTemplate));

            return style;
        }

        // Clic dans la liste : si c'est sur un élément, on ferme (la
        // sélection est déjà posée par le clic). Un clic dans le vide
        // (sous les éléments) ne ferme pas.
        private void ListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_listBox == null) return;
            DependencyObject? clicked = e.OriginalSource as DependencyObject;
            if (clicked != null && ItemsControl.ContainerFromElement(_listBox, clicked) != null)
            {
                IsDropDownOpen = false;
                Focus();
            }
        }

        // Entrée = valider la sélection, Échap = fermer sans changer.
        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                Focus();
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Space))
            {
                IsDropDownOpen = true;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Largeur désirée : si le conteneur ne donne pas de largeur
        /// (Canvas, colonne Auto, StackPanel...), la largeur désirée
        /// serait celle du texte choisi et le champ grandirait avec.
        /// On plafonne à MinWidth (200 par défaut) : le champ garde sa
        /// taille, comme un ComboBox classique dans une grille.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            if (double.IsInfinity(availableSize.Width))
            {
                size.Width = MinWidth;
            }
            return size;
        }

        #endregion

        #region Template

        private Controls.ControlTemplate BuildTemplate()
        {
            WPF.Data.RelativeSource templatedParent =
                new(WPF.Data.RelativeSourceMode.TemplatedParent);

            // Grille racine : DEUX couches superposées.
            // - "glowBorder" : le fond + la bordure du champ (c'est elle
            //   qui porte la lueur néon).
            // - "fieldButton" : le champ cliquable (texte + flèche),
            //   SANS effet -> reste net et lisible.
            // - "popup" : la liste déroulée (fenêtre flottante, ne prend
            //   pas de place dans la mise en page).
            FrameworkElementFactory rootGrid = new(typeof(Controls.Grid));
            rootGrid.Name = "fieldGrid";

            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "glowBorder";
            border.SetBinding(
                Controls.Border.BackgroundProperty,
                new WPF.Data.Binding(nameof(Background)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderBrushProperty,
                new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.BorderThicknessProperty,
                new WPF.Data.Binding(nameof(BorderThickness)) { RelativeSource = templatedParent });
            border.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            // Bouton du champ : transparent, il couvre tout le champ.
            // (Le focus reste sur lui : c'est l'arrêt Tab du contrôle.)
            FrameworkElementFactory fieldButton = new(typeof(Controls.Button));
            fieldButton.Name = "fieldButton";
            fieldButton.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            fieldButton.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            fieldButton.SetValue(Controls.Control.TemplateProperty, BuildFieldButtonTemplate());

            // Contenu du bouton : [texte choisi | flèche ▼].
            // Marge = le Padding du contrôle (rien ne touche les coins arrondis).
            FrameworkElementFactory contentGrid = new(typeof(Controls.Grid));
            contentGrid.SetBinding(
                FrameworkElement.MarginProperty,
                new WPF.Data.Binding(nameof(Padding)) { RelativeSource = templatedParent });

            FrameworkElementFactory colText = new(typeof(ColumnDefinition));
            colText.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory colArrow = new(typeof(ColumnDefinition));
            colArrow.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            contentGrid.AppendChild(colText);
            contentGrid.AppendChild(colArrow);

            // Le texte affiché est posé en code (RefreshDisplayText) :
            // Placeholder ou texte de l'élément choisi.
            FrameworkElementFactory displayText = new(typeof(Controls.TextBlock));
            displayText.Name = "displayText";
            displayText.SetValue(Controls.Grid.ColumnProperty, 0);
            displayText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            displayText.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });

            FrameworkElementFactory arrow = new(typeof(Controls.TextBlock));
            arrow.SetValue(Controls.Grid.ColumnProperty, 1);
            arrow.SetValue(Controls.TextBlock.TextProperty, "▼");
            arrow.SetValue(Controls.TextBlock.FontSizeProperty, 10.0);
            arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 2, 0));
            arrow.SetBinding(
                Controls.TextBlock.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });

            contentGrid.AppendChild(displayText);
            contentGrid.AppendChild(arrow);
            fieldButton.AppendChild(contentGrid);

            rootGrid.AppendChild(border);
            rootGrid.AppendChild(fieldButton);

            // La liste déroulée : une Popup (fenêtre flottante) avec un
            // ListBox dedans. StaysOpen=false : un clic ailleurs la ferme.
            // (Une Popup peut être enfant de la grille du template : elle
            // ne prend pas de place dans la mise en page, elle s'affiche
            // comme une fenêtre séparée.)
            FrameworkElementFactory popup = new(typeof(Popup));
            popup.Name = "popup";
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.VerticalOffsetProperty, 2.0);
            popup.SetValue(Popup.StaysOpenProperty, false);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetBinding(
                Popup.WidthProperty,
                new WPF.Data.Binding(nameof(ActualWidth)) { RelativeSource = templatedParent });
            popup.SetBinding(
                Popup.IsOpenProperty,
                new WPF.Data.Binding(nameof(IsDropDownOpen))
                {
                    RelativeSource = templatedParent,
                    Mode = WPF.Data.BindingMode.TwoWay
                });

            FrameworkElementFactory dropBorder = new(typeof(Controls.Border));
            dropBorder.SetValue(Controls.Border.BackgroundProperty, Media.Brushes.White);
            dropBorder.SetValue(Controls.Border.BorderBrushProperty, new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FFABADB3")));
            dropBorder.SetValue(Controls.Border.BorderThicknessProperty, new Thickness(1));
            dropBorder.SetBinding(
                Controls.Border.CornerRadiusProperty,
                new WPF.Data.Binding(nameof(CornerRadius)) { RelativeSource = templatedParent });

            FrameworkElementFactory listBox = new(typeof(Controls.ListBox));
            listBox.Name = "listBox";
            listBox.SetValue(Controls.Control.BackgroundProperty, Media.Brushes.Transparent);
            listBox.SetBinding(
                Controls.Control.ForegroundProperty,
                new WPF.Data.Binding(nameof(Foreground)) { RelativeSource = templatedParent });
            listBox.SetValue(Controls.Control.BorderThicknessProperty, new Thickness(0));
            listBox.SetBinding(
                Controls.ItemsControl.ItemsSourceProperty,
                new WPF.Data.Binding(nameof(ItemsSource)) { RelativeSource = templatedParent });
            listBox.SetBinding(
                Controls.ItemsControl.DisplayMemberPathProperty,
                new WPF.Data.Binding(nameof(DisplayMemberPath)) { RelativeSource = templatedParent });
            listBox.SetBinding(
                Selector.SelectedItemProperty,
                new WPF.Data.Binding(nameof(SelectedItem))
                {
                    RelativeSource = templatedParent,
                    Mode = WPF.Data.BindingMode.TwoWay
                });
            listBox.SetBinding(
                FrameworkElement.MaxHeightProperty,
                new WPF.Data.Binding(nameof(MaxDropDownHeight)) { RelativeSource = templatedParent });

            dropBorder.AppendChild(listBox);
            popup.AppendChild(dropBorder);
            rootGrid.AppendChild(popup);

            Controls.ControlTemplate template = new(typeof(ComboBoxCustom));
            template.VisualTree = rootGrid;

            // Focus clavier : bordure FocusedBorderBrush, ou BorderBrush si non définie.
            Trigger focusTrigger = new()
            {
                Property = UIElement.IsKeyboardFocusWithinProperty,
                Value = true
            };
            MultiBinding focusBinding = new() { Converter = new FirstNonNullBrushConverter() };
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            focusBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            focusTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = focusBinding
            });
            template.Triggers.Add(focusTrigger);

            // Liste déroulée : on garde la bordure de focus tant qu'elle
            // est ouverte (le focus est dans la Popup, donc
            // IsKeyboardFocusWithin ne le voit plus).
            Trigger openTrigger = new()
            {
                Property = IsDropDownOpenProperty,
                Value = true
            };
            MultiBinding openBinding = new() { Converter = new FirstNonNullBrushConverter() };
            openBinding.Bindings.Add(new WPF.Data.Binding(nameof(FocusedBorderBrush)) { RelativeSource = templatedParent });
            openBinding.Bindings.Add(new WPF.Data.Binding(nameof(BorderBrush)) { RelativeSource = templatedParent });
            openTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BorderBrushProperty,
                TargetName = "glowBorder",
                Value = openBinding
            });
            template.Triggers.Add(openTrigger);

            // Désactivé : champ estompé.
            Trigger disabledTrigger = new()
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter
            {
                Property = UIElement.OpacityProperty,
                TargetName = "fieldGrid",
                Value = 0.5
            });
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        /// <summary>
        /// Template du bouton du champ : fond transparent, léger voile
        /// blanc au survol (comme le bouton œil). Le contenu (texte +
        /// flèche) est affiché par le ContentPresenter.
        /// </summary>
        private static Controls.ControlTemplate BuildFieldButtonTemplate()
        {
            FrameworkElementFactory border = new(typeof(Controls.Border));
            border.Name = "fieldButtonBorder";
            border.SetValue(Controls.Border.BackgroundProperty, Media.Brushes.Transparent);

            // NOTE : "Content" en chaîne, pas nameof(Content) : la classe
            // ComboBoxCustom n'a pas de propriété Content (c'est le Button,
            // le TemplatedParent de ce template, qui l'a).
            FrameworkElementFactory presenter = new(typeof(Controls.ContentPresenter));
            presenter.SetBinding(
                Controls.ContentPresenter.ContentProperty,
                new WPF.Data.Binding("Content")
                {
                    RelativeSource = new WPF.Data.RelativeSource(WPF.Data.RelativeSourceMode.TemplatedParent)
                });
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, WPF.HorizontalAlignment.Stretch);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            border.AppendChild(presenter);

            Controls.ControlTemplate template = new(typeof(Controls.Button));
            template.VisualTree = border;

            // Survol : léger voile gris (visible sur fond clair : le champ est blanc par défaut).
            Trigger hoverTrigger = new()
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter
            {
                Property = Controls.Border.BackgroundProperty,
                TargetName = "fieldButtonBorder",
                Value = new SolidColorBrush(Color.FromArgb(0x1F, 0x00, 0x00, 0x00))
            });
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        #endregion
    }

    #endregion

}
