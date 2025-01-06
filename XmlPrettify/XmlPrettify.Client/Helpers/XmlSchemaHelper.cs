namespace XmlPrettify.Client.Helpers
{
    using Radzen;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    public enum SuggestionType
    {
        None,
        Attribute,
        Element,
        String
    }
    public class XmlSuggestion
    {
        public SuggestionType SuggestionType { get; set; }
        public string Suggestion { get; set; }
        public string Documentation { get; set; }
        public string SchemaType { get; set; }
        public int SortOrder { get; set; }

        public string SortOrderString { get => SortOrder.ToString(); }
        public string InsertText {
            get
            {

                if (SuggestionType == SuggestionType.Element)
                    return $"<{Suggestion}></{Suggestion}>";

                else if (SuggestionType == SuggestionType.Attribute)
                    return $"{Suggestion}=\"\"";

                else if (SuggestionType == SuggestionType.String)
                    return Suggestion;

                return Suggestion;
            }
        }
    }

    public class XmlSchemaHelper
    {
        /// <summary>
        /// Finds the child elements of a given element in the XSD schema.
        /// </summary>
        /// <param name="xml">The path to the XSD file.</param>
        /// <param name="elementName">The name of the target element.</param>
        /// <returns>A list of child element names.</returns>
        public static List<XmlSuggestion>? GetChildElements(XDocument xsdDoc, string elementName)
        {
            // Find the target element definition
            var descendants = xsdDoc.Descendants();

            var targetElement = descendants
                .FirstOrDefault(e => e.Name.LocalName == "element" &&
                                     (string?)e.Attribute("name") == elementName);

            if (targetElement == null)
                return new List<XmlSuggestion>();

            IEnumerable<XElement>? targetElementDescendants = null;

            var type = targetElement.Attribute("type");
            if (string.IsNullOrEmpty(type?.Value))
            {
                targetElementDescendants = targetElement.Descendants();
            }
            else
            {
                var dataType = descendants.FirstOrDefault(d => (string?)d.Attribute("name") == type?.Value);
                if (dataType == null)
                    return [];

                targetElementDescendants = dataType.Descendants();
            }

            var sequenceOrChoice = targetElementDescendants
                .FirstOrDefault(e => e.Name.LocalName == "sequence" || e.Name.LocalName == "all" || e.Name.LocalName == "choice");

            if (sequenceOrChoice == null)
                return [];

            var elements = sequenceOrChoice.Elements();

            var choiceElements = elements.Where(e => e.Name.LocalName == "choice").ToList();
            var choiceElementNames = new List<XmlSuggestion>();
            if (choiceElements.Count > 0)
            {
                var childNames = new List<XmlSuggestion>();
                foreach (var choiceElement in choiceElements)
                {
                    var choices = choiceElement.Elements();
                    childNames = GetSuggestionElements(choices);
                }
                choiceElementNames.AddRange(childNames);
            }

            List<XmlSuggestion?> suggestions = GetSuggestionElements(elements);

            suggestions.AddRange(choiceElementNames);

            return suggestions;
        }

        private static string GetItemDocumentation(XElement element)
        {
            if (element.Descendants().FirstOrDefault(descendant => descendant.Name.LocalName == "documentation") is XElement description)
                return description.Value.Trim('\r', '\n', '\t', ' '); ;

            return string.Empty;
        }

        private static List<XmlSuggestion?> GetSuggestionElements(IEnumerable<XElement> elements)
        {
            var suggestions = new List<XmlSuggestion>();
            var counter = 0;
            foreach (var element in elements.Where(e => e.Name.LocalName == "element").Where(e => !string.IsNullOrEmpty((string?)e.Attribute("name"))))
            {
                suggestions.Add(new XmlSuggestion
                {
                    SuggestionType = SuggestionType.Element,
                    Suggestion = (string?)element.Attribute("name"),
                    SchemaType = (string?)element.Attribute("type"),
                    SortOrder = counter,
                    Documentation = GetItemDocumentation(element)
                });
                counter++;
            }
            return suggestions;
        }

        private static List<string> ExtractChildElementNames(XmlSchemaElement schemaElement)
        {
            var childNames = new List<string>();

            if (schemaElement.ElementSchemaType is XmlSchemaComplexType complexType &&
                complexType.ContentTypeParticle is XmlSchemaGroupBase groupBase)
            {
                foreach (XmlSchemaObject item in groupBase.Items)
                {
                    if (item is XmlSchemaElement childElement)
                        childNames.Add(childElement.Name);
                }
            }

            return childNames;
        }
    }
}
