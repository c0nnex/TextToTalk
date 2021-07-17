using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace TextToTalk
{
    public class Trigger
    {
        public string Text { get; set; }
        public bool IsRegex { get; set; }
        public bool ShouldRemove { get; set; }

        public Trigger()
        {
            Text = "";
        }

        public bool Match(string test)
        {
            if (!IsRegex) return test.Contains(Text);

            try
            {
                return Regex.Match(test, Text).Success;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public class TextReplacer
    {
        public string ChatText { get; set; } = "";
        public string ReplaceWith { get; set; } = "";
        public bool UseSSML { get; set; } = false;

        [JsonIgnore]
        public bool ShouldRemove { get; set; }
    }
}