VITRINE CustomObjectWPF — v12
==============================

Une démo qui tourne : TOUS les contrôles, avec une navigation.

NAVIGATION
----------
- Barre latérale à gauche : la liste des 12 contrôles + l'accueil.
- La barre de recherche en haut de la barre latérale filtre la liste
  (tape "pass" : il ne reste que les mots de passe).
- Clique un contrôle dans la liste (ou une carte de l'accueil) : sa page
  s'affiche, avec un exemple par propriété.
- L'accueil montre les 12 contrôles en cartes cliquables.

CONTENU
-------
- Vitrine.csproj ............ projet WPF (.NET 10, Windows)
- App.xaml / App.xaml.cs ... point d'entrée
- MainWindow.xaml .......... la vitrine (barre latérale + 13 pages)
- MainWindow.xaml.cs ....... navigation + recherche + données des ComboBox
- CustomObjectWPF.cs ....... la librairie complète (v12, 12 contrôles)
- Images/loupe.png ......... loupe cyan (exemple : REMPLACER la loupe intégrée)
- Images/x.png ............. X rouge (exemple : REMPLACER le X intégré)

OUVRIR / LANCER
---------------
- Double-clique sur Vitrine.csproj (Visual Studio 2022, .NET 10),
  puis F5.
- Ou en ligne de commande : dotnet run

LES 12 CONTRÔLES (1 page chacun, plusieurs exemples par page)
------------------------------------------------------------
ButtonCustom, CheckBoxCustom, ToggleSwitchCustom, CardCustom,
SliderCustom, ListBoxCustom, TextBoxCustom, PasswordBoxCustom,
PasswordRevealCustom, SearchBoxCustom, NumericUpDownCustom,
ComboBoxCustom.

La page SearchBox garde la recherche fonctionnelle : la liste de
documents se filtre en direct quand tu tapes, Entrée = événement Search.
La page Slider a une valeur en direct, la page ComboBox montre aussi
DisplayMemberPath avec des objets.

NOTE
----
Le visuel de la librairie elle-même n'a pas changé (fond blanc, bordure
grise, comme l'original) — c'est la vitrine qui est en thème sombre.
Si tu veux un autre look par défaut pour les contrôles, dis-le et on le fait.
