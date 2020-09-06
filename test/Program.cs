using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Tensorflow;
using Console = Colorful.Console;
using static Tensorflow.Binding;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = ParseArgs(args);

            var examples = Assembly.GetEntryAssembly().GetTypes()
                .Where(x => x.Name.Equals("DetectInMobilenet"))
                .Select(x => (IExample)Activator.CreateInstance(x))
                .Where(x => x.InitConfig() != null)
                .Where(x => x.Config.Enabled)
                .OrderBy(x => x.Config.Priority)
                .ToArray();

            if (parsedArgs.ContainsKey("ex"))
                examples = examples.Where(x => x.Config.Name == parsedArgs["ex"]).ToArray();
            var detect = examples[0];

            Console.WriteLine(examples[0], Color.Blue);
            

            Console.WriteLine(Environment.OSVersion, Color.Yellow);
            Console.WriteLine($"64Bit Operating System: {Environment.Is64BitOperatingSystem}", Color.Yellow);
            Console.WriteLine($"TensorFlow.NET v{Assembly.GetAssembly(typeof(TF_DataType)).GetName().Version}", Color.Yellow);
            Console.WriteLine($"TensorFlow Binary v{tf.VERSION}", Color.Yellow);
            Console.WriteLine($".NET CLR: {Environment.Version}", Color.Yellow);
            Console.WriteLine(Environment.CurrentDirectory, Color.Yellow);

            RunExamples(detect);
        }

        private static void RunExamples(IExample example)
        {
            int finished = 0;
            var errors = new List<string>();
            var success = new List<string>();

            var sw = new Stopwatch();

            //Console.WriteLine($"{DateTime.UtcNow} Starting {example.Config.Name}", Color.White);

            try
            {
                sw.Restart();
                bool isSuccess = example.Run();
                sw.Stop();

                if (isSuccess)
                    success.Add($"Example: {example.Config.Name} in {sw.Elapsed.TotalSeconds}s");
                else
                    errors.Add($"Example: {example.Config.Name} in {sw.Elapsed.TotalSeconds}s");
            }
            catch (Exception ex)
            {
                errors.Add($"Example: {example.Config.Name}");
                Console.WriteLine(ex);
            }

            finished++;
            Console.WriteLine($"{DateTime.UtcNow} Completed {example.Config.Name}", Color.White);


            success.ForEach(x => Console.WriteLine($"{x} is OK!", Color.Green));
            errors.ForEach(x => Console.WriteLine($"{x} is Failed!", Color.Red));

            //Console.WriteLine($"{finished} of {examples.Length} example(s) are completed.");
            Console.Write("Press [Enter] to continue...");
            Console.ReadLine();
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var parsed = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i].Substring(1);
                switch (key)
                {
                    case "ex":
                        parsed.Add(key, args[++i]);
                        break;
                    default:
                        break;
                }
            }

            return parsed;
        }
    }
}
