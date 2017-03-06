using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebRole1.interfaces;

namespace WebRole1.models
{
    public class ListNode : AbstractNode
    {
        private List<AbstractNode> childNodes = new List<AbstractNode>();

        public override AbstractNode containsChildWith(char data)
        {
            foreach(AbstractNode child in this.childNodes)
            {
                if (child.data == Char.ToLower(data))
                {
                    return child;
                }
            }
            return null;
        }

        public override void addChild(AbstractNode child)
        {
            this.childNodes.Add(child);
        }

        public override List<AbstractNode> getChildren()
        {
            return this.childNodes;
        }
    }
}