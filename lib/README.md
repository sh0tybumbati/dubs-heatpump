# Reference Assemblies

This directory should contain the RimWorld and mod DLLs needed for compilation:

## Required files for local builds:

### From RimWorld:
- `Assembly-CSharp.dll` - Main RimWorld assembly
- `UnityEngine.CoreModule.dll` - Unity engine core

**Location (Windows/Steam):**
```
C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\
```

**Location (Linux/Steam):**
```
~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/
```

### From Dub's Bad Hygiene:
- `DubsBadHygiene.dll`

**Location (Windows/Steam Workshop):**
```
C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\836308268\Current\Assemblies\
```

**Location (Linux/Steam Workshop):**
```
~/.steam/steam/steamapps/workshop/content/294100/836308268/Current/Assemblies/
```

## For GitHub Actions:

GitHub Actions will attempt to download or create stub assemblies automatically. No manual intervention needed.

## Gitignore:

These DLLs are not committed to the repository (excluded in .gitignore) as they are copyrighted materials.
