using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CoApp.VisualStudio
{
    public static class XElementExtensions
    {
        public static string GetOptionalAttributeValue(this XElement element, string localName, string namespaceName = null)
        {
            XAttribute attr;
            if (String.IsNullOrEmpty(namespaceName))
            {
                attr = element.Attribute(localName);
            }
            else
            {
                attr = element.Attribute(XName.Get(localName, namespaceName));
            }
            return attr != null ? attr.Value : null;
        }

        public static string GetOptionalElementValue(this XContainer element, string localName, string namespaceName = null)
        {
            XElement child;
            if (String.IsNullOrEmpty(namespaceName))
            {
                child = element.ElementsNoNamespace(localName).FirstOrDefault();
            }
            else
            {
                child = element.Element(XName.Get(localName, namespaceName));
            }
            return child != null ? child.Value : null;
        }

        public static IEnumerable<XElement> ElementsNoNamespace(this XContainer container, string localName)
        {
            return container.Elements().Where(e => e.Name.LocalName == localName);
        }
    }
}
