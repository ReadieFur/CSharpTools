using System;
using System.Linq;

namespace CSharpTools.ConsoleExtensions
{
    public static class Input
    {
        private static readonly string retryStr = "Please try again.";

        //After writing some code for another project I realised that blocking on a read will not allow for any other itens in the queue to be processed, this is not preferable.
        /*public static Task<string> QueueReadLine()
        {
            return Helpers.QueueTask(() =>
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();
                return userInput;
            });
        }

        public static Task<int> QueueRead()
        {
            return Helpers.QueueTask(() =>
            {
                return Console.Read();
            });
        }

        public static Task<ConsoleKeyInfo> QueueReadKey(bool intercept = false)
        {
            return Helpers.QueueTask(() =>
            {
                return Console.ReadKey(intercept);
            });
        }*/

        public static string GetString(bool requireValidValue = false, string[]? match = null)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (requireValidValue && string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine($"Input must not be empty. {retryStr}");
                    continue;
                }
                if (match != null && !match.Contains(userInput))
                {
                    Console.WriteLine($"Input must be one of: {string.Join(", ", match)}");
                    continue;
                }
                return userInput;
            }
        }

        public static int? GetInt(bool requireValidNumber = false)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (int.TryParse(userInput, out int value)) return value;
                else if (!requireValidNumber) return null;

                Console.WriteLine($"Input must be a valid integer. {retryStr}");
            }
        }

        public static int? GetInt(bool requireValidNumber = false, int? min = null, int? max = null)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (int.TryParse(userInput, out int value))
                {
                    if (min != null && value < min)
                    {
                        Console.WriteLine($"Input must be greater than {min}. {retryStr}");
                        continue;
                    }
                    if (max != null && value > max)
                    {
                        Console.WriteLine($"Input must be smaller than {max}. {retryStr}");
                        continue;
                    }
                    return value;
                }
                else if (!requireValidNumber) return null;

                Console.WriteLine($"Input must be a valid integer. {retryStr}");
            }
        }

        public static double? GetDouble(bool requireValidNumber = false)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (double.TryParse(userInput, out double value)) return value;
                else if (!requireValidNumber) return null;

                Console.WriteLine($"Input must be a valid integer. {retryStr}");
            }
        }

        public static double? GetDouble(bool requireValidNumber = false, double? min = null, double? max = null)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (double.TryParse(userInput, out double value))
                {
                    if (min != null && value < min)
                    {
                        Console.WriteLine($"Input must be greater than {min}. {retryStr}");
                        continue;
                    }
                    if (max != null && value > max)
                    {
                        Console.WriteLine($"Input must be smaller than {max}. {retryStr}");
                        continue;
                    }
                    return value;
                }
                else if (!requireValidNumber) return null;

                Console.WriteLine($"Input must be a valid double. {retryStr}");
            }
        }

        public static bool? GetBool(bool requireValidBool = false)
        {
            while (true)
            {
                string? userInput = Console.ReadLine();
                if (userInput == null) throw new NullReferenceException();

                if (bool.TryParse(userInput, out bool value)) return value;
                else if (!requireValidBool) return null;

                Console.WriteLine($"Input must be a valid boolean. {retryStr}");
            }
        }
    }
}
