# FXUplift
### A Halo 2 to Halo 3 Effect Converter

## Requirements
* Requires [.NET 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)

## Translates:
- Markers
- Events
- Parts
- Particle systems
- Particle physics
- Particle tags
- Function data
- Function curves (function graphs)
- ALL data relevant to the above tag blocks

## Usage
1. The executable can be placed anywhere, provided you copy `ManagedBlam.dll` from `H3EK\bin` and place it alongside `FXUplift.exe`. Or put `FXUplift.exe` directly in `H3EK\bin`.
2. Run `FXUplift.exe`. Follow the instructions to select single-tag or bulk-tag modes.
3. Input the requested file/folder paths. E.g. `C:\Program Files (x86)\Steam\steamapps\common\H2EK\tags\effects\scenery\smoke\smoke_thick_large.effect` for an effect tag, or `C:\Program Files (x86)\Steam\steamapps\common\H3EK` for your H3EK path.
4. The program will now convert the tag data automatically. The output tag(s) can be found in `H3EK\tags\halo_2` plus the original H2 tag path, e.g. `H3EK\tags\halo_2\effects\scenery\smoke\smoke_thick_large.effect`.

## Notes
- Bulk mode will convert ALL Halo 2 effect tags found in the H2 tags directory. This may take around 3-4 minutes to complete.
- Converted effects should require little to no manual adjustment.
- Make sure the exe is placed alongside `ManagedBlam.dll`, or the program will crash.
- Particle bitmaps are NOT ported. The tag paths are updated to add `halo_2` to the start of the tags-relative path. I would recommend obtaining the source-quality bitmaps [here](http://vaporeon.io/hosted/halo/data/)
- For some particle sprite bitmaps, H3 tool imports them at a different size as it throws away "empty" sections of the image. This can result in a scaling discrepancy for said particle. In such cases, to achieve correct sizing, set the "scale" in the effect emitter to the difference in size. For example a sprite bitmap that imports as 256x256 in H2 but as 256x128 in H3 should have the scale set to 0.5 in the H3 effect tag.

## Legal Disclaimer
This project references functionality provided by ManagedBlam.dll, a component of the official [Halo 3 Editing Kit (H3EK)](https://store.steampowered.com/app/1695791/Halo_3_Mod_Tools__MCC/).
ManagedBlam.dll is not included in this repository. It is proprietary software owned by Microsoft / 343 Industries / Halo Studios / Bungie and is provided only as part of the officially released Halo 3 modding tools.

To build or run this tool, users must obtain ManagedBlam.dll through legitimate access to the Halo 3 Editing Kit.
No copyrighted or proprietary assets from the H3EK are distributed with this codebase, and this project is not affiliated with or endorsed by Microsoft, 343 Industries, or Bungie.

All Halo-related trademarks, assets, and toolsets are the property of their respective owners.

## Credits
- [PepperMan](https://github.com/Pepper-Man) - code
- Microsoft/343 Industries/Halo Studios/Bungie - ManagedBlam.dll assembly
- [Alexis Jonsson | WyvernZu](https://github.com/AlexisJonsson) - icon art
