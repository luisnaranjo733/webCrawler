using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebRole1.interfaces;

namespace WebRole1.models
{
    public class DictionaryNode : AbstractNode
    {
        public Dictionary<char, AbstractNode> childNodes = new Dictionary<char, AbstractNode>();

        public override AbstractNode containsChildWith(char data)
        {
            if (childNodes.ContainsKey(data))
            {
                return childNodes[data];
            } else
            {
                return null;
            }
        }

        public override void addChild(AbstractNode child)
        {
            if (!childNodes.ContainsKey(child.data))
            {
                childNodes[child.data] = child;
            }
        }

        public override List<AbstractNode> getChildren()
        {
            List<AbstractNode> children = new List<AbstractNode>();
            foreach (KeyValuePair<char, AbstractNode> entry in childNodes)
            {
                children.Add(entry.Value);
            }
            return children;
        }
    }
}