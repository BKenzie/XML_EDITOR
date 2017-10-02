using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WPF_XML_Tutorial
{
    // Helper class for interchanging between different ActionPaths in the editor window
    class ActionPathXmlNode
    {
        public int PathID
        {
            get; set;
        }

        public XmlNode XmlNode
        {
            get; set;
        }

        public ActionPathXmlNode( XmlNode xmlNode, int pathID )
        {
            PathID = pathID;
            XmlNode = xmlNode;
        }

    }
}
