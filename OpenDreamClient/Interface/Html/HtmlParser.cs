using System.Text;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Html;

// Super naive HTML parser for displaying formatted messages throughout the interface
public static class HtmlParser {
    private const string TagNotClosedError = "HTML tag was not closed";

    public static void Parse(string text, FormattedMessage appendTo) {
        StringBuilder currentText = new();
        Stack<string> tags = new();
        int i;

        void SkipWhitespace() {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
        }

        void PushCurrentText() {
            appendTo.AddText(currentText.ToString());
            currentText.Clear();
        }

        for (i = 0; i < text.Length; i++) {
            char c = text[i];

            switch (c) {
                case '<':
                    PushCurrentText();

                    i++;
                    SkipWhitespace();
                    if (i >= text.Length) {
                        Logger.Error(TagNotClosedError);
                        return;
                    }

                    bool closingTag = text[i] == '/';
                    if (closingTag)
                        i++;

                    // Gather everything between the '<' and '>'
                    do {
                        c = text[i];
                        if (c == '>')
                            break;

                        currentText.Append(c);
                        i++;
                    } while (i < text.Length);

                    if (c != '>') {
                        Logger.Error(TagNotClosedError);
                        return;
                    }

                    string insideTag = currentText.ToString();
                    string[] attributes = insideTag.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string tagType = attributes[0].ToLowerInvariant();

                    currentText.Clear();
                    if (closingTag) {
                        if (tags.Count == 0) {
                            Logger.Error("Unexpected closing tag");
                            return;
                        } else if (tags.Peek() != tagType) {
                            Logger.Error($"Invalid closing tag </{tagType}>, expected </{tags.Peek()}>");
                            return;
                        }

                        appendTo.Pop();
                        tags.Pop();
                    } else {
                        tags.Push(tagType);

                        appendTo.PushTag(new MarkupNode(tagType, null, ParseAttributes(attributes)), selfClosing: attributes[^1] == "/");
                    }

                    break;
                case '\n':
                    appendTo.PushNewline();
                    break;
                case '&':
                    // HTML named/numbered entity
                    int end = text.IndexOf(';', i);
                    if (end == -1) {
                        // browsers usually allow for some fallibility here
                        break;
                    }
                    
                    string insideEntity = text.Substring(i + 1, end - (i + 1));
                    i = end;

                    if (insideEntity.StartsWith('#')) {
                        if (int.TryParse(insideEntity.Substring(1), out int result)) {
                            currentText.Append((char) result);
                        }
                    } else {
                        switch (insideEntity) {
                            case "nbsp": currentText.Append("\u00A0"); break;
                            case "lt": currentText.Append("<"); break;
                            case "gt": currentText.Append(">"); break;
                            case "amp": currentText.Append("&"); break;
                            case "quot": currentText.Append("\""); break;
                            case "apos": currentText.Append("'"); break;
                            case "cent": currentText.Append("¢"); break;
                            case "pound": currentText.Append("£"); break;
                            case "yen": currentText.Append("¥"); break;
                            case "euro": currentText.Append("€"); break;
                            case "copyright": currentText.Append("©"); break;
                            case "trademark": currentText.Append("®"); break;
                            default:
                                currentText.Append("&" + insideEntity + ";");
                                break;
                        }
                    }
                    
                    break;
                default:
                    currentText.Append(c);
                    break;
            }
        }

        PushCurrentText();
        while (tags.TryPop(out _))
            appendTo.Pop();
    }

    private static Dictionary<string, MarkupParameter> ParseAttributes(string[] attributes) {
        Dictionary<string, MarkupParameter> parsedAttributes = new();

        for (int i = 1; i < attributes.Length; i++) { // First one should be the tag type, skip it
            string attribute = attributes[i];
            if (attribute == "/")
                continue;

            int equalsIndex = attribute.IndexOf('=');
            if (equalsIndex == -1)
                continue;

            string attributeName = attribute.Substring(0, equalsIndex);
            string attributeValue = attribute.Substring(equalsIndex + 1);
            if (attributeValue[0] is not '"' and not '\'' || attributeValue[^1] is not '"' and not '\'')
                continue;

            string attributeTextValue = attributeValue.Substring(1, attributeValue.Length - 2);
            MarkupParameter parameter;
            switch (attributeName) {
                case "size":
                    long.TryParse(attributeTextValue, out var longValue);
                    parameter = new(longValue);
                    break;
                case "color":
                    if (!Color.TryFromName(attributeTextValue, out var color))
                        color = Color.TryFromHex(attributeTextValue) ?? Color.Black;

                    parameter = new(color);
                    break;
                default:
                    Logger.Debug($"Unimplemented HTML attribute \"{attributeName}\"");
                    continue;
            }

            parsedAttributes.Add(attributeName, parameter);
        }

        return parsedAttributes;
    }
}
