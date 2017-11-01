﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WPF_XML_Tutorial
{
    // Helper class for interchanging between different ActionPaths in the editor window
    class UOPXmlNode : IEquatable<UOPXmlNode>
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

        public UOPXmlNode( XmlNode xmlNode, int pathID, List<string> tabHeaders )
        {
            PathID = pathID;
            XmlNode = xmlNode;
            TabHeaders = tabHeaders;
        }

        public bool Equals( UOPXmlNode other )
        {
            if ( other != null )
            {
                return this.PathID == other.PathID;
            }
            return false;
        }
    }
}
