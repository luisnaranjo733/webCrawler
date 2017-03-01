using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebRole1.models
{
    public class TrieNode 
    {
        private char data;
        public bool isWordEnding = false;
        public List<TrieNode> children = new List<TrieNode>();


        public override string ToString()
        {
            string repr = "" + this.data;
            if (this.isWordEnding)
            {
                repr = repr + "$";
            }
            return repr;
        }

        public TrieNode containsChildWith(char data)
        {
            foreach(TrieNode child in children)
            {
                if (child.data == Char.ToLower(data))
                {
                    return child;
                }
            }
            return null;
        }

        public void setData(char data)
        {
            this.data = Char.ToLower(data);
        }

        public char getData()
        {
            return this.data;
        }

        public void addChild(TrieNode child)
        {
            this.children.Add(child);
        }

        public List<TrieNode> getChildren()
        {
            return this.children;
        }

        public void markAsWordEnding()
        {
            this.isWordEnding = true;
        }
    }
}