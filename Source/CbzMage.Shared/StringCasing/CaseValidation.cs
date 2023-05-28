using System.Text.RegularExpressions;

namespace CbzMage.Shared.StringCasing
{
    public static class CaseValidation
    {
        public static bool IsUpperCased(this string str) => !str.Any(char.IsLower);

        public static bool IsLowerCased(this string str) => !str.Any(char.IsUpper);

        public static bool IsRomanNumeral(this string str)
        {
            // Use flexible matching that catches IIII etc.
            // https://www.oreilly.com/library/view/regular-expressions-cookbook/9780596802837/ch06s09.html
            return Regex.IsMatch(str, "^(?=[MDCLXVI])M*(C[MD]|D?C*)(X[CL]|L?X*)(I[XV]|V?I*)$");
        }

        public static string FixCasedString(string str, bool isLowerCased)
        {
            // Don't "fix" uppercased roman numerals
            if (!isLowerCased && str.IsRomanNumeral())
            {
                return str;
            }

            var chars = str.ToCharArray();

            bool wordStart = false;

            for (int i = 0, sz = chars.Length; i < sz; i++)
            {
                if (CheckCase(chars[i]))
                {
                    if (!wordStart)
                    {
                        wordStart = true;
                        if (isLowerCased)
                        {
                            chars[i] = char.ToUpper(str[i]);
                        }
                    }
                    else if (!isLowerCased)
                    {
                        chars[i] = char.ToLower(str[i]);
                    }
                }
                else
                {
                    wordStart = false;
                }
            }

            return new string(chars);

            bool CheckCase(char ch) => isLowerCased ? char.IsLower(ch) : char.IsUpper(ch);
        }
    }
}
