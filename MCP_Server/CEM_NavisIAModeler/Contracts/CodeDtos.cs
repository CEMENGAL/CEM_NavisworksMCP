// Cemengal: the replies of send_code_to_navisworks and take_screenshot (docs/decisions/0010).
using System.Collections.Generic;

namespace CEM_NavisIAModeler.Contracts
{
    /// <summary>What a script returned and printed.</summary>
    public sealed class CodeRunDto
    {
        /// <summary>The returned value as JSON: plain data as is, Navisworks objects as text, bounded in depth and size.</summary>
        public string result_json { get; set; } = "null";

        /// <summary>The returned value's .NET type, or empty for null.</summary>
        public string result_type { get; set; } = "";

        /// <summary>What the script wrote with Console.Write/WriteLine (bounded).</summary>
        public string output { get; set; } = "";

        public long compile_ms { get; set; }
        public long run_ms { get; set; }

        /// <summary>Compiler warnings in the script, with its line numbers.</summary>
        public List<string> warnings { get; set; } = new List<string>();
    }

    /// <summary>An image of Navisworks.</summary>
    public sealed class ScreenshotDto
    {
        /// <summary>view | window</summary>
        public string mode { get; set; } = "";
        public int width { get; set; }
        public int height { get; set; }
        public string png_base64 { get; set; } = "";
        public string document_title { get; set; } = "";
    }
}
