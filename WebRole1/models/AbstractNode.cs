using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRole1.interfaces
{
    public abstract class AbstractNode
    {
        public char data;
        public bool isWordEnding = false;

        abstract public AbstractNode containsChildWith(char data);
        abstract public void addChild(AbstractNode child);
        abstract public List<AbstractNode> getChildren();

        public override string ToString()
        {
            string repr = "" + this.data;
            if (this.isWordEnding)
            {
                repr = repr + "$";
            }
            return repr;
        }

        public void setData(char data)
        {
            this.data = Char.ToLower(data);
        }

        public char getData()
        {
            return this.data;
        }

        public void markAsWordEnding()
        {
            this.isWordEnding = true;
        }
    }

}
