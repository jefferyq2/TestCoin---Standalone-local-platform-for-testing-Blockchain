using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCoin.Common
{
    public static class Common
    {
        /// <summary>
        /// Prompts the form to create an alert for the user
        /// </summary>
        /// <param name="text"></param>
        public static void alertBox(String text)
        {
            Alert alert = new Alert(text);
            alert.ShowDialog();
        }
        /// <summary>
        /// Fast method to remove whitespace from strings
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string[] splitAt(string line, string splitpoint)
        {
            if (splitpoint.Equals("newline"))
            {
                return line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }
            else
            {
                return line.Split(new[] { splitpoint }, StringSplitOptions.None);
            }
        }

    }
}
