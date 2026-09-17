Option Strict On
Option Explicit On
Friend NotInheritable Class TrapCatalog
    Friend Shared ReadOnly Names As String() = {"Empty", "Chompy Mage", "Dr. Krankcase", "Wolfgang", "Chef Pepper Jack", "Nightshade", "Luminous", "Golden Queen", "Dreamcatcher", "Gulper", "Kaos", "Cuckoo Clocker", "Buzzer Beak", "Shield Shredder", "Cross Crow", "Bone Chompy", "Brawl and Chain", "Bomb Shell", "Masker Mind", "Chill Bill", "Sheep Creep", "Shrednaught", "Chomp Chest", "Broccoli Guy", "Rage Mage", "Lob Goblin", "Chompy", "Fisticuffs", "Trolling Thunder", "Hood Sickle", "Bruiser Cruiser", "Brawlrus", "Tussle Sprout", "Krankenstein", "Scrap Shooter", "Slobber Trap", "Grinnade", "Bad Juju", "Blaster-Tron", "Tae Kwon Crow", "Pain-Yatta", "Smoke Scream", "Eye Five", "Grave Clobber", "Threatpack", "Mab Lobs", "Eye Scream"}
    Friend Shared ReadOnly Elements As Integer() = {0, 217, 214, 213, 215, 218, 219, 216, 212, 211, 220, 217, 212, 217, 211, 213, 211, 210, 213, 211, 217, 214, 216, 217, 210, 219, 217, 218, 214, 213, 214, 214, 216, 212, 215, 211, 215, 212, 219, 218, 210, 215, 219, 216, 211, 214, 218}
    Friend Shared ReadOnly Variants As New Dictionary(Of Integer, String) From {{13, "Riot Shield Shredder"}, {16, "Outlaw Brawl and Chain"}, {21, "Steampunk Shrednaught"}, {23, "Steamed Broccoli Guy"}, {25, "Rebel Lob Goblin"}, {32, "Red Hot Tussle Sprout"}}
    'Returns the element label associated with the supplied catalog ID.
    Friend Shared Function ElementName(id As Integer) As String
        If id < 210 OrElse id > 220 Then Return "Unknown"
        Return {"Magic", "Water", "Air", "Undead", "Tech", "Fire", "Earth", "Life", "Dark", "Light", "Kaos"}(id - 210)
    End Function
    'Returns the primary or supported variant villain name for the stored ID.
    Friend Shared Function VillainName(id As Integer, variantValue As Boolean) As String
        If id < 0 OrElse id >= Names.Length Then Return "Unknown villain (" & id & ")"
        If variantValue AndAlso Variants.ContainsKey(id) Then Return Variants(id)
        Return Names(id)
    End Function
    'Checks whether a villain belongs to the scanned trap's element.
    Friend Shared Function Compatible(id As Integer, trapId As Integer) As Boolean
        Return id > 0 AndAlso id < Elements.Length AndAlso Elements(id) = trapId
    End Function
    Private Shared ReadOnly Shapes As New Dictionary(Of String, String) From {
        {"212:12302", "Air Hourglass (Tempest Timer)"},
        {"212:12294", "Air Jughead (Drafty Decanter)"},
        {"212:12305", "Air Screamer (Storm Warning)"},
        {"212:12304", "Air Snake (Cloudy Cobra)"},
        {"212:12312", "Air Sword (Cyclone Sabre)"},
        {"212:12291", "Air Toucan (Breezy Bird)"},
        {"218:12314", "Dark Handstand (Ghastly Grimace)"},
        {"218:12308", "Dark Spider (Shadow Spider)"},
        {"218:12312", "Dark Sword (Dark Dagger)"},
        {"216:12298", "Earth Hammer (Slag Hammer)"},
        {"216:12314", "Easter Bunny Earth Trap"},
        {"216:12302", "Earth Hourglass (Dust of Time)"},
        {"216:12292", "Earth Orb (Banded Boulder)"},
        {"216:12306", "Earth Totem (Spinning Sandstorm)"},
        {"216:12291", "Earth Toucan (Rock Hawk)"},
        {"215:12311", "Fire Captain's Hat (Spark Spear)"},
        {"215:12297", "Fire Scepter (Fire Flower)"},
        {"215:12305", "Fire Screamer (Scorching Stopper)"},
        {"215:12293", "Fire Torch (Eternal Flame)"},
        {"215:12306", "Fire Totem (Searing Spinner)"},
        {"215:12315", "Fire Yawn (Blazing Belch)"},
        {"220:12318", "Kaos Trap"},
        {"217:12298", "Life Hammer (Weed Whacker)"},
        {"217:12304", "Life Snake (Seed Serpent)"},
        {"217:12312", "Life Sword (Jade Blade)"},
        {"217:12293", "Life Torch (Emerald Energy)"},
        {"217:12289", "Life Toucan (Oak Eagle)"},
        {"217:12315", "Life Yawn (Shrub Shrieker)"},
        {"219:12303", "Light Owl (Heavenly Hawk)"},
        {"219:12309", "Light Rocket (Shining Ship)"},
        {"219:12315", "Light Yawn (Beam Scream)"},
        {"210:12299", "Magic Axe (Axe of Illusion)"},
        {"210:12302", "Magic Hourglass (Arcane Hourglass)"},
        {"210:12290", "Magic Log Holder (Biter's Bane)"},
        {"210:12309", "Magic Rocket (Rune Rocket)"},
        {"210:12296", "Magic Skull (Sorcerous Skull)"},
        {"210:12306", "Magic Totem (Spell Slapper)"},
        {"214:12295", "Tech Angel (Automatic Angel)"},
        {"214:12310", "Tech Flying Helmet (Makers Mana)"},
        {"214:12300", "Tech Hand (Grabbing Gadget)"},
        {"214:12314", "Tech Handstand (Topsy Techy)"},
        {"214:12297", "Tech Scepter (Factory Flower)"},
        {"214:12289", "Tech Tiki (Tech Totem)"},
        {"213:12299", "Undead Axe (Haunted Hatchet)"},
        {"213:12311", "Undead Captain's Hat (Dream Piercer)"},
        {"213:12300", "Undead Hand (Grim Gripper)"},
        {"213:12292", "Undead Orb (Spirit Sphere)"},
        {"213:12296", "Undead Skull (Spectral Skull)"},
        {"213:12304", "Undead Snake (Spooky Snake)"},
        {"211:12295", "Water Angel (Soaking Staff)"},
        {"211:12299", "Water Axe (Aqua Axe)"},
        {"211:12310", "Water Flying Helmet (Frost Helm)"},
        {"211:12294", "Water Jughead (Flood Flask)"},
        {"211:12290", "Water Log Holder (Wet Walter)"},
        {"211:12289", "Water Tiki (Tidal Tiki)"},
        {"211:13318", "Legendary Flood Flask (Water Jughead)"},
        {"213:13320", "Legendary Spectral Skull (Undead Skull)"},
        {"213:13316", "Legendary Spirit Sphere (Undead Orb)"},
        {"220:13599", "Ultimate Kaos Trap (Dark Edition Variant)"}
    }

    'Combines the trap ID and variant value into a recognized trap name or fallback label.
    Friend Shared Function TrapName(id As Integer, variantValue As Integer) As String
        Dim key As String = id & ":" & variantValue
        If Shapes.ContainsKey(key) Then Return Shapes(key)
        Return ElementName(id) & " Trap (variant " & variantValue & ")"
    End Function
End Class
