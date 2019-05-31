using System.Collections.Generic;

namespace NameAnonymizer
{
    public class Player
    {
        public Player()
        {
            Files = new List<string>();
            Lines = new List<TextLine>();
        }

        public string Original { get; set; }
        public string Replaced { get; set; }
        public List<string> Files { get; set; }
        public List<TextLine> Lines { get; set; }
    }
}