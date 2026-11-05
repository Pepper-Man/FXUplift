using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FXUplift: A Halo 2 to Halo 3 effect converter\n\n");

            while (true)
            {
                Console.WriteLine("Single or Bulk effect tag conversion? (S/B):");
                string userInput = Console.ReadLine();
                if (userInput.ToLower() == "s")
                {
                    Console.WriteLine("Selected single tag conversion.\n");
                    SingleTagConversion();
                    break;
                }
                else if (userInput.ToLower() == "b")
                {
                    Console.WriteLine("Selected bulk tag conversion.\n");
                    BulkTagConversion();
                    break;
                }

                Console.WriteLine("Invalid input. Enter \"S\" or \"B\" characters only. Please try again.");
            }


            // TEMPORARY HARDCODING
            /*
            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\objects\\solo\\deltatemple\\gondola\\gondola_beam_rail.effect";
            string h3ek = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            string h2ek = Utils.GetH2EKPath(h2EffectPath);
            string outputFolder = "halo_2\\effe_test\\gondola_beam_rail";
            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
            ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek, true);
            WriteToH3.WriteH3Effect(h3ek, outputFolder, effect);
            */
        }

        static void SingleTagConversion()
        {
            string h2EffectPath = "";
            
            // Get H2 effect tag path
            while (true)
            {
                Console.WriteLine("Enter full filepath to H2 effect tag, from drive letter to file extension:");
                h2EffectPath = Console.ReadLine().Replace("\"", "");
                if (File.Exists(h2EffectPath))
                {
                    if (h2EffectPath.Contains("H2EK\\tags"))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unable to locate \"H2EK\" directory. Make sure file is inside \"H2EK\\tags\".\n");
                    }
                }
                else
                {
                    Console.WriteLine("File does not exist! Please try again.\n");
                }
            }

            string h3ek = Utils.GetEditingKitFolder("H3EK");
            string h2ek = Utils.GetH2EKPath(h2EffectPath);
            string outputFolder = Utils.H2ToH3Path(h2EffectPath);

            bool overwrite = false;
            string existingH3Tag = WriteToH3.GetH3TagFilePath(h3ek, outputFolder, "effect");
            if (File.Exists(existingH3Tag))
            {
                Console.WriteLine($"An H3 tag already exists at:\n\"{existingH3Tag}\"\nOverwrite it? (Y/N):");
                overwrite = Console.ReadLine().ToLower() == "y";

                if (!overwrite)
                {
                    Console.WriteLine("Skipped.");
                    return;
                }
            }


            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
            bool debugOutput = false;
#if DEBUG
            debugOutput = true;
#endif
            ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek, debugOutput);
            WriteToH3.WriteH3Effect(h3ek, outputFolder, effect);
        }

        static void BulkTagConversion()
        {
            Console.WriteLine("The bulk option will convert ALL effect tags within \"H2EK\\tags\" and will take a while.\nAre you sure you want to proceed? (Y/N):");
            string input = Console.ReadLine();
            if (input.ToLower() != "y")
            {
                return;
            }

            string h2ek = Utils.GetEditingKitFolder("H2EK");
            string h3ek = Utils.GetEditingKitFolder("H3EK");

            string[] allEffects = Directory.GetFiles(Path.Combine(h2ek, "tags"), "*.effect", SearchOption.AllDirectories);

            Stopwatch batchTimer = Stopwatch.StartNew();
            Utils.ExtractTagsBatch(allEffects, "effect", h2ek);
            batchTimer.Stop();
            Console.WriteLine($"Batch XML export took {batchTimer.Elapsed}\n");

            int i = 0;
            bool debugOutput = false;
#if DEBUG
            debugOutput = true;
#endif
            foreach (string h2EffectPath in allEffects)
            {
                string outputFolder = Utils.H2ToH3Path(h2EffectPath);
                string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
                ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek, debugOutput);
                WriteToH3.WriteH3Effect(h3ek, outputFolder, effect);
                Console.WriteLine($"Converted: {i + 1}/{allEffects.Length} effect tags");
                i++;
            }

            Console.WriteLine("Done all! Press enter to exit.");
            Console.ReadLine();
        }
    }
}
