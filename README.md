# How to Use: SkyGUI - A Skylander Figure Editor
- Run SkyReader-GUI.exe
- Supports Non-Xbox 360 Portal 
- Types of figures handled:
  - Cores (Gold/XP)
  - Giants (Gold/XP)
  - Swappers (Gold/XP)
  - Trap Masters (Gold/XP)
  - Superchargers (Gold/XP)
  - Chases (Gold/XP)
  - Senseis (Gold/XP with Minor adjustments to process)
  - Vehicles (Gearbits)
  - Traps (Input Villian, Evolved, and Variants!)

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

### Struggling with setting up your Non-Xbox Portal?
Use Zadig --> WinUSB --> Device Manager --> Update Driver [Tutorial inside Program, or online Youtube video work as well!]

# Skylander Editor SkyGUI
Everything in this first section of the README contains changes, additions, and notes for this modified version of the project. Below this section is hegyak's original README.

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

ALWAYS KEEP A VALID BACKUP OF YOUR SKYLANDER BEFORE MODIFYING DATA

This program only works with Non-Xbox Skylander Portals

## Resources and Information Used for Development
https://github.com/Texthead1/Riches  
https://github.com/NefariousTechSupport/Runes  
https://github.com/xMcacutt/SkanderNET  
https://xmcacutt.github.io/SkanderNET/docs/introduction.html  
https://github.com/hegyak/SkyEditGUI/tree/master  
https://github.com/skylandersNFC/Skylanders-GUI-Tool  
https://www.pyrofersprojects.com/blog/skylanders-gui-tool/
https://github.com/Texthead1/Skylander-IDs
https://github.com/Texthead1/Revolve
https://github.com/Texthead1/Skylanders-Portal-IDs

### Special Thanks:
- hegyak
- Texthead1
- NefariousTechSupport
- skylandersNFC
- XMcacutt
- pyrofersprojects

### Source Credits:
- Skylandeer - Shattered Trap Team background
- Motion Vision - Animated Cloud background
- Activision Skylanders - Icons, Logo, and UI details

## MASSIVE REDESEIGN AND UPDATE 9/16/2026
- NEW Skylanders UI
  - Added a main menu with Skylanders XP and Gold Modifier, Traps, Vehicles, Advanced, and Imaginators. Advanced opens the original Developer editor. The separate Imaginators page is still in development.
    - Note that the Advanced page will no longer be maintained by me, but will be used for testing scenarios. Use at your own risk.
  - Added the Linotype Markin LT font.
  - Figure artwork is displayed in a circle above the Portal of Power.
- Simplified Skylanders XP and Gold Modifier so that it is easier to use for general users.
- Expandable Preview lists to see possible figures.

- Regular Character Gold and XP Save Updates
  - Big thanks to NefariousTechSupport, reading through the Runes project and using the given context from my research has helped tremendously at understanding and setting up a consistently working Gold and XP save.

- Swap Force Character Handling
  - The simplified editor supports reading an assembled Swap Force figure by reading the bottom half and then the top half during one read operation.
  - Gold, XP, and Level edits target the top half. Bottom-only reads and ambiguous combinations of figures are rejected.
  - The Developer/Advanced editor retains its separate first/second figure controls. The simplified editor resolves the target portal slot again before saving.

- Trap Villain Workshop
  - Traps page contains trap reading, and identification of empty traps or their current villains.
  - Added evolution and de-evolution editing using the Revolve project as reference.
  - Added supported villain variants. Variant editing is available only for villains with a supported variant.

- Simplified Vehicle Gearbits Modifier
  - Gearbits can be set from 0 to 33,000.
  - The simplified vehicle page writes and verifies the changes directly.

- Help Portal and Connection Troubleshooting
  - Added a compact Help Portal button at the top left. The help page includes an official Zadig website button and step-by-step portal driver instructions.
  - Connection failure messages remind users that only non-Xbox portals are supported and point them to Help Portal.

### Older Changes
- Sensei Gold and Level modifier now accepts Heartbreaker Buckshot

Gold and Level modifiers for Instant Super Shot Stealth Elf, Instant Dive Clops, Instant Spitfire, Instant Dive Clops, Instant Stealth Stinger, Instant Hot Streak, and VVindup.
  - After scanning in these Skylanders, once the Header and Serial areas are both green, both Gold and Level may be edited.

- Sensei Gold and Level Modifier
  - Senseis can now be safely detected, read, modified, and written back through the Portal of Power.
  - The editor currently supports modifying Sensei Gold and Level/EXP values.
  - READ THIS: Senseis must first be initialized in-game before editing. Place the Sensei in Skylanders Imaginators, walk around, and collect at least 1 gold so the game saves valid Sensei data to the NFC tag.
  - After the Sensei has been initialized in-game, read it in the editor again. Once the checksum/status boxes are green, Gold and Level can be modified and written successfully.

- Vehicle Gearbits Modifier
  - After connecting the portal and configuring the correct driver so the application can detect it, place a vehicle on the Portal of Power and read the first figure.
  - Once the vehicle has been read successfully and the relevant checksum/status boxes are green, open the Vehicles editor.
  - You can then modify the Gearbits value to any amount from 0 to 33,000.
  - After making your change, close the vehicle editor and choose Yes when prompted to apply the changes.
  - Finally, use the main form's write option to write the first figure back to the portal. If the write succeeds, the vehicle should now contain the new Gearbits amount.

- Portal Connection Debug Log
  - A debug log was added to help diagnose portal detection and connection issues.
  - When connecting the portal, you may see a popup mentioning the debug log. This popup is only informational.
  - The log records details about HID/device detection, portal connection attempts, and how Windows is identifying the device.

## Notes

- The current vehicle-related work is focused specifically on Gearbits editing.
- The vehicle editor changes are intended for vehicles that use the standard SuperChargers vehicle data layout.
- Saving changes in the vehicle editor updates the figure data in memory first. You must still use the main form write option to send the modified data back to the figure on the portal.
- hegyak's original README has been left unchanged, including the TODO list.

---

# SkyEditGUI - Base Project, Now Archived
GUI Editor for MiFare based figures

I am trying to create an All-in-One Editor for a game that uses MiFare based Figures.

Current Features:
Identify Most (but not all) of the Figures that the game supports.

Edit ALL values for Characters. This includes Hat, Name, Gold, Exp/Level, Heroic Challenges, Fairy Path and Hero Points.

Reads in Figures from a NON-XBox 360 USB Game Portal connected to a Windows PC.

Reads in Encrypted Figure Dumps for editing.

Reads in Decrypted Figures Dumps for editing.

Writes Decrypted Figure Dumps and Encrypted Figure Dumps by user selection in a file menu.

Can write data back to a Figure on the Non-XBox 360 Game Portal.

The Figure's serial number can be randomized but should ONLY be used on a MiFare S50/Magic Chinese card. NOT on a figure on the Portal. See below for Known issues with this.

Can edit Figure's Character ID and Variant/Generation ID. With Exceptions, as noted below.

Supports Maxlander Tags for reading/editing except for the 2KB Swap Force Dumps.

Corrects the Checksums for Figures when writing to the Portal, or back to your PC.  With known issues, as explained below.

Writes data to both Data blocks for the Figure. So your edited data is there, no matter which data block the game reads from.


To do:
The Program can not identify All Imaginator Crystals. The program does recognize Imaginator crystals but according to the Skylander Wiki, I am missing some dumps still for identification purposes.

The Fairy Skills writing may write all skills as obtained for that path, and not allow the user a more fine grained selection of skills.

Traps have the wrong Checksum being written.  They use a unique Type Vs. Most other figures.

Vehicles are mostly editable except for Mods.  More Information is required for me to determine what bytes go where.

Imaginators Crystals are completely unknown for me. They should have the same header bytes, but beyond that, their data structure is unknown. Editing these, would be the most sought after feature to have but would require a LOT of effort to fully impliment. Resetting a Creator Crystal should be easy to implement though.

Reading and Writing via an Xbox 360 portal would be a nice feature to have but would require more effort for the user as well as myself to make that work. This is due to the Drivers for the Xbox 360 portal being unsigned. There is a way to fix this but it does require more technical know how for the end user as well. Also, the Xbox 360 portal will require more commands sent via USB compared to a Non-Xbox 360 portal. This is a low priority feature.


Known Editor Defects/issues:
The Program can NOT write Header Data to a blank MiFare (S50/Magic Chinese) Card via a the Game USB Portal. This is NOT a fault of the program, but how the Portal itself works/behaves. The Portal can be told "Write to the Header Blocks of the Figure/Card" but those commands are ignored by the portal itself.

This program does not write to external hardware like the ACR122u. It's not supported by this program.

Not all Figures are correctly identified. If you have a figure that is not identified by the editor, please let me know what it is supposed to be, and make sure your dump is valid/good.

Hats are editable on a single game basis even though you can set a hat for each game from Superchargers going backward to Adventures on a single figure.

If a Figure's Nickname, contains a NON-ASCII Character or characters, then the program will assume that the dump is encrypted. Even if the figure is not encrypted.

Attempting to Load a Maxander 2KB dump for a Swap Force Character's Top and Bottom Half, will not work. The program even mention that it thinks you are loading a Maxlander 2KB Dump file.

The Figure Detection method is not a thing. Loading any 1KB file will be treated as a tag. Any 2KB File is thought of as a Maxlander Dump for a Swapforce Character top and bottom half. But this does not create a valid Tag from a blank 1KB File.

Gen 6 figures have Special Protections and can NOT change the Figure ID/Variant ID as that WILL brick/break the figure or create an invalid figure on a MiFare card if you attempt to write the changed Figure.
