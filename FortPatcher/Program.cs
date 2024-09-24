using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

class Program
{
    static string logFilePath = "FortnitePatcherLog.txt";

    [STAThread]
    static void Main()
    {
        if (File.Exists(logFilePath))
        {
            File.Delete(logFilePath);
        }

        string folderPath = OpenFolderDialog();
        if (string.IsNullOrEmpty(folderPath))
        {
            Log("No folder selected.");
            ExitProgram();
            return;
        }

        string fortniteExePath = Path.Combine(folderPath, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe");

        if (!File.Exists(fortniteExePath))
        {
            Log("The selected path does not contain a valid Fortnite application.");
            ExitProgram();
            return;
        }

        bool? isVersion5OrHigher = DetectGameVersion(fortniteExePath);

        if (isVersion5OrHigher == null)
        {
            Log("Could not detect game version.");
            ExitProgram();
            return;
        }

        string versionMethod = isVersion5OrHigher.Value ? "5.00 and higher" : "4.5 and lower";
        Log($"Using method: {versionMethod}");

        ModifyHexValues(fortniteExePath, isVersion5OrHigher.Value);

        ExitProgram();
    }

    static void ExitProgram()
    {
        Log("Press any key to exit.");
        Console.ReadKey();
    }

    static void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(logFilePath, message + Environment.NewLine);
    }

    [DllImport("ole32.dll")]
    static extern void CoTaskMemFree(IntPtr ptr);

    static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    static readonly Guid IID_IFileDialog = new Guid("42f85136-db7e-439c-85f1-e4075d135fc8");

    const uint FOS_PICKFOLDERS = 0x00000020;

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileDialog
    {
        [PreserveSig] int Show(IntPtr hwndOwner);
        void SetFileTypes();
        void SetFileTypeIndex();
        void GetFileTypeIndex();
        void Advise();
        void Unadvise();
        void SetOptions(uint options);
        void GetOptions(out uint options);
        void SetDefaultFolder();
        void SetFolder();
        void GetFolder();
        void GetCurrentSelection();
        void SetFileName();
        void GetFileName();
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel();
        void SetFileNameLabel();
        void GetResult(out IShellItem ppsi);
        void AddPlace();
        void SetDefaultExtension();
        void Close();
        void SetClientGuid();
        void ClearClientData();
        void SetFilter();
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem
    {
        void BindToHandler();
        void GetParent();
        void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
        void GetAttributes();
        void Compare();
    }

    enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000,
    }

    public static string OpenFolderDialog()
    {
        var dialog = (IFileDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileOpenDialog));
        dialog.SetOptions(FOS_PICKFOLDERS);
        dialog.SetTitle("Select folder with FortniteGame and Engine");

        int hr = dialog.Show(IntPtr.Zero);
        if (hr < 0)
        {
            return null;
        }

        dialog.GetResult(out IShellItem item);
        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr ptrPath);
        string path = Marshal.PtrToStringAuto(ptrPath);
        CoTaskMemFree(ptrPath);

        return path;
    }

    static bool? DetectGameVersion(string exePath)
    {
        byte[] fileBytes = File.ReadAllBytes(exePath);

        byte[] versionPattern = new byte[]
        {
            0x46, 0x00, 0x6F, 0x00, 0x72, 0x00, 0x74, 0x00, 0x6E, 0x00,
            0x69, 0x00, 0x74, 0x00, 0x65, 0x00, 0x2B, 0x00, 0x52, 0x00,
            0x65, 0x00, 0x6C, 0x00, 0x65, 0x00, 0x61, 0x00, 0x73, 0x00,
            0x65, 0x00
        };

        int patternIndex = IndexOfSequence(fileBytes, versionPattern);

        if (patternIndex == -1)
        {
            return null;
        }

        int versionStartIndex = patternIndex + versionPattern.Length;

        string versionString = ExtractVersionString(fileBytes, versionStartIndex);

        if (string.IsNullOrEmpty(versionString))
        {
            return null;
        }

        Log($"Detected Version: {versionString}");

        return IsVersion5OrHigher(versionString);
    }

    static string ExtractVersionString(byte[] fileBytes, int startIndex)
    {
        StringBuilder versionString = new StringBuilder();

        for (int i = startIndex; i < fileBytes.Length; i += 2)
        {
            byte currentByte = fileBytes[i];

            if ((currentByte >= 0x30 && currentByte <= 0x39) || currentByte == 0x2E)
            {
                versionString.Append((char)currentByte);
            }
            else if (currentByte == 0x2D)
            {
                continue;
            }
            else if (currentByte != 0x00)
            {
                break;
            }
        }

        return versionString.ToString();
    }

    static bool IsVersion5OrHigher(string versionString)
    {
        string[] versionParts = versionString.Split('.');
        if (versionParts.Length > 0 && int.TryParse(versionParts[0], out int majorVersion))
        {
            return majorVersion > 4;
        }
        return false;
    }

    static void ModifyHexValues(string exePath, bool isVersion5OrHigher)
    {
        byte[] fileBytes = File.ReadAllBytes(exePath);

        byte[][] oldHexes5Plus = new byte[][]
        {
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x6F, 0x00, 0x62, 0x00, 0x65, 0x00 },
            new byte[] { 0x2D, 0x00, 0x77, 0x00, 0x69, 0x00, 0x6E, 0x00, 0x64, 0x00, 0x6F, 0x00, 0x77, 0x00, 0x65, 0x00, 0x64 },
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x6F, 0x00, 0x65, 0x00, 0x70, 0x00, 0x69, 0x00, 0x63, 0x00, 0x70, 0x00, 0x6F, 0x00, 0x72, 0x00, 0x74, 0x00, 0x61, 0x00, 0x6C, 0x00 },
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x6F, 0x00, 0x67, 0x00, 0x61, 0x00, 0x6D, 0x00, 0x65, 0x00, 0x70, 0x00, 0x61, 0x00, 0x64, 0x00, 0x73 }
        };

        byte[][] newHexes5Plus = new byte[][]
        {
            new byte[] { 0x2D, 0x00, 0x6C, 0x00, 0x6F, 0x00, 0x67, 0x00, 0x20, 0x00 },
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x6F, 0x00, 0x73, 0x00, 0x6F, 0x00, 0x75, 0x00, 0x6E, 0x00, 0x64, 0x00, 0x20 },
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x6F, 0x00, 0x73, 0x00, 0x70, 0x00, 0x6C, 0x00, 0x61, 0x00, 0x73, 0x00, 0x68, 0x00, 0x20, 0x00, 0x20, 0x00, 0x20, 0x00, 0x20, 0x00 },
            new byte[] { 0x2D, 0x00, 0x6E, 0x00, 0x75, 0x00, 0x6C, 0x00, 0x6C, 0x00, 0x72, 0x00, 0x68, 0x00, 0x69, 0x00, 0x20, 0x00, 0x20, 0x00, 0x20 }
        };

        // placeholder for legacy builds, will add later when i have time lol
        byte[][] oldHexes4_5 = new byte[][]
        {
            new byte[] {},
            new byte[] {},
            new byte[] {},
            new byte[] {}
        };

        byte[][] newHexes4_5 = new byte[][]
        {
            new byte[] {},
            new byte[] {},
            new byte[] {},
            new byte[] {}
        };

        byte[][] oldHexes = isVersion5OrHigher ? oldHexes5Plus : oldHexes4_5;
        byte[][] newHexes = isVersion5OrHigher ? newHexes5Plus : newHexes4_5;

        for (int i = 0; i < oldHexes.Length; i++)
        {
            ReplaceBytes(ref fileBytes, oldHexes[i], newHexes[i]);
        }

        File.WriteAllBytes(exePath, fileBytes);
        Log("Hex values have been modified successfully!");
    }

    static void ReplaceBytes(ref byte[] fileBytes, byte[] oldBytes, byte[] newBytes)
    {
        int index = IndexOfSequence(fileBytes, oldBytes);
        if (index >= 0)
        {
            for (int i = 0; i < newBytes.Length; i++)
            {
                fileBytes[index + i] = newBytes[i];
            }
        }
    }

    static int IndexOfSequence(byte[] haystack, byte[] needle)
    {
        int haystackLength = haystack.Length;
        int needleLength = needle.Length;

        for (int i = 0; i < haystackLength - needleLength + 1; i++)
        {
            bool found = true;
            for (int j = 0; j < needleLength; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                return i;
            }
        }

        return -1;
    }
}