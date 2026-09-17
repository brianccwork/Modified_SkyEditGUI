Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions

Friend NotInheritable Class FigureArtwork
    Private ReadOnly root As String
    Private ReadOnly paths As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly editionPaths As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)

    'Finds the Images directory and indexes PNG paths for exact and edition-insensitive matching.
    'This has bugs that I will come back to fix hopefully idk.
    Friend Sub New()
        Dim directory As DirectoryInfo = New DirectoryInfo(Application.StartupPath)
        For depth As Integer = 0 To 4
            If directory Is Nothing Then Exit For
            Dim candidate As String = Path.Combine(directory.FullName, "Images")
            If IO.Directory.Exists(candidate) Then
                root = candidate
                Exit For
            End If
            directory = directory.Parent
        Next
        If root Is Nothing Then Return
        Try
            For Each file As String In IO.Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories)
                Dim relative As String = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                Dim game As String = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)(0)
                Dim key As String = game & "/" & Normalize(Path.GetFileNameWithoutExtension(file))
                If Not paths.ContainsKey(key) Then paths(key) = New List(Of String)()
                paths(key).Add(file)
                Dim editionKey As String = game & "/" & WithoutEdition(Normalize(Path.GetFileNameWithoutExtension(file)))
                If Not editionPaths.ContainsKey(editionKey) Then editionPaths(editionKey) = New List(Of String)()
                editionPaths(editionKey).Add(file)
            Next
        Catch ex As IOException
            DeviceManagement.DebugWrite("Artwork index: " & ex.Message)
        Catch ex As UnauthorizedAccessException
            DeviceManagement.DebugWrite("Artwork index: " & ex.Message)
        End Try
    End Sub

    'Normalizes artwork names, spelling differences, and word order for filename matching.
    '(At first I wasnt doing this, but idk anymore, ran into too many bugs so i just had this done)
    Friend Shared Function Normalize(name As String) As String
        Dim value As String = name.ToLowerInvariant()
        value = Regex.Replace(value.Normalize(System.Text.NormalizationForm.FormD), "\p{Mn}", "")
        value = Regex.Replace(value, "^(sobersu|fruitsnack)'s\s+", "")
        value = Regex.Replace(value, "_logo(?:_v\d+)?$", "")
        value = Regex.Replace(value, "_(giant|mini)$", "")
        value = Regex.Replace(value, "_lc$", " lightcore")
        value = Regex.Replace(value, "_series(\d+)$", " series $1")
        value = value.Replace("(employee edition)", "").Replace("splitfire", "spitfire")
        value = value.Replace("crystal clear starcast", "clear starcast")
        value = value.Replace("flarewolf", "flare wolf")
        value = Regex.Replace(value, "\((top|bottom)\)", "")
        value = Regex.Replace(value, "[^a-z0-9]+", " ").Trim()
        'Hyphens and apostrophes 
        value = value.Replace("jet vac", "jetvac").Replace("dive clops", "diveclops")
        value = value.Replace("eye brawl", "eyebrawl").Replace("ro bow", "robow")
        Return String.Join(" ", value.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries).OrderBy(Function(word) word, StringComparer.Ordinal))
    End Function

    'Removes series numbers and LightCore words for a secondary artwork lookup.
    Private Shared Function WithoutEdition(name As String) As String
        'Some of the more unqiue figures like the lightcore or series variants
        Return String.Join(" ", name.Split(" "c).Where(Function(word) word <> "series" AndAlso word <> "lightcore" AndAlso Not Regex.IsMatch(word, "^\d+$")))
    End Function

    Private Shared ReadOnly TrapPaths As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"Chompy Mage", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Chompy_Mage_Villain_Icon_coin_300dpi.png"},
        {"Dr. Krankcase", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Dr._Krankcase_Villain_Icon_coin_300dpi.png"},
        {"Wolfgang", "STT\7) Traps\3) Trappable Villains\Undead\Sobersu's Wolfgang_Villain_Icon_coin_300dpi.png"},
        {"Chef Pepper Jack", "STT\7) Traps\3) Trappable Villains\Fire\Sobersu's Chef_Pepper_Jack_Villain_Icon_coin_300dpi.png"},
        {"Nightshade", "STT\7) Traps\3) Trappable Villains\Dark\Sobersu's Nightshade_Villain_Icon_coin_300dpi.png"},
        {"Luminous", "STT\7) Traps\3) Trappable Villains\Light\Sobersu's Luminous_Villain_Icon_coin_300dpi.png"},
        {"Golden Queen", "STT\7) Traps\3) Trappable Villains\Earth\Sobersu's Golden_Queen_Villain_Icon_coin_300dpi.png"},
        {"Dreamcatcher", "STT\7) Traps\3) Trappable Villains\Air\Sobersu's Dreamcatcher_Villain_Icon_coin_300dpi.png"},
        {"Gulper", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Gulper_Villain_Icon_coin_300dpi.png"},
        {"Kaos", "STT\7) Traps\3) Trappable Villains\Kaos\Sobersu's Kaos_Villain_Icon_coin_300dpi.png"},
        {"Cuckoo Clocker", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Cuckoo_Clocker_Villain_Icon_coin_300dpi.png"},
        {"Buzzer Beak", "STT\7) Traps\3) Trappable Villains\Air\Sobersu's Buzzer_Beak_Villain_Icon_coin_300dpi.png"},
        {"Shield Shredder", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Shield_Shredder_Villain_Icon_coin_300dpi.png"},
        {"Cross Crow", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Cross_Crow_Villain_Icon_coin_300dpi.png"},
        {"Bone Chompy", "STT\7) Traps\3) Trappable Villains\Undead\Sobersu's Bone_Chompy_Villain_Icon_coin_300dpi.png"},
        {"Brawl and Chain", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Brawl_&_Chain_Villain_Icon_coin_300dpi.png"},
        {"Bomb Shell", "STT\7) Traps\3) Trappable Villains\Magic\Sobersu's Bomb_Shell_Villain_Icon_coin_300dpi.png"},
        {"Masker Mind", "STT\7) Traps\3) Trappable Villains\Undead\Sobersu's Masker_Mind_Villain_Icon_coin_300dpi.png"},
        {"Chill Bill", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Chill_Bill_Villain_Icon_coin_300dpi.png"},
        {"Sheep Creep", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Sheep_Creep_Villain_Icon_coin_300dpi.png"},
        {"Shrednaught", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Shrednaught_Villain_Icon_coin_300dpi.png"},
        {"Chomp Chest", "STT\7) Traps\3) Trappable Villains\Earth\Sobersu's Chomp_Chest_Villain_Icon_coin_300dpi.png"},
        {"Broccoli Guy", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Broccoli_Guy_Villain_Icon_coin_300dpi.png"},
        {"Rage Mage", "STT\7) Traps\3) Trappable Villains\Magic\Sobersu's Rage_Mage_Villain_Icon_coin_300dpi.png"},
        {"Lob Goblin", "STT\7) Traps\3) Trappable Villains\Light\Sobersu's Lob_Goblin_Villain_Icon_coin_300dpi.png"},
        {"Chompy", "STT\7) Traps\3) Trappable Villains\Life\Sobersu's Chompy_Villain_Icon_coin_300dpi.png"},
        {"Fisticuffs", "STT\7) Traps\3) Trappable Villains\Dark\Sobersu's Fisticuffs_Villain_Icon_coin_300dpi.png"},
        {"Trolling Thunder", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Trolling_Thunder_Villain_Icon_coin_300dpi.png"},
        {"Hood Sickle", "STT\7) Traps\3) Trappable Villains\Undead\Sobersu's Hood_Sickle_Villain_Icon_coin_300dpi.png"},
        {"Bruiser Cruiser", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Bruiser_Cruiser_Villain_Icon_coin_300dpi.png"},
        {"Brawlrus", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Brawlrus_Villain_Icon_coin_300dpi.png"},
        {"Tussle Sprout", "STT\7) Traps\3) Trappable Villains\Earth\Sobersu's Tussle_Sprout_Villain_Icon_coin_300dpi.png"},
        {"Krankenstein", "STT\7) Traps\3) Trappable Villains\Air\Sobersu's Krankenstein_Villain_Icon_coin_300dpi.png"},
        {"Scrap Shooter", "STT\7) Traps\3) Trappable Villains\Fire\Sobersu's Scrap_Shooter_Villain_Icon_coin_300dpi.png"},
        {"Slobber Trap", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Slobber_Trap_Villain_Icon_coin_300dpi.png"},
        {"Grinnade", "STT\7) Traps\3) Trappable Villains\Fire\Sobersu's Grinnade_Villain_Icon_coin_300dpi.png"},
        {"Bad Juju", "STT\7) Traps\3) Trappable Villains\Air\Sobersu's Bad_Juju_Villain_Icon_coin_300dpi.png"},
        {"Blaster-Tron", "STT\7) Traps\3) Trappable Villains\Light\Sobersu's Blaster-Tron_Villain_Icon_coin_300dpi.png"},
        {"Tae Kwon Crow", "STT\7) Traps\3) Trappable Villains\Dark\Sobersu's Tae_Kwon_Crow_Villain_Icon_coin_300dpi.png"},
        {"Pain-Yatta", "STT\7) Traps\3) Trappable Villains\Magic\Sobersu's Pain-Yatta_Villain_Icon_coin_300dpi.png"},
        {"Smoke Scream", "STT\7) Traps\3) Trappable Villains\Fire\Sobersu's Smoke_Scream_Villain_Icon_coin_300dpi.png"},
        {"Eye Five", "STT\7) Traps\3) Trappable Villains\Light\Sobersu's Eye_Five_Villain_Icon_coin_300dpi.png"},
        {"Grave Clobber", "STT\7) Traps\3) Trappable Villains\Earth\Sobersu's Grave_Clobber_Villain_Icon_coin_300dpi.png"},
        {"Threatpack", "STT\7) Traps\3) Trappable Villains\Water\Sobersu's Threatpack_Villain_Icon_coin_300dpi.png"},
        {"Mab Lobs", "STT\7) Traps\3) Trappable Villains\Tech\Sobersu's Mab_Lobs_Villain_Icon_coin_300dpi.png"},
        {"Eye Scream", "STT\7) Traps\3) Trappable Villains\Dark\Sobersu's Eye_Scream_Villain_Icon_coin_300dpi.png"},
        {"Riot Shield Shredder", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's riot shield shredder.png"},
        {"Outlaw Brawl and Chain", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's outlaw brawl & chain.png"},
        {"Steampunk Shrednaught", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's steampunk shrednaught.png"},
        {"Steamed Broccoli Guy", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's steamed broccoli guy.png"},
        {"Rebel Lob Goblin", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's rebel lob goblin.png"},
        {"Red Hot Tussle Sprout", "STT\7) Traps\4) Trappable Villain Variants\fruitsnack's red hot tussle sprout.png"},
        {"Air Hourglass (Tempest Timer)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_HOURGLASS_logo_v2.png"},
        {"Air Jughead (Drafty Decanter)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_JUGHEAD_logo_v2.png"},
        {"Air Screamer (Storm Warning)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_SCREAMER_logo_v2.png"},
        {"Air Snake (Cloudy Cobra)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_COBRA_logo_v2.png"},
        {"Air Sword (Cyclone Sabre)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_SWORD_logo_v2.png"},
        {"Air Toucan (Breezy Bird)", "STT\7) Traps\1) Crystal Traps\Air\Sobersu's air trap_TOUCAN_logo_v2.png"},
        {"Dark Handstand (Ghastly Grimace)", "STT\7) Traps\1) Crystal Traps\Dark\Sobersu's dark trap_HANDSTAND_logo_v2.png"},
        {"Dark Spider (Shadow Spider)", "STT\7) Traps\1) Crystal Traps\Dark\Sobersu's dark trap_SPIDER_logo_v2.png"},
        {"Dark Sword (Dark Dagger)", "STT\7) Traps\1) Crystal Traps\Dark\Sobersu's dark trap_SWORD_logo_v2.png"},
        {"Earth Hammer (Slag Hammer)", "STT\7) Traps\1) Crystal Traps\Earth\Sobersu's earth trap_HAMMER_logo_v2.png"},
        {"Easter Bunny Earth Trap", "STT\7) Traps\2) Crystal Trap Variants\Sobersu's earth trap_EASTER BUNNY_logo_v2.png"},
        {"Earth Hourglass (Dust of Time)", "STT\7) Traps\1) Crystal Traps\Earth\Sobersu's earth trap_HOURGLASS_logo_v2.png"},
        {"Earth Orb (Banded Boulder)", "STT\7) Traps\1) Crystal Traps\Earth\Sobersu's earth trap_ORB_logo_v2.png"},
        {"Earth Totem (Spinning Sandstorm)", "STT\7) Traps\1) Crystal Traps\Earth\Sobersu's earth trap_TOTEM_logo_v2.png"},
        {"Earth Toucan (Rock Hawk)", "STT\7) Traps\1) Crystal Traps\Earth\Sobersu's earth trap_TOUCAN_logo_v2.png"},
        {"Fire Captain's Hat (Spark Spear)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_CAPTAINS HAT_logo_v2.png"},
        {"Fire Scepter (Fire Flower)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_SCEPTER_logo_v2.png"},
        {"Fire Screamer (Scorching Stopper)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_SCREAMER_logo_v2.png"},
        {"Fire Torch (Eternal Flame)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_TORCH_logo_v2.png"},
        {"Fire Totem (Searing Spinner)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_TOTEM_logo_v2.png"},
        {"Fire Yawn (Blazing Belch)", "STT\7) Traps\1) Crystal Traps\Fire\Sobersu's fire trap_YAWN_logo_v2.png"},
        {"Kaos Trap", "STT\7) Traps\1) Crystal Traps\Kaos\Sobersu's kaos trap_KAOS_logo_v2.png"},
        {"Life Hammer (Weed Whacker)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_HUMMER_logo_v2.png"},
        {"Life Snake (Seed Serpent)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_COBRA_logo_v2.png"},
        {"Life Sword (Jade Blade)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_SWORD_logo_v2.png"},
        {"Life Torch (Emerald Energy)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_TORCH_logo_v2.png"},
        {"Life Toucan (Oak Eagle)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_TOUCAN_logo_v2.png"},
        {"Life Yawn (Shrub Shrieker)", "STT\7) Traps\1) Crystal Traps\Life\Sobersu's life trap_YAWN_logo_v2.png"},
        {"Light Owl (Heavenly Hawk)", "STT\7) Traps\1) Crystal Traps\Light\Sobersu's light trap_HAWK_logo_v2.png"},
        {"Light Rocket (Shining Ship)", "STT\7) Traps\1) Crystal Traps\Light\Sobersu's light trap_SHIP_logo_v2.png"},
        {"Light Yawn (Beam Scream)", "STT\7) Traps\1) Crystal Traps\Light\fruitsnack's light trap yawn v2.png"},
        {"Magic Axe (Axe of Illusion)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_AXE_logo_v2.png"},
        {"Magic Hourglass (Arcane Hourglass)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_HOURGLASS_logo_v2.png"},
        {"Magic Log Holder (Biter's Bane)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_LOG HOLDER_logo_v2.png"},
        {"Magic Rocket (Rune Rocket)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_SHIP_logo_v2.png"},
        {"Magic Skull (Sorcerous Skull)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_SKULL_logo_v2.png"},
        {"Magic Totem (Spell Slapper)", "STT\7) Traps\1) Crystal Traps\Magic\Sobersu's magic trap_TOTEM_logo_v2.png"},
        {"Tech Angel (Automatic Angel)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_ANGEL_logo_v2.png"},
        {"Tech Flying Helmet (Makers Mana)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_HELMET_logo_v2.png"},
        {"Tech Hand (Grabbing Gadget)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_HAND_logo_v2.png"},
        {"Tech Handstand (Topsy Techy)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_HANDSTAND_logo_v2.png"},
        {"Tech Scepter (Factory Flower)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_TORCH_logo_v2.png"},
        {"Tech Tiki (Tech Totem)", "STT\7) Traps\1) Crystal Traps\Tech\Sobersu's tech trap_TOTEM_logo_v2.png"},
        {"Undead Axe (Haunted Hatchet)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_AXE_logo_v2.png"},
        {"Undead Captain's Hat (Dream Piercer)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_CAPTAINS HAT_logo_v2.png"},
        {"Undead Hand (Grim Gripper)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_HAND_logo_v2.png"},
        {"Undead Orb (Spirit Sphere)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_ORB_logo_v2.png"},
        {"Undead Skull (Spectral Skull)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_SKULL_logo_v2.png"},
        {"Undead Snake (Spooky Snake)", "STT\7) Traps\1) Crystal Traps\Undead\Sobersu's undead trap_COBRA_logo_v2.png"},
        {"Water Angel (Soaking Staff)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_ANGEL_logo_v2.png"},
        {"Water Axe (Aqua Axe)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_AXE_logo_v2.png"},
        {"Water Flying Helmet (Frost Helm)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_HELMET_logo_v2.png"},
        {"Water Jughead (Flood Flask)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_JUGHEAD_logo_v2.png"},
        {"Water Log Holder (Wet Walter)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_WET WALTER_logo_v2.png"},
        {"Water Tiki (Tidal Tiki)", "STT\7) Traps\1) Crystal Traps\Water\Sobersu's water trap_TOTEM_logo_v2.png"},
        {"Legendary Flood Flask (Water Jughead)", "STT\7) Traps\2) Crystal Trap Variants\Sobersu's water trap_JUGHEAD LEGENDARY_logo_v2.png"},
        {"Legendary Spectral Skull (Undead Skull)", "STT\7) Traps\2) Crystal Trap Variants\Sobersu's undead trap_SKULL LEGENDARY_logo_v2.png"},
        {"Legendary Spirit Sphere (Undead Orb)", "STT\7) Traps\2) Crystal Trap Variants\Sobersu's undead trap_ORB LEGENDARY_logo_v2.png"},
        {"Ultimate Kaos Trap (Dark Edition Variant)", "STT\7) Traps\2) Crystal Trap Variants\Sobersu's kaos trap_KAOS LEGENDARY_logo_v2.png"}
    }

    'Resolves the requested artwork through explicit mappings, indexed names, and the error-image fallback.
    Friend Function Load(gameName As String, figureName As String) As Image
        If gameName = "Traps" AndAlso root IsNot Nothing Then
            Dim relative As String = Nothing
            If TrapPaths.TryGetValue(figureName, relative) Then
                Dim found As Image = TryLoad(Path.Combine(root, relative.Replace("\"c, Path.DirectorySeparatorChar)))
                If found IsNot Nothing Then Return found
            End If
        End If
        Dim folder As String = ""
        Select Case gameName
            Case "Spyro's Adventure" : folder = "SSA"
            Case "Giants" : folder = "SG"
            Case "Swap Force" : folder = "SSF"
            Case "Trap Team", "Traps" : folder = "STT"
            Case "SuperChargers", "Vehicles", "Vehicle" : folder = "SSC"
            Case "Imaginators", "Imaginators Crystals" : folder = "SI"
        End Select
        Dim matches As List(Of String) = Nothing
        If paths.TryGetValue(folder & "/" & Normalize(figureName), matches) AndAlso matches.Count = 1 Then
            Dim found As Image = TryLoad(matches(0))
            If found IsNot Nothing Then Return found
        End If
        Dim normalized As String = Normalize(figureName)
        If Not Regex.IsMatch(normalized, "\b(series|lightcore)\b") AndAlso
           editionPaths.TryGetValue(folder & "/" & normalized, matches) AndAlso matches.Count = 1 Then
            Dim found As Image = TryLoad(matches(0))
            If found IsNot Nothing Then Return found
        End If
        If root IsNot Nothing Then
            For Each fallback As String In New String() {
                Path.Combine(root, "ERROR", "Error_Figure.png"),
                Path.Combine(root, "ERROR) Error_Figure.png")}
                Dim found As Image = TryLoad(fallback)
                If found IsNot Nothing Then Return found
            Next
        End If
        'fallback here
        Return Nothing
    End Function

    'Loads a detached image copy when a candidate artwork file exists and can be opened.
    Private Shared Function TryLoad(path As String) As Image
        Try
            If Not File.Exists(path) Then Return Nothing
            Using stream As FileStream = File.OpenRead(path), source As Image = Image.FromStream(stream)
                Return New Bitmap(source)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is ArgumentException OrElse
                                   TypeOf ex Is OutOfMemoryException OrElse TypeOf ex Is UnauthorizedAccessException
            Return Nothing
        End Try
    End Function
End Class
