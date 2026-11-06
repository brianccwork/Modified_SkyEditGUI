Option Strict On
Option Explicit On

Imports System.Drawing

'Artwork-only metadata. Browsing never selects an identity or changes scanned figure data.
Friend Enum ArtworkBrowserMode
    Skylanders
    Vehicles
    Crystals
End Enum

Friend NotInheritable Class ArtworkBrowserSection
    Friend ReadOnly Title As String
    Friend ReadOnly Game As String
    Friend ReadOnly Names As String()
    Friend ReadOnly BackColor As Color

    Friend Sub New(title As String, game As String, names As IEnumerable(Of String), color As Color)
        Me.Title = title
        Me.Game = game
        Me.Names = names.ToArray()
        Me.BackColor = color
    End Sub
End Class

Friend NotInheritable Class EditorArtworkCatalog
    Private Sub New()
    End Sub

    Private NotInheritable Class CrystalArtwork
        Friend ReadOnly Id As Integer
        Friend ReadOnly VariantId As Integer
        Friend ReadOnly Name As String
        Friend ReadOnly Element As String
        Friend ReadOnly Path As String
        Friend Sub New(id As Integer, variantId As Integer, name As String, element As String, path As String)
            Me.Id = id : Me.VariantId = variantId : Me.Name = name : Me.Element = element : Me.Path = path
        End Sub
    End Class

    'Exact cropped PNG filenames from the supplied tree, plus the UNSORTED PNGs.
    Private Shared ReadOnly crystals As CrystalArtwork() = {
        New CrystalArtwork(682, &H5202, "Air Angel", "Air", "SI\8) Creation Crystals\SkylandersNFC's Air Angel.png"),
        New CrystalArtwork(682, &H5207, "Air Lantern", "Air", "SI\8) Creation Crystals\SkylandersNFC's Air Lantern.png"),
        New CrystalArtwork(688, &H5206, "Dark Pyramid", "Dark", "SI\8) Creation Crystals\SkylandersNFC's Dark Pyramid.png"),
        New CrystalArtwork(688, &H520E, "Dark Reactor", "Dark", "SI\8) Creation Crystals\SkylandersNFC's Dark Reactor.png"),
        New CrystalArtwork(688, &H520A, "Dark Rune", "Dark", "SI\8) Creation Crystals\SkylandersNFC's Dark Rune.png"),
        New CrystalArtwork(686, &H5213, "Earth Armor", "Earth", "SI\8) Creation Crystals\SkylandersNFC's Earth Armor.png"),
        New CrystalArtwork(686, &H521D, "Earth Rocket", "Earth", "SI\8) Creation Crystals\SkylandersNFC's Earth Rocket.png"),
        New CrystalArtwork(685, &H5211, "Fire Acorn", "Fire", "SI\8) Creation Crystals\SkylandersNFC's Fire Acorn.png"),
        New CrystalArtwork(685, &H520F, "Fire Reactor", "Fire", "SI\8) Creation Crystals\SkylandersNFC's Fire Reactor.png"),
        New CrystalArtwork(687, &H5210, "Life Acorn", "Life", "SI\8) Creation Crystals\SkylandersNFC's Life Acorn.png"),
        New CrystalArtwork(687, &H521A, "Life Claw", "Life", "SI\8) Creation Crystals\SkylandersNFC's Life Claw.png"),
        New CrystalArtwork(687, &H521E, "Life Rocket", "Life", "SI\8) Creation Crystals\SkylandersNFC's Life Rocket.png"),
        New CrystalArtwork(687, &H521F, "Life Rune", "Life", "SI\8) Creation Crystals\SkylandersNFC's Life Rune.png"),
        New CrystalArtwork(689, &H5216, "Light Fanged", "Light", "SI\8) Creation Crystals\SkylandersNFC's Light Fanged.png"),
        New CrystalArtwork(689, &H520B, "Light Rune", "Light", "SI\8) Creation Crystals\SkylandersNFC's Light Rune.png"),
        New CrystalArtwork(680, &H521B, "Magic Claw", "Magic", "SI\8) Creation Crystals\SkylandersNFC's Magic Claw.png"),
        New CrystalArtwork(680, &H5208, "Magic Lantern", "Magic", "SI\8) Creation Crystals\SkylandersNFC's Magic Lantern.png"),
        New CrystalArtwork(680, &H5204, "Magic Pyramid", "Magic", "SI\8) Creation Crystals\SkylandersNFC's Magic Pyramid.png"),
        New CrystalArtwork(684, &H5215, "Tech Armor", "Tech", "SI\8) Creation Crystals\SkylandersNFC's Tech Armor.png"),
        New CrystalArtwork(684, &H520D, "Tech Reactor", "Tech", "SI\8) Creation Crystals\SkylandersNFC's Tech Reactor.png"),
        New CrystalArtwork(683, &H5219, "Undead Claw", "Undead", "SI\8) Creation Crystals\SkylandersNFC's Undead Claw.png"),
        New CrystalArtwork(683, &H5217, "Undead Fanged", "Undead", "SI\8) Creation Crystals\SkylandersNFC's Undead Fanged.png"),
        New CrystalArtwork(683, &H5209, "Undead Lantern", "Undead", "SI\8) Creation Crystals\SkylandersNFC's Undead Lantern.png"),
        New CrystalArtwork(681, &H5214, "Water Armor", "Water", "SI\8) Creation Crystals\SkylandersNFC's Water Armor.png"),
        New CrystalArtwork(681, &H5218, "Water Fanged", "Water", "SI\8) Creation Crystals\SkylandersNFC's Water Fanged.png"),
        New CrystalArtwork(681, &H521C, "Water Rocket", "Water", "SI\8) Creation Crystals\SkylandersNFC's Water Rocket.png"),
        New CrystalArtwork(687, &H5610, "Legendary Life Acorn", "Life", "SI\8) Creation Crystals\SkylandersNFC's Legendary Life Acorn.png"),
        New CrystalArtwork(689, &H5616, "Legendary Light Fanged", "Light", "SI\8) Creation Crystals\SkylandersNFC's Legendary Light Fanged.png"),
        New CrystalArtwork(680, &H5608, "Legendary Magic Lantern", "Magic", "SI\8) Creation Crystals\SkylandersNFC's Legendary Magic Lantern.png"),
        New CrystalArtwork(682, &H5212, "Air Acorn", "Air", "UNSORTED\AirAcornCC.png"),
        New CrystalArtwork(686, &H520C, "Earth Rune", "Earth", "UNSORTED\EarthRuneCC.png"),
        New CrystalArtwork(685, &H5203, "Fire Angel", "Fire", "UNSORTED\FireAngelCC.png"),
        New CrystalArtwork(689, &H5201, "Light Angel", "Light", "UNSORTED\LightAngelCC.png"),
        New CrystalArtwork(684, &H5205, "Tech Pyramid", "Tech", "UNSORTED\TechPyramidCC.png")
    }

    Private Shared ReadOnly paths As Dictionary(Of String, String) = BuildPaths()

    Private Shared Function BuildPaths() As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each crystal As CrystalArtwork In crystals
            result.Add("Imaginators Crystals/" & crystal.Name, crystal.Path)
        Next
        result.Add("Vehicles/Buzz Wing", "SSC\2) Vehicles\Air Vehicles\fruitsnack's buzz wing.png")
        result.Add("Vehicles/Clown Cruiser", "SSC\2) Vehicles\Air Vehicles\fruitsnack's clown cruiser.png")
        result.Add("Vehicles/Jet Stream", "SSC\2) Vehicles\Air Vehicles\fruitsnack's jet stream.png")
        result.Add("Vehicles/Sky Slicer", "SSC\2) Vehicles\Air Vehicles\fruitsnack's sky slicer.png")
        result.Add("Vehicles/Stealth Stinger", "SSC\2) Vehicles\Air Vehicles\fruitsnack's stealth stinger.png")
        result.Add("Vehicles/Sun Runner", "SSC\2) Vehicles\Air Vehicles\fruitsnack's sun runner.png")
        result.Add("Vehicles/Barrel Blaster", "SSC\2) Vehicles\Land Vehicles\fruitsnack's barrel blaster.png")
        result.Add("Vehicles/Burn-Cycle", "SSC\2) Vehicles\Land Vehicles\fruitsnack's burn-cycle.png")
        result.Add("Vehicles/Crypt Crusher", "SSC\2) Vehicles\Land Vehicles\fruitsnack's crypt crusher.png")
        result.Add("Vehicles/Gold Rusher", "SSC\2) Vehicles\Land Vehicles\fruitsnack's gold rusher.png")
        result.Add("Vehicles/Hot Streak", "SSC\2) Vehicles\Land Vehicles\fruitsnack's hot streak.png")
        result.Add("Vehicles/Shark Tank", "SSC\2) Vehicles\Land Vehicles\fruitsnack's shark tank.png")
        result.Add("Vehicles/Shield Striker", "SSC\2) Vehicles\Land Vehicles\fruitsnack's shield striker.png")
        result.Add("Vehicles/Thump Truck", "SSC\2) Vehicles\Land Vehicles\fruitsnack's thump truck.png")
        result.Add("Vehicles/Tomb Buggy", "SSC\2) Vehicles\Land Vehicles\fruitsnack's tomb buggy.png")
        result.Add("Vehicles/Dive Bomber", "SSC\2) Vehicles\Sea Vehicles\fruitsnack's dive bomber.png")
        result.Add("Vehicles/Reef Ripper", "SSC\2) Vehicles\Sea Vehicles\fruitsnack's reef ripper.png")
        result.Add("Vehicles/Sea Shadow", "SSC\2) Vehicles\Sea Vehicles\fruitsnack's sea shadow.png")
        result.Add("Vehicles/Soda Skimmer", "SSC\2) Vehicles\Sea Vehicles\fruitsnack's soda skimmer.png")
        result.Add("Vehicles/Splatter Splasher", "SSC\2) Vehicles\Sea Vehicles\fruitsnack's splatter splasher.png")
        result.Add("Vehicles/Dark Clown Cruiser", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Air Vehicles\fruitsnack's dark clown cruiser.png")
        result.Add("Vehicles/Nitro Stealth Stinger", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Air Vehicles\fruitsnack's nitro stealth stinger.png")
        result.Add("Vehicles/Dark Barrel Blaster", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's dark barrel blaster.png")
        result.Add("Vehicles/Dark Hot Streak", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's dark hot streak .png")
        result.Add("Vehicles/E3 Hot Streak", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's event exclusive hot streak.png")
        result.Add("Vehicles/Golden Hot Streak", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's golden hot streak.png")
        result.Add("Vehicles/Instant Hot Streak", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's mobile hot streak.png")
        result.Add("Vehicles/Power Blue Gold Rusher", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Land Vehicles\fruitsnack's power blue gold rusher.png")
        result.Add("Vehicles/Dark Sea Shadow", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Sea Vehicles\fruitsnack's dark sea shadow.png")
        result.Add("Vehicles/Nitro Soda Skimmer", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Sea Vehicles\fruitsnack's nitro soda skimmer.png")
        result.Add("Vehicles/Power Blue Splatter Splasher", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Sea Vehicles\fruitsnack's power blue splatter splasher.png")
        result.Add("Vehicles/Spring Ahead Dive Bomber", "SSC\6) In-Game Variant Vehicles\1) In-Game Variants\Sea Vehicles\fruitsnack's spring ahead dive bomber.png")
        result.Add("Vehicles/Legendary Sun Runner", "SSC\6) In-Game Variant Vehicles\2) Legendaries\fruitsnack's legendary sun runner.png")
        result.Add("Swap Force/Template Legacy", "UNSORTED\TemplateLegacy.png")
        result.Add("Swap Force/Template Template Top", "UNSORTED\TemplateTemplateTop.png")
        result.Add("Swap Force/Template Template Bottom", "UNSORTED\TemplateTemplateBottom.png")
        Return result
    End Function

    Friend Shared Function RelativePath(game As String, name As String) As String
        Dim path As String = Nothing
        paths.TryGetValue(game & "/" & name, path)
        Return path
    End Function

    'Artwork labels follow the scanned ID/variantId; they are never used as write data.
    Friend Shared Function CrystalName(id As Integer, variantId As Integer, fallback As String) As String
        Dim found As CrystalArtwork = crystals.FirstOrDefault(Function(item) item.Id = id AndAlso item.VariantId = variantId)
        If found IsNot Nothing Then Return found.Name
        'Older custom dumps exchanged the unreleased Fire/Light Angel deco bytes.
        If id = 685 AndAlso variantId = &H5201 Then Return "Fire Angel"
        If id = 689 AndAlso variantId = &H5203 Then Return "Light Angel"
        Return fallback
    End Function

    Friend Shared Function Sections(mode As ArtworkBrowserMode) As List(Of ArtworkBrowserSection)
        Dim result As New List(Of ArtworkBrowserSection)()
        Select Case mode
            Case ArtworkBrowserMode.Crystals
                For Each element As String In New String() {"Air", "Dark", "Earth", "Fire", "Life", "Light", "Magic", "Tech", "Undead", "Water"}
                    Dim names As String() = crystals.Where(Function(item) item.Element = element).
                        Select(Function(item) item.Name).OrderBy(Function(name) name, StringComparer.OrdinalIgnoreCase).ToArray()
                    result.Add(New ArtworkBrowserSection(element & " Crystals", "Imaginators Crystals", names, ElementColor(element)))
                Next
            Case ArtworkBrowserMode.Vehicles
                Dim names As List(Of String) = FigureGallery.Names("Vehicles")
                If Not names.Contains("Legendary Sun Runner") Then names.Add("Legendary Sun Runner")
                For Each terrain As String In New String() {"Land", "Water", "Sky"}
                    Dim selected As String() = names.Where(Function(name) VehicleTerrain(name) = terrain).
                        OrderBy(Function(name) name, StringComparer.OrdinalIgnoreCase).ToArray()
                    result.Add(New ArtworkBrowserSection(terrain & " Vehicles", "Vehicles", selected,
                        If(terrain = "Land", ElementColor("Earth"), If(terrain = "Water", ElementColor("Water"), ElementColor("Air")))))
                Next
            Case Else
                For Each game As String In FigureGallery.Games
                    result.Add(New ArtworkBrowserSection(game, game, FigureGallery.Names(game), GameColor(game)))
                Next
        End Select
        Return result
    End Function

    Private Shared Function VehicleTerrain(name As String) As String
        For Each model As String In New String() {"Buzz Wing", "Clown Cruiser", "Jet Stream", "Sky Slicer", "Stealth Stinger", "Sun Runner"}
            If name.EndsWith(model, StringComparison.OrdinalIgnoreCase) Then Return "Sky"
        Next
        For Each model As String In New String() {"Dive Bomber", "Reef Ripper", "Sea Shadow", "Soda Skimmer", "Splatter Splasher"}
            If name.EndsWith(model, StringComparison.OrdinalIgnoreCase) Then Return "Water"
        Next
        Return "Land"
    End Function

    'Element palette refined by the user; Land Vehicles shares Earth's brown.
    Private Shared Function ElementColor(element As String) As Color
        Select Case element
            Case "Water" : Return Color.FromArgb(6, 117, 200)
            Case "Earth" : Return Color.FromArgb(126, 85, 48)
            Case "Undead" : Return Color.FromArgb(139, 145, 160)
            Case "Tech" : Return Color.FromArgb(239, 157, 24)
            Case "Dark" : Return Color.FromArgb(8, 15, 63)
            Case "Fire" : Return Color.FromArgb(211, 0, 0)
            Case "Air" : Return Color.FromArgb(63, 244, 255)
            Case "Life" : Return Color.FromArgb(9, 225, 100)
            Case "Magic" : Return Color.FromArgb(147, 66, 192)
            Case "Light" : Return Color.FromArgb(255, 255, 142)
            Case Else : Return Color.Gray
        End Select
    End Function

    Friend Shared Function HeaderInk(color As Color) As Color
        Return If(CInt(color.R) * 299 + CInt(color.G) * 587 + CInt(color.B) * 114 < 140000, Color.White, Color.Black)
    End Function

    Private Shared Function GameColor(game As String) As Color
        Select Case game
            Case "Spyro's Adventure" : Return Color.FromArgb(0, 255, 26)
            Case "Giants" : Return Color.FromArgb(253, 155, 20)
            Case "Swap Force" : Return Color.FromArgb(82, 207, 224)
            Case "Trap Team" : Return Color.FromArgb(254, 60, 57)
            Case "SuperChargers" : Return Color.FromArgb(249, 240, 0)
            Case "Imaginators" : Return Color.FromArgb(18, 33, 123)
            Case Else : Return Color.FromArgb(188, 96, 222)
        End Select
    End Function
End Class
