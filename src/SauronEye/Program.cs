﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EPocalipse.IFilter;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SauronEye {
    class Program {

        private static List<string> Directories, FileTypes, Keywords;
        private static bool SearchContents;
        private static bool SystemDirs;
        private static RegexSearch regexSearcher;
        static void Main(string[] args) {
            Console.WriteLine("\n\t === SauronEye === \n");
            Directories = new List<string>();
            FileTypes = new List<string>();
            Keywords = new List<string>();
            SearchContents = false;
            SystemDirs = false;
            parseArguments(args);
            Console.WriteLine("Directories to search: " + string.Join(", ", Directories));
            Console.WriteLine("For file types: " + string.Join(", ", FileTypes));
            Console.WriteLine("Containing: " + string.Join(", ", Keywords));
            Console.WriteLine("Search contents: " + SearchContents.ToString());
            Console.WriteLine("Search Program Files directories: " + SystemDirs.ToString() + "\n");
            Stopwatch sw = new Stopwatch();

            sw.Start();

            var options = new ParallelOptions { MaxDegreeOfParallelism = Directories.Count };
            Parallel.ForEach(Directories, options, (dir) => {
                Console.WriteLine("Searching in parallel: " + dir);
                var fileSystemSearcher = new FSSearcher(dir, FileTypes, Keywords, SearchContents, SystemDirs, regexSearcher);
                fileSystemSearcher.Search();
                        
            });
            sw.Stop();

            Console.WriteLine("\n Done. Time elapsed = {0}", sw.Elapsed);

            if (Debugger.IsAttached)
                Console.ReadKey();
        }


        // Implemented my own args parser. This is probably a very bad idea...
        // Transforms: 
        // -Dirs C:\,D:\,\\NLDOMFS\testing\ -Filetype .txt, .ini,.conf , .bat
        // To:
        // List<string>{ "C:\", "D:\", "\\NLDOMFS\testing\" } and List<string>{ ".txt", ".ini", ".conf", ".bat" }
        private static void parseArguments(string[] args) {
            for (var arg = 0 ; arg < args.Length;) {
                if (args[arg].StartsWith("-")) {
                    switch (args[arg].ToLower()) {
                        case "-dirs": {
                                if (args[arg+1].ToLower().Equals("-dirs")) { arg++; break; }
                                while (arg+1 != args.Length && !args[arg+1].StartsWith("-")) {
                                    Directories.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-filetypes": {
                                if (args[arg+1].ToLower().Equals("-filetype")) { arg++; break; }
                                while (arg+1 != args.Length && !args[arg+1].StartsWith("-")) { 
                                    FileTypes.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-keywords": {
                                if (args[arg + 1].ToLower().Equals("-keywords")) { arg++; break; }
                                while (arg + 1 != args.Length && !args[arg + 1].StartsWith("-")) {
                                    Keywords.AddRange(strimAndSplit(args[arg + 1].ToLower()));
                                    arg++;
                                }
                            } break;
                        case "-contents": {
                                SearchContents = true;
                                arg++;
                            } break;
                        case "-systemdirs": {
                                SystemDirs = true;
                                arg++;
                            }
                            break;
                        default:
                            // unknown arg, proceed to next
                            arg++;
                            break;
                    }
                } else {
                    // increment to next arg
                    arg++;
                } 
            }
            // remove empty or duplicate args
            Directories = Directories.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            FileTypes = FileTypes.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            Keywords = Keywords.Where(s => !isNullOrWhiteSpace(s)).Distinct().ToList();
            regexSearcher = new RegexSearch(Keywords);
            return;
        }
        
        private static bool isNullOrWhiteSpace(string s) {
            return String.IsNullOrEmpty(s) || s.Trim().Length == 0;
        }

        // Remove whitespaces before and after ',' and split at ',' afterwards
        private static string[] strimAndSplit(string s) {
            return Regex.Replace(s, " *, *", ",").Split(',');
        }

        private static ConcurrentBag<string> ConverToConcurrentBag(List<string> list) {
            var cbag = new ConcurrentBag<string>();
            foreach (string i in list) {
                cbag.Add(i);
            }
            return cbag;
        } 
    }


}
