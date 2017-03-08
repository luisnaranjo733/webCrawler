using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebRole1.interfaces;

namespace WebRole1.models
{
    public class Trie : ITrie
    {
        private AbstractNode root = new ListNode();

        public int Size { get; set; }
        public string LastTitleInserted { get; set; }

        public bool IsBuilt()
        {
            return root.getChildren().Count > 0;
        }

        public void AddTitle(string title)
        {
            AddTitleHelper(title, this.root);
            Size = Size + 1;
            LastTitleInserted = title;
        }

        private void AddTitleHelper(string title, AbstractNode node)
        {
            if (title.Length == 0) // handle base case
            {
                return;
            }

            char firstLetter = title[0];
            AbstractNode nextNode = node.containsChildWith(firstLetter);

            if (nextNode == null) // char not there yet, so add it and recurse
            {
                nextNode = new ListNode(); // create new node
                nextNode.setData(firstLetter); // set its data
                node.addChild(nextNode); // add new node to current node's collection
                
                if (title.Length == 1)
                {
                    nextNode.markAsWordEnding();
                }

            }
            AddTitleHelper(title.Substring(1), nextNode); // recurse
        }

        public List<string> SearchForPrefix(string prefix)
        {
            AbstractNode prefixRoot = GetPrefixRoot(prefix, this.root);
            List<string> results = new List<string>();
            if (prefixRoot != null)
            {
                SearchFromPrefixRoot(ref results, prefix, prefixRoot);
            }
            
            return results;
        }

        private AbstractNode GetPrefixRoot(string prefix, AbstractNode node)
        {
            if (prefix.Length == 0)
            {
                return node;
            }

            char firstLetter = prefix[0];
            AbstractNode nextNode = node.containsChildWith(firstLetter);

            if (nextNode != null)
            {
                return GetPrefixRoot(prefix.Substring(1), nextNode);

            } else
            {
                return null;
            }
        }

        private void SearchFromPrefixRoot(ref List<string> results, string prefix, AbstractNode prefixRoot)
        {
            if (results.Count >= 10)
            {
                return;
            }

            foreach(AbstractNode childNode in prefixRoot.getChildren())
            {
                StringBuilder suggestionCandidate = new StringBuilder(prefix);
                suggestionCandidate.Append(childNode.getData());

                if (childNode.isWordEnding && results.Count < 10)
                {
                    int n = results.Count;
                    results.Add(suggestionCandidate.ToString());
                }

                SearchFromPrefixRoot(ref results, suggestionCandidate.ToString(), childNode);
            }

        }

    }
}