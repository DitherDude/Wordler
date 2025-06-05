using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Xml;

namespace Wordler
{
    internal class Program
    {
        static string[] words;
        static int maxlen = 0;
        static Dictionary<string, int> wordbank;
        static string[] ogargs;
        static string somewhere = "";
        static string weightfile = "";
        static void Main(string[] args)
        {
            Console.Clear();
            Console.WriteLine("NOTE: When you input wordle response; green: 2, yellow: 1, gray: 0");
            Console.WriteLine("So if you got green-gray-gray-yellow-gray, type 20010\n");
            ogargs = args;
            Console.WriteLine("Obtaining Data...");
            string input = "";
            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                {
                    input = File.ReadAllText(args[0]);
                    if (args.Length > 1)
                    {
                        if (File.Exists(args[1]))
                        {
                            weightfile = args[1];
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid file!");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
            else
            {
                if (!File.Exists("input.txt"))
                {
                    Console.WriteLine("Missing input.txt!");
                    Console.WriteLine("Try passing your input file as a parameter.");
                    Console.ReadKey();
                    Environment.Exit(2);
                }
                input = File.ReadAllText("input.txt").ToLower().Trim();
            }
            Console.WriteLine("Sorting Data...");
            words = input.ToLower().Trim().Split('\n');
            maxlen = words[0].Trim().Length;
            foreach (string word in words)
            {
                if (word.Trim().Length != maxlen)
                {
                    Console.WriteLine("Word length inconsistency!");
                    Console.ReadKey();
                    Environment.Exit(3);
                }
            }
            wordbank = words
            .Select((str, index) => new { Key = str.Trim(), Value = 0 })
            .ToDictionary(pair => pair.Key, pair => pair.Value);
            if (weightfile == "")
            {
                CalculateWeights();
            }
            else
            {
                LoadWeights();
            }
        }
        static void CalculateWeights()
        {
            Console.Write("Weighing Data... ");
            wordbank.Where(word => !somewhere.All(letter => word.Key.Contains(letter)))
                .ToList().ForEach(kvp => wordbank.Remove(kvp.Key));
            somewhere = "";
            int index = 0;
            foreach (string myguess in wordbank.Keys)
            {
                int score = 0;
                foreach (string slot in wordbank.Keys)
                {
                    string used = "";
                    for (int i = 0; i < maxlen; i++)
                    {
                        if (used.Contains(myguess[i]))
                        {
                            continue;
                        }
                        if (slot[i] == myguess[i])
                        {
                            score += 3;
                        }
                        else if (slot.Contains(myguess[i]))
                        {
                            score += 1;
                        }
                        used = used + myguess[i];
                    }
                }
                wordbank[myguess] = score;
                Console.Write($"\rWeighing Data... ({Math.Round(index * 100 / (double)wordbank.Count())}%)");
                index++;
            }
            Console.WriteLine("\rWeighing Data... Done. ");
            Console.WriteLine("Sorting Dictionary...");
            wordbank = wordbank.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            Guess();
        }
        static void Guess(string input = "")
        {
            KeyValuePair<string, int> guess = new KeyValuePair<string, int>("", 0);
            try
            {
                if (wordbank.ContainsKey(input) && input != "")
                {
                    guess = wordbank.Where(x => x.Key == input).First();
                }
                else
                {
                    guess = wordbank.First();
                }
            }
            catch
            {
                Console.WriteLine("Hmmm... no more words...");
                Console.WriteLine("Please confirm that the word is contained within the input file.");
            }
            Console.WriteLine($"\nGuess: {guess.Key.ToUpper()}\n");
            if (wordbank.Count != 1)
            {
                Console.WriteLine($"Words remaining: {(BigInteger)wordbank.Count} | Guess score: {Math.Round(guess.Value * 100 / (double)wordbank.Sum(x => x.Value), 2)}%");
            }
            else
            {
                {
                    Console.WriteLine("GG! Press any key to play again.");
                    Console.ReadKey();
                    Main(ogargs);
                }
            }
            bool ok = false;
            string? result = "";
            while (!ok)
            {
                Console.Write("\nResult: ");
                result = Console.ReadLine()?.Trim();
                if (result == null)
                {
                    Console.WriteLine("Please type result.");
                    continue;
                }
                if (result == "save")
                {
                    SaveWeights();
                    continue;
                }
                bool invalid = false;
                foreach (char c in result)
                {
                    if (c != '0' && c != '1' && c != '2')
                    {
                        invalid = true;
                    }
                }
                if (invalid)
                {
                    int len = wordbank.First().Key.Length;
                    int countlen = wordbank.First().Value.ToString().Length;
                    if (result == "@list")
                    {
                        Console.WriteLine(printLine(len, countlen));
                        foreach (KeyValuePair<string, int> word in wordbank)
                        {
                            Console.WriteLine($"Word: {word.Key} | Guess score: {Math.Round(word.Value * 100 / (double)wordbank.Sum(x => x.Value), 2).ToString("F2")}% | Guess value: {word.Value}");
                        }
                        Console.WriteLine(printLine(len, countlen));
                        Guess();
                        return;
                    }
                    else if (result.StartsWith("@list"))
                    {
                        int head = 0;
                        int.TryParse(result.Replace("@list", ""), out head);
                        Console.WriteLine(printLine(len, countlen));
                        for (int i = 0; i < head && i < wordbank.Count(); i++)
                        {
                            KeyValuePair<string, int> word = wordbank.ElementAt(i);
                            Console.WriteLine($"Word: {word.Key} | Guess score: {Math.Round(word.Value * 100 / (double)wordbank.Sum(x => x.Value), 2).ToString("F2")}% | Guess value: {word.Value}");
                        }
                        Console.WriteLine(printLine(len, countlen));
                        Guess();
                        return;
                    }
                    else if (wordbank.ContainsKey(result) && result != "")
                    {
                        Guess(result);
                        return;
                    }
                    Console.WriteLine("Invalid result.");
                    continue;
                }
                if (result?.Length != maxlen)
                {
                    Console.WriteLine("Invalid length.");
                    continue;
                }
                ok = true;
            }
            if (result.All(c => c == '2'))
            {
                Main(ogargs);
                return;
            }
            Console.WriteLine("Removing Redundancies...");
            wordbank.Remove(guess.Key);
            string used = "";
            for (int i = 0; i < maxlen; i++)
            {
                if (used.Contains(guess.Key[i]))
                {
                    continue;
                }
                if (result[i] == '2')
                {
                    //wordbank = wordbank.Where(kvp => !kvp.Key[i].Equals(result[i])).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    wordbank.Where(x => !x.Key[i].Equals(guess.Key[i])).ToList()
                        .ForEach(kvp => wordbank.Remove(kvp.Key));
                }
                else if (result[i] == '1')
                {
                    wordbank.Where(x => x.Key[i].Equals(guess.Key[i])).ToList()
                        .ForEach(kvp => wordbank.Remove(kvp.Key));
                    wordbank.Where(x => !x.Key.Contains(guess.Key[i])).ToList()
                        .ForEach(kvp => wordbank.Remove(kvp.Key));
                    somewhere = somewhere + guess.Key[i];
                }
                else if (result[i] == '0')
                {
                    bool found = false;
                    if (guess.Key != guess.Key.Distinct().ToString())
                    {
                        for (int j = 0; j < maxlen; j++)
                        {
                            if (guess.Key[j] == guess.Key[i] && j != i)
                            {
                                found = true;
                            }
                        }
                    }
                    if (found) { continue; }
                    //wordbank = wordbank.Where(kvp => !kvp.Key.Contains(result[i])).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    wordbank.Where(x => x.Key.Contains(guess.Key[i])).ToList()
                        .ForEach(kvp => wordbank.Remove(kvp.Key));
                }
                used = used + guess.Key[i];
            }
            CalculateWeights();
        }
        static void LoadWeights()
        {
            string input = File.ReadAllText(weightfile).Trim();
            int[] weights = input.Split('\n').Select(x => Convert.ToInt32(x.Trim())).ToArray();
            if (weights.Count() != words.Count())
            {
                Console.WriteLine("Weight/Input file mismatch.");
                Console.WriteLine($"{words.Count()}, {weights.Count()}");
                Console.ReadKey();
                Environment.Exit(4);
            }
            for (int i = 0; i < words.Count(); i++)
            {
                wordbank[words[i]] = weights[i];
            }
            Console.WriteLine("Sorting Dictionary...");
            wordbank = wordbank.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            Guess();
        }
        static void SaveWeights()
        {
            string data = "";
            Console.WriteLine("Saving weights...");
            foreach (int i in wordbank.Values)
            {
                data += i + "\n";
            }
            File.WriteAllText("weights.txt", data);
            Console.WriteLine($"Current weights state saved to {Environment.CurrentDirectory}\\weights.txt");
            data = "";
            Console.WriteLine("Saving sorted data...");
            foreach (string s in wordbank.Keys)
            {
                data += s + "\n";
            }
            File.WriteAllText("sortedinput.txt", data);
            Console.WriteLine($"Current sorted wordbank state saved to {Environment.CurrentDirectory}\\sortedinput.txt");
        }
        static string printLine(int length, int scorelength)
        {
            string line = "=======";
            for (int i = 0; i < length; i++)
            {
                line += "=";
            }
            line += "|====================|==============";
            for (int i = 0; i < scorelength; i++)
            {
                line += "=";
            }
            return line;
        }
    }
}
