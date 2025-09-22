# Thunder Store Release Instructions

## Package Ready ✅

Your Thunder Store package is ready in the `thunderstore_package/` directory with all required files:

- ✅ `manifest.json` - Thunder Store metadata
- ✅ `README.md` - Thunder Store description
- ✅ `icon.png` - Package icon (256x256 recommended)
- ✅ `CrowbaneCommands.dll` - Built plugin file

## Upload Steps

1. **Create ZIP Package**
   - Navigate to `E:/build/crowbanecommands/thunderstore_package/`
   - Select all files (manifest.json, README.md, icon.png, CrowbaneCommands.dll)
   - Create ZIP archive named `CrowbaneCommands-0.1.10.zip`

2. **Upload to Thunder Store**
   - Go to https://thunderstore.io/c/v-rising/
   - Click "Upload" (requires account)
   - Upload your ZIP file
   - Verify package details match manifest.json

## Package Contents

- **Name**: CrowbaneCommands
- **Version**: 0.1.10
- **Dependencies**:
  - BepInEx-BepInExPack_V_Rising-1.0.0
  - deca-VampireCommandFramework-0.9.7

## License Compliance ✅

- MIT License included
- All audit logging implemented
- No compilation errors
- Ready for public release

## Support

- Repository: Ready - Discord: https://discord.gg/ZnGGfj69zv
  for git commit