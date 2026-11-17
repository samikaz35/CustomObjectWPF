# CustomObjectWPF

Mon premier projet en **VibeCoding**.

L'idée, c'est d'expérimenter le vibe coding — coder en décrivant ce que je veux à une IA, puis itérer — en créant des **assets pour mes futurs projets WPF** : une librairie de contrôles custom, prêts à copier-coller.

> **Version actuelle : v16** — 12 contrôles, 138 exemples dans la vitrine.

---

## Table des matières

- [C'est quoi ?](#cest-quoi-)
- [Pour l'utiliser (3 étapes)](#pour-lutiliser-3-étapes)
- [La vitrine (démo)](#la-vitrine-démo)
- [Les idées de base (à lire une fois)](#les-idées-de-base-à-lire-une-fois)
  - [Un seul fichier, autonome](#un-seul-fichier-autonome)
  - [Le Néon](#le-néon)
  - [Le Watermark (texte d'aide)](#le-watermark-texte-daide)
  - [Les défauts neutres (thème blanc)](#les-défauts-neutres-thème-blanc)
- [Les 12 contrôles, en détail](#les-12-contrôles-en-détail)
  - [ButtonCustom](#buttoncustom)
  - [CheckBoxCustom](#checkboxcustom)
  - [ToggleSwitchCustom](#toggleswitchcustom)
  - [CardCustom](#cardcustom)
  - [SliderCustom](#slidercustom)
  - [ListBoxCustom](#listboxcustom)
  - [TextBoxCustom](#textboxcustom)
  - [PasswordBoxCustom](#passwordboxcustom)
  - [PasswordRevealCustom](#passwordrevealcustom)
  - [SearchBoxCustom](#searchboxcustom)
  - [NumericUpDownCustom](#numericupdowncustom)
  - [ComboBoxCustom](#comboboxcustom)
- [Les événements](#les-événements)
- [Cas d'utilisation (recettes)](#cas-dutilisation-recettes)
- [Historique des versions](#historique-des-versions)
- [Stack et prérequis](#stack-et-prérequis)

---

## C'est quoi ?

Une librairie de **12 contrôles WPF custom** dans **un seul fichier C#** (`CustomObjectWPF.cs`), sans aucune dépendance externe. Les images (œil, loupe, X) sont intégrées directement dans le fichier en base64 : **le fichier voyage seul**, pas besoin de dossier d'images.

Chaque contrôle a ses propres propriétés utiles (coins arrondis, néon, texte d'aide, icônes...) et **garde le comportement normal de WPF** : les `Style`, les `Trigger` et les `Binding` marchent comme d'habitude.

| Contrôle | C'est quoi, en une phrase |
| --- | --- |
| `ButtonCustom` | Bouton avec coins arrondis, couleurs au survol et au clic, néon et icône. |
| `CheckBoxCustom` | Case à cocher stylée : taille de la case, coche colorée, néon. |
| `ToggleSwitchCustom` | Interrupteur on/off avec couleurs personnalisées. |
| `CardCustom` | Carte avec titre, contenu et néon. |
| `SliderCustom` | Curseur avec piste et pastille colorées. |
| `ListBoxCustom` | Liste avec sélection stylée — même des cartes dedans. |
| `TextBoxCustom` | Champ texte avec Watermark et néon. |
| `PasswordBoxCustom` | Mot de passe classique, avec un `Password` bindable. |
| `PasswordRevealCustom` | Mot de passe + œil pour révéler le texte. |
| `SearchBoxCustom` | Barre de recherche : loupe cliquable, X pour effacer, Watermark, événements. |
| `NumericUpDownCustom` | Nombre avec boutons +/−, molette, flèches et format d'affichage. |
| `ComboBoxCustom` | Liste déroulante avec Placeholder et sélection lisible. |

---

## Pour l'utiliser (3 étapes)

**1.** Copie `CustomObjectWPF.cs` dans ton projet (n'importe quel dossier).

**2.** Ajoute le namespace dans ton XAML :

```xml
xmlns:custom="clr-namespace:CustomObjectWPF.Model"
```

**3.** Utilise un contrôle :

```xml
<custom:PasswordRevealCustom Password="secret123" Width="320" Height="42"
                             CornerRadius="10" Watermark="Mot de passe" />
```

C'est tout. **Pas de package NuGet, pas de DLL à référencer, pas de dossier d'images à copier.**

### Un exemple complet (fenêtre de connexion)

```xml
<Window x:Class="MonApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:custom="clr-namespace:CustomObjectWPF.Model"
        Title="Connexion" Width="400" Height="300">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <custom:TextBoxCustom Watermark="Nom d'utilisateur"
                              Height="42" FontSize="16" Margin="0,0,0,12" />
        <custom:PasswordRevealCustom Watermark="Mot de passe"
                                    Height="42" FontSize="16" Margin="0,0,0,16" />
        <custom:ButtonCustom Content="Se connecter" Height="42" FontSize="16"
                             CornerRadius="10" />
    </StackPanel>
</Window>
```

---

## La vitrine (démo)

Le projet `Vitrine/` montre **tous les contrôles** avec une navigation : **une page par contrôle, un exemple par propriété** (138 exemples), avec des cas d'utilisation concrets (quantité, note sur 10, prix, courriel, téléphone...).

Pour la lancer :

- Ouvre `Vitrine.csproj` dans Visual Studio 2022, puis **F5** (ou `dotnet run`).
- La barre de recherche en haut de la barre latérale filtre la liste des contrôles (tape « pass » : il ne reste que les mots de passe).
- Clique un contrôle dans la liste (ou une carte de l'accueil) : sa page s'affiche.

La vitrine est habillée en sombre (c'est son décor), mais **le look par défaut de la librairie n'a pas changé** : fond blanc classique, texte noir, lisible sur thème blanc.

---

## Les idées de base (à lire une fois)

### Un seul fichier, autonome

Toute la librairie tient dans `CustomObjectWPF.cs`. Les icônes (œil ouvert/fermé, loupe, X) sont intégrées en base64 dans le fichier : si tu copies le `.cs` seul dans un autre projet, les icônes suivent, rien ne manque.

Pour mettre **tes propres images** à la place des icônes intégrées, il y a une propriété par icône (`RevealImage`, `HideImage`, `SearchIcon`, `ClearIcon`, `Icon`). En XAML :

```xml
<custom:SearchBoxCustom SearchIcon="Images/loupe.png" />
```

Le PNG doit être dans ton projet avec **Build Action = Resource**, sinon l'application plante au démarrage (l'image est introuvable).

### Le Néon

Tous les contrôles ont les 3 mêmes propriétés :

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Neon` | `bool` | `False` | Active la lueur autour du contrôle. |
| `NeonRadius` | `double` | `20` | La taille de la lueur (plus c'est grand, plus ça diffuse). |
| `NeonColor` | `Color` | `Cyan` | La couleur de la lueur. |

```xml
<custom:ButtonCustom Content="Valider" Neon="True" NeonColor="Magenta" NeonRadius="30" />
```

La lueur est peinte sur une couche **derrière** le texte : le texte reste net (un effet sur le contrôle entier tuerait le lissage du texte).

### Le Watermark (texte d'aide)

Les 4 champs de texte (`TextBoxCustom`, `PasswordBoxCustom`, `PasswordRevealCustom`, `SearchBoxCustom`) ont un Watermark : un texte d'aide affiché quand le champ est vide, qui disparaît dès qu'on tape, et qui réapparaît si le champ redevient vide.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Watermark` | `string` | `""` (vide = pas d'aide) | Le texte d'aide. Pour la recherche, le défaut est `"Rechercher..."`. |
| `WatermarkColor` | `Brush` | Gris `#FF808080` | La couleur du texte d'aide. |

Deux règles fixes (pas de propriété pour les changer) :

- Le Watermark est **toujours un peu transparent (0,6)**, comme un vrai placeholder. La couleur se choisit avec `WatermarkColor`, la transparence est fixe.
- Sa taille **= la taille de la police du champ** (`FontSize`). Pas de propriété de taille séparée.
- Il est **aligné avec le curseur** : le texte d'aide commence exactement là où le curseur clignote (décalé d'un Padding vers la droite, parce que le champ décale son texte en interne).

```xml
<custom:TextBoxCustom Watermark="Ex. : jean@email.com" WatermarkColor="DarkBlue" />
```

### Les défauts neutres (thème blanc)

La librairie part du principe que tout est sur **thème blanc, avec les couleurs par défaut de WPF**. Les défauts sont choisis pour être lisibles sur fond blanc :

- Texte **noir** par défaut sur les champs (fond blanc), bordure grise `#FFABADB3`, taille de base 200 × 32.
- Le Watermark est gris, le Placeholder est gris, le titre des cartes est noir.
- La sélection dans les listes est **cyan + texte noir** (lisible), jamais le bleu clair du système sur fond blanc.
- La liste déroulante du ComboBox **suit le champ quand on défile** (un Popup ne le fait pas tout seul : la librairie le repositionne).
- Les voiles de survol des petits boutons (œil, X, +/−, champ du ComboBox) sont **gris**, pas blancs : sur un champ blanc, un voile blanc était invisible.

Et les valeurs par défaut **ne bloquent rien** : elles sont posées sans créer de valeur locale, donc un `Style`, un `Trigger` ou une valeur en XAML reprend le dessus, comme d'habitude.

---

## Les 12 contrôles, en détail

> Dans les tableaux, **Défaut** = la valeur quand tu ne mets rien. Les propriétés de base héritées de WPF (ex. : `IsEnabled`, `Width`, `Height`, `FontSize`, `Background`, `BorderBrush`) ne sont pas listées : elles marchent comme d'habitude.

### ButtonCustom

Un bouton. Il reste « tenu » même si la souris glisse hors du bouton ensuite (le fond pressé suit le clic, pas seulement le survol).

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `CornerRadius` | `CornerRadius` | `15` | Les coins arrondis du bouton. |
| `HoverBackground` | `Brush` | `null` (= `Background`) | Le fond quand la souris passe dessus. |
| `PressedBackground` | `Brush` | `null` (= `Background`) | Le fond quand on clique. |
| `Icon` | `ImageSource` | `null` (pas d'icône) | Une image à gauche du texte. |
| `IconWidth` | `double` | `20` | La taille de l'icône, en largeur et en hauteur (carrée). |
| `IconSpacing` | `double` | `8` | L'espacement entre l'icône et le texte. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur (voir [Le Néon](#le-néon)). |

```xml
<custom:ButtonCustom Content="Valider" Width="140" Height="42" CornerRadius="10" />
<custom:ButtonCustom Content="Rechercher" Icon="Images/loupe.png" IconWidth="24" Height="42" />
<custom:ButtonCustom Content="Supprimer" HoverBackground="DarkBlue" PressedBackground="DarkRed" />
```

### CheckBoxCustom

Une case à cocher. Le crochet s'adapte automatiquement à la taille de la case.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `IsChecked` | `bool?` | `False` | Coché ou pas (propriété de base de WPF, bindable). |
| `CheckColor` | `Brush` | Noir | La couleur du crochet. |
| `BoxSize` | `double` | `14` | La taille (côté, en px) de la case. |
| `BoxSpacing` | `double` | `5` | L'espacement entre la case et le libellé. |
| `BorderHover` | `Brush` | Bleu `#FF2997DE` | La couleur de la bordure de la case au survol. |
| `CornerRadius` | `CornerRadius` | `0` (carré) | Les coins arrondis de la case. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:CheckBoxCustom Content="Se souvenir de moi" IsChecked="True" CheckColor="Green" />
<custom:CheckBoxCustom Content="Grosse case" BoxSize="28" BoxSpacing="20" />
```

### ToggleSwitchCustom

Un interrupteur on/off. La pastille glisse d'un côté à l'autre.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `IsChecked` | `bool?` | `False` | Allumé ou éteint (propriété de base de WPF, bindable). |
| `OnBrush` | `Brush` | Cyan `#FF00D1FF` | La couleur de la piste quand c'est allumé. |
| `OffBrush` | `Brush` | Gris foncé `#FF555555` | La couleur de la piste quand c'est éteint. |
| `ThumbBrush` | `Brush` | Blanc | La couleur de la pastille (le cercle). |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:ToggleSwitchCustom IsChecked="True" OnBrush="Green" OffBrush="Red" ThumbBrush="Yellow" />
```

### CardCustom

Une carte : un panneau avec un titre optionnel et n'importe quel contenu (texte, image, bouton, liste...). Utile pour structurer des sections.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Title` | `string` | `""` (pas de titre) | Le titre de la carte. Vide = pas de titre affiché. |
| `TitleForeground` | `Brush` | Noir | La couleur du titre. |
| `TitleFontSize` | `double` | `14` | La taille du titre. |
| `TitleFontWeight` | `FontWeight` | `SemiBold` | La graisse du titre (`Normal`, `SemiBold`, `Bold`...). |
| `CornerRadius` | `CornerRadius` | `0` (carré) | Les coins arrondis de la carte. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:CardCustom Title="Paramètres" CornerRadius="10" Width="320">
    <StackPanel>
        <TextBlock Text="Contenu de la carte..." />
        <custom:ButtonCustom Content="Enregistrer" />
    </StackPanel>
</custom:CardCustom>
```

### SliderCustom

Un curseur (piste + pastille). La valeur, le minimum et le maximum sont les propriétés de base du `Slider` de WPF (`Value`, `Minimum`, `Maximum`, `SmallChange`, `LargeChange`).

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Value` | `double` | `0` | La valeur actuelle (base WPF, bindable). |
| `Minimum` / `Maximum` | `double` | `0` / `100` | Les bornes (base WPF). |
| `TrackBrush` | `Brush` | Gris foncé `#FF555555` | La couleur de la piste. |
| `ThumbBrush` | `Brush` | Cyan `#FF00D1FF` | La couleur de la pastille. |
| `TrackHeight` | `double` | `6` | L'épaisseur (en px) de la piste. |
| `ThumbSize` | `double` | `18` | La taille de la pastille. |
| `TrackCornerRadius` | `CornerRadius` | `1000` (pilule) | La forme de la piste : `0` = rectangle, grande valeur = pilule. |
| `ThumbCornerRadius` | `CornerRadius` | `1000` (rond) | La forme de la pastille : `0` = carré, grande valeur = rond. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:SliderCustom Value="30" Minimum="0" Maximum="100" Width="320" />
<custom:SliderCustom Value="8" Minimum="0" Maximum="10" TrackBrush="DarkBlue" ThumbBrush="Red" />
```

### ListBoxCustom

Une liste. La sélection est stylée, et les lignes peuvent contenir n'importe quoi (du texte, ou même des cartes).

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `ItemsSource` | `IEnumerable` | `null` | La liste des éléments (base WPF). |
| `SelectedItem` | `object` | `null` | L'élément choisi (base WPF, bindable). |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis de la liste. |
| `ItemCornerRadius` | `CornerRadius` | `8` | Les coins arrondis de chaque ligne. |
| `SelectedItemBackground` | `Brush` | Cyan `#FF00D1FF` | Le fond de la ligne choisie. |
| `SelectedItemForeground` | `Brush` | Noir | Le texte de la ligne choisie. |
| `SelectedItemBorderBrush` | `Brush` | Cyan `#FF00D1FF` | La bordure de la ligne choisie. |

Le survol d'une ligne montre un fond bleu très clair (texte inchangé, noir par défaut : lisible). **Quand on survole une ligne déjà choisie, c'est sa sélection qui reste** (son fond et son texte) : le survol ne cache jamais la sélection, donc un texte blanc sur fond bleu foncé reste lisible.

```xml
<custom:ListBoxCustom Width="320" Height="180" SelectedItem="Code">
    <ListBoxItem>Documents</ListBoxItem>
    <ListBoxItem>Images</ListBoxItem>
    <ListBoxItem>Code</ListBoxItem>
</custom:ListBoxCustom>
```

**Des cartes dans la liste** (pratique à connaître) : les lignes peuvent contenir des `CardCustom` — le contenu d'une ligne peut être n'importe quoi, et `SelectedItem` te donne directement l'objet choisi.

```xml
<custom:ListBoxCustom Width="320">
    <ListBoxItem><custom:CardCustom Title="Rapport.docx" CornerRadius="8" /></ListBoxItem>
    <ListBoxItem><custom:CardCustom Title="Budget.xlsx" CornerRadius="8" /></ListBoxItem>
</custom:ListBoxCustom>
```

### TextBoxCustom

Un champ texte classique (c'est un vrai `TextBox` de WPF, avec `Text`, `MaxLength`, `IsReadOnly`...), avec en plus :

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis du champ. |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand le champ a le focus. |
| `Watermark` | `string` | `""` (pas d'aide) | Le texte d'aide quand le champ est vide. |
| `WatermarkColor` | `Brush` | Gris `#FF808080` | La couleur du texte d'aide. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:TextBoxCustom Text="Bonjour" Watermark="Tape ton nom..." CornerRadius="20" />
<custom:TextBoxCustom Watermark="Ex. : jean@email.com" WatermarkColor="DarkBlue" MaxLength="10" />
```

### PasswordBoxCustom

Un mot de passe classique. En WPF, `PasswordBox` est **scellé** (on ne peut pas en hériter), donc c'est un habillage (`Control`) autour d'un vrai `PasswordBox` — avec un bonus : le `Password` est **bindable** (le vrai ne l'est pas).

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Password` | `string` | `""` | Le mot de passe, bindable dans les deux sens. |
| `PasswordChar` | `char` | `•` | Le caractère affiché à la place des lettres. |
| `MaxLength` | `int` | `0` (= illimité) | La longueur max. |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis du champ. |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand le champ a le focus. |
| `Watermark` | `string` | `""` (pas d'aide) | Le texte d'aide quand le champ est vide. |
| `WatermarkColor` | `Brush` | Gris `#FF808080` | La couleur du texte d'aide. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:PasswordBoxCustom Password="secret" Watermark="Mot de passe" />
<custom:PasswordBoxCustom Password="1234" PasswordChar="*" MaxLength="4" Watermark="Code PIN" />
```

### PasswordRevealCustom

La fusion des deux : un mot de passe + un bouton œil pour révéler le texte. Les deux champs (masqué et visible) partagent les mêmes propriétés visuelles, donc **le champ ne change pas d'allure quand on bascule**.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Password` | `string` | `""` | Le mot de passe, bindable dans les deux sens. |
| `IsPasswordRevealed` | `bool` | `False` | Le basculeur : `True` = texte visible, `False` = masqué. À lire et à changer en code (`get`/`set`). |
| `PasswordChar` | `char` | `•` | Le caractère affiché à la place des lettres. |
| `MaxLength` | `int` | `0` (= illimité) | La longueur max. |
| `RevealImage` | `ImageSource` | Œil ouvert (intégré) | L'image de l'œil quand le texte est visible. |
| `HideImage` | `ImageSource` | Œil fermé (intégré) | L'image de l'œil quand le texte est masqué. |
| `EyeColor` | `Brush` | `null` (noir d'origine) | La couleur de l'œil (il est reteint pixel par pixel). |
| `EyeHoverColor` | `Brush` | `null` | La couleur de l'œil au survol (gagne sur `EyeColor`, marche même sans `EyeColor`). |
| `EyeWidth` | `double` | `45` | La largeur du bouton œil. |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis du champ. |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand le champ a le focus. |
| `Watermark` | `string` | `""` (pas d'aide) | Le texte d'aide quand le champ est vide. |
| `WatermarkColor` | `Brush` | Gris `#FF808080` | La couleur du texte d'aide. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:PasswordRevealCustom Password="secret" Watermark="Mot de passe" />
<custom:PasswordRevealCustom Password="secret" IsPasswordRevealed="True" EyeColor="Red" EyeHoverColor="Orange" />
<custom:PasswordRevealCustom Password="secret" RevealImage="Images/oeil-ouvert.png" HideImage="Images/oeil-ferme.png" />
```

**En code** (le basculeur) :

```csharp
// Lire : est-ce que le mot de passe est visible ?
bool visible = pwd.IsPasswordRevealed;
// Changer : afficher le mot de passe en clair.
pwd.IsPasswordRevealed = true;
```

### SearchBoxCustom

Une barre de recherche : une loupe à gauche, un X pour effacer (il n'apparaît que quand il y a du texte), un Watermark, et deux événements.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Watermark` | `string` | `"Rechercher..."` | Le texte d'aide quand la barre est vide. |
| `WatermarkColor` | `Brush` | Gris `#FF808080` | La couleur du texte d'aide. |
| `IconColor` | `Brush` | `null` (= `Foreground`) | La couleur de la loupe. |
| `IconHoverColor` | `Brush` | `null` | La couleur de la loupe au survol. |
| `ClearIconColor` | `Brush` | `null` (= `IconColor`, sinon `Foreground`) | La couleur du X. |
| `ClearIconHoverColor` | `Brush` | `null` | La couleur du X au survol. |
| `SearchIcon` | `ImageSource` | Loupe (intégrée) | Ta propre image à la place de la loupe (null = pas d'icône). |
| `ClearIcon` | `ImageSource` | X (intégré) | Ta propre image à la place du X (null = pas d'icône). |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis de la barre. |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand la barre a le focus. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

Les icônes par défaut sont **noires** (comme les yeux) : `IconColor` / `ClearIconColor` les teintent. Une image perso garde ses propres couleurs (elle n'est pas teintée).

```xml
<custom:SearchBoxCustom Watermark="Rechercher un document..." />
<custom:SearchBoxCustom Text="test" IconColor="DarkBlue" ClearIconColor="Red" />
<custom:SearchBoxCustom SearchIcon="Images/loupe.png" ClearIcon="Images/x.png" />
```

**Recherche fonctionnelle** (filtrer une liste en direct) :

```xml
<custom:SearchBoxCustom x:Name="recherche" Watermark="Rechercher un document..." Search="Recherche_Search" />
<ListBox x:Name="resultats" />
```

```csharp
private void Recherche_TextChanged(object sender, TextChangedEventArgs e)
{
    resultats.ItemsSource = _documents
        .Where(d => d.Contains(recherche.Text, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

// Entrée = événement Search.
private void Recherche_Search(object sender, RoutedEventArgs e)
{
    MessageBox.Show($"Recherche lancée pour « {recherche.Text} ».");
}
```

### NumericUpDownCustom

Un nombre avec boutons +/−, molette et flèches. Maintenir un bouton = répétition. Un nombre invalide = retour à la dernière valeur.

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `Value` | `double` | `0` | La valeur actuelle (bindable). |
| `Minimum` | `double` | `0` | La borne basse. |
| `Maximum` | `double` | `100` | La borne haute. |
| `Increment` | `double` | `1` | Le pas (+/−, molette, flèches). |
| `Format` | `string` | `null` (= nombre brut) | Le format d'affichage : `"0.00"` (2 décimales), `"C2"` (prix), `"0' %'"` (pourcentage). C'est de l'affichage seulement : quand on tape, on tape le nombre brut. |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis du champ. |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand le champ a le focus. |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:NumericUpDownCustom Value="5" Minimum="0" Maximum="20" />
<custom:NumericUpDownCustom Value="9.99" Minimum="0" Maximum="100" Increment="0.5" Format="C2" />
<custom:NumericUpDownCustom Value="50" Minimum="0" Maximum="100" Increment="5" Format="0' %'" />
<custom:NumericUpDownCustom Value="25" Minimum="0" Maximum="120" />
```

### ComboBoxCustom

Une liste déroulante. La liste est un `Popup` (une fenêtre flottante) : elle suit le champ quand on défile, se ferme sur Entrée / Échap / clic ailleurs, et on peut naviguer aux flèches dès l'ouverture. Le texte du champ est lié au `Foreground` du contrôle (noir par défaut), et **quand on survole un élément déjà choisi dans la liste, c'est sa sélection qui reste** (comme pour la ListBox).

| Propriété | Type | Défaut | Ça fait quoi |
| --- | --- | --- | --- |
| `ItemsSource` | `IEnumerable` | `null` | La liste des choix. |
| `SelectedItem` | `object` | `null` | L'élément choisi (bindable). |
| `DisplayMemberPath` | `string` | `null` | La propriété à afficher quand les choix sont des objets (ex. : `"Nom"`). |
| `Placeholder` | `string` | `"Choisir..."` | Le texte quand rien n'est choisi. |
| `PlaceholderColor` | `Brush` | Gris `#FF808080` | La couleur du Placeholder. |
| `MaxDropDownHeight` | `double` | `200` | La hauteur max de la liste (elle défile au-delà). |
| `IsDropDownOpen` | `bool` | `False` | Ouvre/ferme la liste (bindable, en code). |
| `SelectedItemBackground` | `Brush` | Cyan `#FF00D1FF` | Le fond de l'élément choisi dans la liste. |
| `SelectedItemForeground` | `Brush` | Noir | Le texte de l'élément choisi dans la liste. |
| `SelectedItemBorderBrush` | `Brush` | Cyan `#FF00D1FF` | La bordure de l'élément choisi dans la liste. |
| `CornerRadius` | `CornerRadius` | `10` | Les coins arrondis du champ **et** du cadre de la liste — les lignes suivent aussi ce rayon (fini le rectangle dans le rectangle). |
| `FocusedBorderBrush` | `Brush` | `null` (= `BorderBrush`) | La couleur de la bordure quand le champ a le focus (gardée tant que la liste est ouverte). |
| `Neon` / `NeonRadius` / `NeonColor` | `bool` / `double` / `Color` | `False` / `20` / `Cyan` | La lueur. |

```xml
<custom:ComboBoxCustom ItemsSource="{Binding Categories}" SelectedItem="{Binding CategorieChoisie}" DisplayMemberPath="Nom" Placeholder="Choisir une catégorie..." />
<custom:ComboBoxCustom x:Name="combo" Placeholder="Choisir..." IsDropDownOpen="True" />
```

**Avec des objets** (le texte affiché vient de la propriété `Nom`) :

```csharp
public record Categorie(string Nom);
combo.ItemsSource = new List<Categorie> { new("Documents"), new("Images"), new("Code") };
```

```xml
<custom:ComboBoxCustom x:Name="combo" DisplayMemberPath="Nom" />
```

---

## Les événements

| Événement | Contrôle | Déclenché quand... | En XAML |
| --- | --- | --- | --- |
| `Search` | `SearchBoxCustom` | On appuie sur **Entrée** dans la barre. | `Search="MaMethode"` |
| `SearchIconClicked` | `SearchBoxCustom` | On **clique sur la loupe**. Sa propre action, séparée de `Search` : tu y attaches ce que tu veux (ex. : lancer la recherche). | `SearchIconClicked="MaMethode"` |

```csharp
// Entrée : lancer la recherche.
private void Recherche_Search(object sender, RoutedEventArgs e)
{
    Filtrer(recherche.Text);
}

// Clic sur la loupe : la même action, à la souris.
private void Recherche_SearchIconClicked(object sender, RoutedEventArgs e)
{
    Filtrer(recherche.Text);
}
```

La loupe montre un curseur « main » quand la souris passe dessus : on voit qu'elle est cliquable.

---

## Cas d'utilisation (recettes)

**Un formulaire de connexion** — `TextBoxCustom` (nom) + `PasswordRevealCustom` (mot de passe) + `ButtonCustom` (valider). Voir [l'exemple complet](#pour-lutiliser-3-étapes).

**Un code PIN à 4 chiffres** — `PasswordBoxCustom` avec `PasswordChar="*"` et `MaxLength="4"`.

**Une quantité dans un panier** — `NumericUpDownCustom` avec `Minimum="1"`, `Maximum="99"`, `Value="1"`.

**Un prix** — `NumericUpDownCustom` avec `Format="C2"`, `Increment="0.5"`.

**Une note sur 10** — `NumericUpDownCustom` avec `Maximum="10"`, ou `SliderCustom` avec `Maximum="10"`.

**Un pourcentage** — `NumericUpDownCustom` avec `Minimum="0"`, `Maximum="100"`, `Increment="5"`, `Format="0' %'"`.

**Un âge** — `NumericUpDownCustom` avec `Minimum="0"`, `Maximum="120"`.

**Une recherche qui filtre une liste** — `SearchBoxCustom` + `TextChanged` (filtre en direct) + `Search` (Entrée) ou `SearchIconClicked` (clic sur la loupe). Voir [SearchBoxCustom](#searchboxcustom).

**Une catégorie (objets)** — `ComboBoxCustom` avec `DisplayMemberPath="Nom"` et `SelectedItem` bindé sur ton objet. Voir [ComboBoxCustom](#comboboxcustom).

**Des cartes dans une liste** — `ListBoxCustom` avec des `CardCustom` dans les lignes : `SelectedItem` te donne directement l'objet choisi. Voir [ListBoxCustom](#listboxcustom).

**Un panneau d'options** — `CardCustom` avec un titre et des contrôles dedans (bouton, interrupteur, case à cocher...).

---

## Historique des versions

- **v1–v3** — Les 3 champs (texte, mot de passe, mot de passe + œil). Le `PasswordBox` est scellé en WPF : les mots de passe sont des habillages (`Control`), pas des héritages.
- **v4–v5** — Les yeux intégrés en base64 dans le `.cs` (le fichier voyage seul) + corrections des avertissements.
- **v6** — Néon sur les 3 champs + taille de base + œil en haute qualité.
- **v7** — Fond blanc par défaut + l'œil sorti du bouton (c'était le vrai bug d'`EyeColor`) + compteur `VERSION` en tête du fichier.
- **v8** — L'œil est reteint pixel par pixel (même taille, même position) + `EyeHoverColor` + le champ ne grandit plus avec le texte.
- **v9** — `SearchBoxCustom` + `NumericUpDownCustom`.
- **v10** — `ComboBoxCustom` + passe de vérification complète.
- **v11** — `Watermark` / `WatermarkColor` sur les 3 champs + icônes perso pour la recherche.
- **v12** — Loupe + X intégrés en base64, exactement comme les yeux.
- **v13** — Néon sur `CardCustom` + texte noir par défaut + Watermark semi-transparent (0,6) + Watermark derrière le texte + `IconHoverColor` / `ClearIconHoverColor`.
- **v14** — Thème blanc : sélection de la liste déroulante lisible + la liste suit le champ quand on défile + titre des cartes noir par défaut + survol des lignes + voiles de survol gris.
- **v15** — Bouton avec icône (`Icon` / `IconWidth` / `IconSpacing`) + clic sur la loupe (`SearchIconClicked`) + `Format` du nombre + `TitleFontWeight`.
- **v16** — Le survol ne cache plus la sélection (ListBox + liste du ComboBox) + Watermark aligné avec le curseur + texte du champ du ComboBox lié au `Foreground` (noir par défaut).

Le compteur `VERSION` est en tête de `CustomObjectWPF.cs`, avec l'historique : **+1 à chaque modification**.

---

## Stack et prérequis

- C# / WPF (.NET 10, Windows) — la librairie compile sans erreur, vérifiée à chaque modification.
- Codé en dialogue avec une IA (vibe coding), relu et testé par compilation à chaque étape. Le rendu visuel est testé sous Windows.
- Aucune dépendance externe, aucun package NuGet.
