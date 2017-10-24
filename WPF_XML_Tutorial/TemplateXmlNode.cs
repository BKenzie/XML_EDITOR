using System.Collections.Generic;
using System.Xml;

namespace WPF_XML_Tutorial
{

    // Helper class for keeping track of different available templates in the editor
    public class TemplateXmlNode : System.IEquatable<TemplateXmlNode>
    {
        public string Name
        {
            get; set;
        }

        public XmlNode XmlNode
        {
            get; set;
        }

        public List<string> TabHeaders
        {
            get; set;
        }

        public TemplateXmlNode( XmlNode xmlNode, string name )
        {
            Name = name;
            XmlNode = xmlNode;
        }

        public bool Equals( TemplateXmlNode other )
        {
            if ( other != null )
            {
                return this.Name == other.Name;
            }
            return false;
        }
    }
}