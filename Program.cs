using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
namespace SpellCheck
{
    class Program
    {
        static Dictionary<string, int> Dic;

        static string dicFile = "dic.txt";
        static string trainingFile = "training.txt";

        static void Main(string[] args)
        {

            if (File.Exists(dicFile))
            {
                Console.WriteLine("加载词典中...");
                LoadDic();
                Console.WriteLine("加载词典完成");
            }
            else
            {
                Console.WriteLine("训练词典中...");
                Dic = LoadUSDic();
                TrainDic(trainingFile, Dic);
                StringBuilder dicBuilder = new StringBuilder();
                foreach (var item in Dic)
                {
                    dicBuilder.AppendLine(item.Key + "\t" + item.Value);
                }
                File.WriteAllText(dicFile, dicBuilder.ToString());
                var wordCount = Dic.Count;
                Console.WriteLine("训练完成...");

            }
            Console.WriteLine("请输入词语...");
            var inputWord = Console.ReadLine();

            while (!inputWord.Equals("exit"))
            {
                if (Dic.ContainsKey(inputWord))
                {
                    Console.WriteLine("你输入的词语 【" + inputWord + "】 是正确的!");
                }
                else
                {
                    var suggestWords = GetSuggestWords(inputWord);
                    Console.WriteLine("候选词语: ");
                    foreach (var word in suggestWords)
                    {
                        Console.WriteLine("\t\t\t " + word);
                    }
                }
                Console.WriteLine("请输入词语....");
                inputWord = Console.ReadLine();

            }
        }

        /// <summary>
        /// 加载词典
        /// </summary>
        public static void LoadDic()
        {
            Dic = new Dictionary<string, int>();

            var lines = File.ReadAllLines(dicFile);

            foreach (var line in lines)
            {
                if (line != "")
                {
                    var dicItem = line.Split('\t');
                    if (dicItem.Length == 2)
                        Dic.Add(dicItem[0], int.Parse(dicItem[1]));
                }
            }
        }

        /// <summary>
        /// 训练词典
        /// </summary>
        /// <param name="trainingFile"></param>
        /// <param name="ht"></param>
        public static void TrainDic(string trainingFile, Dictionary<string, int> ht)
        {

            StreamReader reader = new StreamReader(trainingFile);
            string sLine = "";//存放每一个句子

            string pattern = @"[a-z]+";//匹配单词

            Regex regex = new Regex(pattern);
            int count = 0;//计算单词的个数

            while (sLine != null)
            {
                sLine = reader.ReadLine();
                if (sLine != null)
                {
                    sLine = sLine.ToLower().Replace("'", " ");
                    var matchWords = regex.Matches(sLine);

                    foreach (Match match in matchWords)
                    {
                        var word = match.Value;
                        if (!ht.ContainsKey(word))
                        {
                            count++;
                            ht.Add(word, 1);
                        }
                        else
                        {
                            ht[word]++;
                        }
                    }
                }
            }
            reader.Close();
        }

        /// <summary>
        /// 从en-US读取词语【词语开始[Words]】
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, int> LoadUSDic()
        {
            var dic = new Dictionary<string, int>();
            string currentSection = "";

            FileStream fs = new FileStream("en-US.dic", FileMode.Open, FileAccess.Read, FileShare.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            while (sr.Peek() >= 0)
            {
                string tempLine = sr.ReadLine().Trim();
                if (tempLine.Length > 0)
                {
                    switch (tempLine)
                    {
                        case "[Words]":
                            currentSection = tempLine;
                            break;
                        default:
                            switch (currentSection)
                            {
                                case "[Words]": // dictionary word list
                                    // splits word into its parts
                                    string[] parts = tempLine.Split('/');
                                    dic.Add(parts[0], 1);
                                    break;
                            } // currentSection swith
                            break;
                    } //tempLine switch
                } // if templine
            } // read line
            sr.Close();
            fs.Close();
            return dic;
        }

        /// <summary>
        /// 编辑距离为1的词语
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static List<string> GetEdits1(string word)
        {
            var n = word.Length;
            var tempWord = "";
            var editsWords = new List<string>();
            for (int i = 0; i < n; i++)//delete一个字母的情况
            {
                tempWord = word.Substring(0, i) + word.Substring(i + 1);
                if (!editsWords.Contains(tempWord))
                    editsWords.Add(tempWord);
            }

            for (int i = 0; i < n - 1; i++)//调换transposition一个字母的情况
            {
                tempWord = word.Substring(0, i) + word.Substring(i + 1, 1) + word.Substring(i, 1) + word.Substring(i + 2);
                if (!editsWords.Contains(tempWord))
                    editsWords.Add(tempWord);
            }

            for (int i = 0; i < n; i++)//替换replace一个字母的情况
            {
                string t = word.Substring(i, 1);
                for (int ch = 'a'; ch <= 'z'; ch++)
                {
                    if (ch != Convert.ToChar(t))
                    {
                        tempWord = word.Substring(0, i) + Convert.ToChar(ch) + word.Substring(i + 1);
                        if (!editsWords.Contains(tempWord))
                            editsWords.Add(tempWord);
                    }
                }
            }


            for (int i = 0; i <= n; i++)//insert一个字母的情况
            {
                //string t = word.Substring(i, 1);
                for (int ch = 'a'; ch <= 'z'; ch++)
                {
                    tempWord = word.Substring(0, i) + Convert.ToChar(ch) + word.Substring(i);
                    if (!editsWords.Contains(tempWord))
                        editsWords.Add(tempWord);
                }
            }

            return editsWords;
        }

        /// <summary>
        /// 获取编辑距离为2的单词
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static List<string> GetEdits2(string word)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var words = GetEdits1(word);

            var result = words.AsReadOnly().ToList();

            foreach (var edit in words)
            {
                GetEdits1(edit).ForEach(w =>
                {
                    if (Dic.ContainsKey(w))
                    {
                        result.Add(w);
                    }
                });
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            return result;
        }

        //static WordCompare compare = new WordCompare();

        /// <summary>
        /// 获取建议词语
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static List<string> GetSuggestWords(string word)
        {
            var result = GetEdits1(word).Where(w => Dic.ContainsKey(w)).ToList();

            if (result.Count == 0)
            {
                result = GetEdits2(word);
                if (result.Count == 0)
                {
                    result.Add(word);
                }
            }

            // 按先验概率排序
            result = result.OrderByDescending(w => Dic.ContainsKey(w) ? Dic[w] : 1).ToList();
            return result.Take(Math.Min(result.Count, 5)).ToList();
        }


        /// <summary>
        /// 自定义比较
        /// </summary>
        class WordCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var hash1 = Dic.ContainsKey(x) ? Dic[x] : 1;
                var hash2 = Dic.ContainsKey(y) ? Dic[y] : 1;
                return hash1.CompareTo(hash2);
            }
        }
    }
}
