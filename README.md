# FortPatcher

This project is designed to modify the FortniteClient-Win64-Shipping executable for two purposes:

1. **Run the executable in headless mode**: Ideal for setting up game servers for hosting.
2. **Apply bug fixes**: Automatically detect the game version and apply the corresponding patch for that version.

The patcher works by detecting the game version and applying byte modifications to the executable based on pre-defined patterns for different versions.

## Features

- **Headless Mode**: Modify the executable to run without any gui or directX requirement (nullrhi).
- **Bug Fixes**: Automatically apply version-specific bug fixes by replacing specific byte patterns in the executable.

## Usage

1. Run the patcher.
2. Select one of the following options:
   - **Option 1**: Modify the executable to run in headless mode (for game server hosting).
   - **Option 2**: Apply bug fixes based on the detected game version.
   
3. The patcher will guide you through selecting the correct executable and will apply the necessary modifications based on your choice.

## How to Contribute

To contribute new patches for different game versions, follow these steps:

1. **Identify the game version**: Determine the version for which you want to create a patch.
2. **Add a new version entry** in the `GetVersionSpecificBytes` function:
   - In the function, create an `else if` block for your version.
   - Replace the periods (`.`) in the version with underscores (`_`).
   - Add the corresponding old and new byte arrays that need to be replaced.

### Example Contribution

Suppose you want to add a patch for version `6.10`. You would modify the `GetVersionSpecificBytes` function as follows:

```csharp
else if (arrayName == "bytesold_6_10")
{
    return new byte[][]
    {
        new byte[] { 0xAA, 0xBB, 0xCC }, // Old byte pattern 1 for version 6.10
        new byte[] { 0xDD, 0xEE, 0xFF }  // Old byte pattern 2 for version 6.10
    };
}
else if (arrayName == "bytesnew_6_10")
{
    return new byte[][]
    {
        new byte[] { 0x11, 0x22, 0x33 }, // New byte pattern 1 for version 6.10
        new byte[] { 0x44, 0x55, 0x66 }  // New byte pattern 2 for version 6.10
    };
}
```

3. **Test your changes** to ensure that the patch is applied correctly for the specified version.
4. **Submit a pull request** with your changes and a brief description of the patches you've added.
