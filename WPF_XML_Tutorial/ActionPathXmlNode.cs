using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WPF_XML_Tutorial
{
    // Helper class for interchanging between different ActionPaths in the editor window
    class ActionPathXmlNode : IEquatable<ActionPathXmlNode>
    {
        public int PathID
        {
            get; set;
        }

        public XmlNode XmlNode
        {
            get; set;
        }

        public List<String> TabHeaders
        {
            get; set;
        }

        public ActionPathXmlNode( XmlNode xmlNode, int pathID, List<string> tabHeaders )
        {
            PathID = pathID;
            XmlNode = xmlNode;
            TabHeaders = tabHeaders;
        }

        public bool Equals( ActionPathXmlNode other )
        {
            if ( other != null )
            {
                return this.PathID == other.PathID;
            }
            return false;
        }
    }
}
