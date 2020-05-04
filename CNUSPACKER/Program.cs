using System;
using System.Collections.Generic;
using System.IO;
using CNUSPACKER.crypto;
using CNUSPACKER.packaging;
using CNUSPACKER.utils;

namespace CNUSPACKER
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("CNUSPacker v0.01-c2w by NicoAICP [C# Port of NUSPacker by timogus, FIX94]\n\n");

            string inputPath = "input";
            string outputPath = "output";

            string encryptionKey = "";
            string encryptKeyWith = "";

            long titleID = 0x0L;
            long osVersion = 0x000500101000400AL;
            uint appType = 0x80000000;
            short titleVersion = 0;

            bool skipXMLReading = false;

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide at least the in and out parameter");

                ShowHelp();
                Environment.Exit(0);
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-in"))
                {
                    if (args.Length > i + 1)
                    {
                        inputPath = args[i + 1];
                        i++;
                    }
                }
                else if (args[i].Equals("-out"))
                {
                    if (args.Length > i + 1)
                    {
                        outputPath = args[i + 1];
                        Directory.CreateDirectory(outputPath);
                        i++;
                    }
                }
                else if (args[i].Equals("-tID"))
                {
                    if(args.Length > i + 1)
                    {
                        titleID = Convert.ToInt64(args[i + 1], 16);
                        i++;
                    }
                }
                else if (args[i].Equals("-OSVersion"))
                {
                    if (args.Length > i + 1)
                    {
                        osVersion = Convert.ToInt64(args[i + 1], 16);
                        i++;
                    }
                }
                else if (args[i].Equals("-appType"))
                {
                    if (args.Length > i + 1)
                    {
                        appType = Convert.ToUInt32(args[i + 1], 16);
                        i++;
                    }
                }
                else if (args[i].Equals("-titleVersion"))
                {
                    if (args.Length > i + 1)
                    {
                        titleVersion = Convert.ToInt16(args[i + 1], 16);
                        i++;
                    }
                }
                else if (args[i].Equals("-encryptionKey"))
                {
                    if (args.Length > i + 1)
                    {
                        encryptionKey = args[i + 1];
                        i++;
                    }
                }
                else if (args[i].Equals("-encryptKeyWith"))
                {
                    if (args.Length > i + 1)
                    {
                        encryptKeyWith = args[i + 1];
                        i++;
                    }
                }
                else if (args[i].Equals("-skipXMLParsing"))
                {
                    skipXMLReading = true;
                }
                else if (args[i].Equals("-help"))
                {
                    ShowHelp();
                    Environment.Exit(0);
                }
            }

            if (!Directory.Exists(Path.Combine(inputPath, "code")) || !Directory.Exists(Path.Combine(inputPath, "content")) || !Directory.Exists(Path.Combine(inputPath, "meta")))
            {
                Console.WriteLine($"Invalid input dir ({Path.GetFullPath(inputPath)}): It's missing either the code, content or meta folder");
                Environment.Exit(0);
            }

            AppXMLInfo appinfo = new AppXMLInfo
            {
                titleID = titleID,
                groupID = (short)(titleID >> 8),
                appType = appType,
                osVersion = osVersion,
                titleVersion = titleVersion
            };

            if (encryptionKey == "" || encryptionKey.Length != 32)
            {
                encryptionKey = Settings.defaultEncryptionKey;
                Console.WriteLine($"Empty or invalid encryption provided. Will use {encryptionKey} instead");
            }
            Console.WriteLine();
            if (encryptKeyWith == "" || encryptKeyWith.Length != 32)
            {
                Console.WriteLine($"Will try to load the encryptionWith key from the file \"{Settings.encryptWithFile}\".");
                encryptKeyWith = LoadEncryptWithKey();
            }
            if (encryptKeyWith == "" || encryptKeyWith.Length != 32)
            {
                encryptKeyWith = Settings.defaultEncryptWithKey;
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine($"WARNING:Empty or invalid encryptWith key provided. Will use {encryptKeyWith} instead!");
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            Console.WriteLine();

            string appxml = inputPath + Settings.pathToAppXml;

            if (!skipXMLReading)
            {
                Console.WriteLine("Parsing app.xml in code folder (use the -skipXMLParsing argument to disable it)");
                try
                {

                    XMLParser parser = new XMLParser();
                    parser.LoadDocument(appxml);

                    appinfo = parser.GetAppXMLInfo();
                }
                catch (Exception  e)
                {
                    Console.WriteLine($"Error while parsing the app.xml from path \"{Settings.pathToAppXml}\".");
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Skipped app.xml parsing");
            }

            short content_group = appinfo.groupID;
            titleID = appinfo.titleID;

            long parentID = titleID & ~0x0000000F00000000L;
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine($"Input            : \"{inputPath}\"");
            Console.WriteLine($"Output           : \"{outputPath}\"");

            Console.WriteLine($"TitleID          : {appinfo.titleID:X16}");
            Console.WriteLine($"GroupID          : {appinfo.groupID:X4}");
            Console.WriteLine($"ParentID         : {parentID:X16}");
            Console.WriteLine($"AppType          : {appinfo.appType:X8}");
            Console.WriteLine($"OSVersion        : {appinfo.osVersion:X16}");
            Console.WriteLine($"Encryption key   : {encryptionKey}");
            Console.WriteLine($"Encrypt key with : {encryptKeyWith}");
            Console.WriteLine();

            Console.WriteLine("---");
            List<ContentRule> rules = ContentRule.GetCommonRules(content_group, parentID);

            Directory.CreateDirectory(Settings.tmpDir);

            NusPackageConfiguration config = new NusPackageConfiguration(inputPath, appinfo, new Key(encryptionKey), new Key(encryptKeyWith), rules);
            NUSpackage nuspackage = NUSPackageFactory.CreateNewPackage(config);
            nuspackage.PackContents(outputPath);
            nuspackage.PrintTicketInfos();

            Utils.DeleteDir(Settings.tmpDir);
        }

        private static string LoadEncryptWithKey()
        {
            if (!File.Exists(Settings.encryptWithFile))
                return "";
            string key = "";
            try
            {
                using StreamReader input = new StreamReader(Settings.encryptWithFile);
                key = input.ReadLine() ?? "";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to read \"{Settings.encryptWithFile}\".");
                Console.WriteLine(e.Message);
            }

            return key;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("help:");
            Console.WriteLine("-in             ; is the dir where you have your decrypted data. Make this pointing to the root folder with the folder code, content and meta.");
            Console.WriteLine("-out            ; Where the installable package will be saved.");
            Console.WriteLine("");
            Console.WriteLine("(optional! will be parsed from app.xml if missing)");
            Console.WriteLine("-tID            ; titleId of this package. Will be saved in the TMD and provided as 00050000XXXXXXXX");
            Console.WriteLine("-OSVersion      ; target OS version");
            Console.WriteLine("-appType        ; app type");
            Console.WriteLine("-skipXMLParsing ; disables the app.xml parsing");
            Console.WriteLine("");
            Console.WriteLine("(optional! defaults values will be used if missing (or loaded from external file))");
            Console.WriteLine("-encryptionKey  ; the key that is used to encrypt the package");
            Console.WriteLine("-encryptKeyWith ; the key that is used to encrypt the encryption key");
            Console.WriteLine("");
        }
    }
}
