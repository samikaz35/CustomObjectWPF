# CustomObjectWPF

Mon premier projet en **VibeCoding**.

L'idée, c'est d'expérimenter le vibe coding — coder en décrivant ce que je veux à une IA, puis itérer — en créant des **assets pour mes futurs projets WPF** : une librairie de contrôles custom, prêts à copier-coller.

## C'est quoi ?

Une librairie de **12 contrôles WPF custom** dans **un seul fichier C#** (`CustomObjectWPF.cs`), sans aucune dépendance externe. Les images (œil, loupe, X) sont intégrées directement dans le fichier en base64 : le fichier voyage seul, pas besoin de dossier d'images.

Chaque contrôle a ses propres propriétés utiles (coins arrondis, néon, texte d'aide...) et garde le comportement normal de WPF : les `Style`, les `Trigger` et les `Binding` marchent comme d'habitude.

## Les 12 contrôles

| Contrôle | C'est quoi |
| --- | --- |
| `ButtonCustom` | Bouton avec coins arrondis, couleurs au survol et au clic, néon |
| `CheckBoxCustom` | Case à cocher stylée : taille de la case, coche colorée, néon |
| `ToggleSwitchCustom` | Interrupteur on/off avec couleurs personnalisées |
| `CardCustom` | Carte avec titre, contenu et néon |
| `SliderCustom` | Curseur avec piste et pastille colorées |
| `ListBoxCustom` | Liste avec sélection stylée — même des cartes dedans |
| `TextBoxCustom` | Champ texte avec Watermark et néon |
| `PasswordBoxCustom` | Mot de passe classique, avec un `Password` bindable |
| `PasswordRevealCustom` | Mot de passe + œil pour révéler le texte |
| `SearchBoxCustom` | Barre de recherche : loupe, X pour effacer, Watermark, événement `Search` |
| `NumericUpDownCustom` | Nombre avec boutons +/−, molette et flèches Haut/Bas |
| `ComboBoxCustom` | Liste déroulante avec Placeholder |

## Pour l'utiliser

**1.** Copie `CustomObjectWPF.cs` dans ton projet.

**2.** Ajoute le namespace dans ton XAML :

```xml
xmlns:custom="clr-namespace:CustomObjectWPF.Model"
```

**3.** Utilise un contrôle :

```xml
<custom:PasswordRevealCustom Password="secret123" Width="320" Height="42"
                             CornerRadius="10" Watermark="Mot de passe" />
```

C'est tout. Pas de package NuGet, pas de DLL à référencer, pas de dossier d'images à copier.

## La vitrine

Le projet `Vitrine/` montre tous les contrôles avec une navigation : **une page par contrôle, un exemple par propriété**, avec des cas d'utilisation concrets (quantité, note sur 10, prix, courriel, téléphone...).

Pour la lancer : ouvre `Vitrine.csproj` dans Visual Studio 2022, puis **F5** (ou `dotnet run`).

## Points forts

- **Un seul fichier** : toute la librairie tient dans `CustomObjectWPF.cs`.
- **Autonome** : les icônes sont intégrées en base64, aucun fichier externe requis.
- **Coins arrondis partout** : `CornerRadius` sur (presque) tous les contrôles.
- **Néon** : `Neon` / `NeonColor` / `NeonRadius` sur tous les contrôles — la lueur est derrière le texte, donc le texte reste net.
- **Watermark** : un texte d'aide quand le champ est vide, avec sa couleur (`WatermarkColor`), qui disparaît dès qu'on tape.
- **Icônes personnalisables** : l'œil, la loupe et le X se remplacent par tes propres images (`RevealImage`, `HideImage`, `SearchIcon`, `ClearIcon`), avec leur couleur au survol (`EyeHoverColor`, `IconHoverColor`, `ClearIconHoverColor`).
- **Défauts neutres** : les valeurs par défaut ne bloquent ni les `Style`, ni les `Trigger`, ni les `Binding`.

## Stack

- C# / WPF (.NET 10, Windows)
- Codé en dialogue avec une IA (vibe coding), vérifié par compilation à chaque modification

## État

Version **v13** — en développement actif. La librairie compile sans erreur ; le rendu visuel est testé sous Windows.
