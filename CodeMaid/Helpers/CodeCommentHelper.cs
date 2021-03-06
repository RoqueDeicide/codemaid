﻿#region CodeMaid is Copyright 2007-2015 Steve Cadwallader.

// CodeMaid is free software: you can redistribute it and/or modify it under the terms of the GNU
// Lesser General Public License version 3 as published by the Free Software Foundation.
//
// CodeMaid is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details <http://www.gnu.org/licenses/>.

#endregion CodeMaid is Copyright 2007-2015 Steve Cadwallader.

using EnvDTE;
using SteveCadwallader.CodeMaid.Properties;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SteveCadwallader.CodeMaid.Helpers
{
    /// <summary>
    /// A set of helper methods focused around code comments.
    /// </summary>
    internal static class CodeCommentHelper
    {
        public const int CopyrightExtraIndent = 4;
        public const char KeepTogetherSpacer = '\a';
        public const char Spacer = ' ';

        /// <summary>
        /// Creates the XML close tag string for an XElement.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>
        /// The XML close tag, or <c>null</c> if the element has no value and is a self-closing tag.
        /// </returns>
        internal static string CreateXmlCloseTag(System.Xml.Linq.XElement element)
        {
            if (element.IsEmpty)
            {
                return null;
            }

            var name = element.Name.LocalName;

            var result = string.Format("</{0}>", Settings.Default.Formatting_CommentXmlTagsToLowerCase ? name.ToLowerInvariant() : name);

            return Settings.Default.Formatting_CommentXmlKeepTagsTogether ? SpaceToFake(result) : result;
        }

        /// <summary>
        /// Creates the XML open tag string for an XElement.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The XML open tag. In case of an element without value, the tag is self-closing.</returns>
        internal static string CreateXmlOpenTag(System.Xml.Linq.XElement element)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("<");
            var name = element.Name.LocalName;
            builder.Append(Settings.Default.Formatting_CommentXmlTagsToLowerCase ? name.ToLowerInvariant() : name);

            if (element.HasAttributes)
            {
                foreach (var attr in element.Attributes())
                {
                    builder.Append(Spacer);
                    builder.Append(attr);
                }
            }

            if (element.IsEmpty)
            {
                if (Settings.Default.Formatting_CommentXmlSpaceSingleTags)
                {
                    builder.Append(Spacer);
                }

                builder.Append("/");
            }

            builder.Append(">");

            var result = builder.ToString();

            return Settings.Default.Formatting_CommentXmlKeepTagsTogether ? SpaceToFake(result) : result;
        }

        internal static string FakeToSpace(string value)
        {
            return value.Replace(KeepTogetherSpacer, Spacer);
        }

        /// <summary>
        /// Get the comment prefix (regex) for the given document's language.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>The comment prefix regex, without trailing spaces.</returns>
        internal static string GetCommentPrefix(TextDocument document)
        {
            return GetCommentPrefixForLanguage(document.Language);
        }

        /// <summary>
        /// Get the comment prefix (regex) for the given document's language.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>The comment prefix regex, without trailing spaces.</returns>
        internal static string GetCommentPrefixForLanguage(string language)
        {
            switch (language)
            {
                case "C/C++":
                case "CSharp":
                case "CSS":
                case "F#":
                case "JavaScript":
                case "JScript":
                case "LESS":
                case "Node.js":
                case "PHP":
                case "SCSS":
                case "TypeScript":
                    return "///?";

                case "Basic":
                    return "'+";

                case "PowerShell":
                    return "#+";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the regex for matching a complete comment line.
        /// </summary>
        internal static Regex GetCommentRegex(string language, bool includePrefix = true)
        {
            string prefix = null;
            if (includePrefix)
            {
                prefix = GetCommentPrefixForLanguage(language);
                if (prefix == null)
                {
                    Debug.Fail("Attempting to create a comment regex for a document that has no comment prefix specified.");
                }

                // Be aware of the added space to the prefix. When prefix is added, we should take
                // care not to match code comment lines.
                prefix = string.Format(@"(?<prefix>[\t ]*{0})(?<initialspacer>( |\t|\r|\n|$))?", prefix);
            }

            var pattern = string.Format(@"^{0}(?<line>(?<indent>[\t ]*)(?<listprefix>[-=\*\+]+[ \t]*|\w+[\):][ \t]+|\d+\.[ \t]+)?((?<words>[^\t\r\n ]+)*[\t ]*)*)[\r]*[\n]?$", prefix);
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
        }

        internal static int GetTabSize(CodeMaidPackage package, TextDocument document)
        {
            const int fallbackTabSize = 4;

            try
            {
                var settings = package.IDE.Properties["TextEditor", document.Language];
                var tabsize = settings.Item("TabSize").Value as int? ?? fallbackTabSize;
                return tabsize;
            }
            catch (Exception)
            {
                // Some languages (e.g. F#, PowerShell) may not correctly resolve tab settings.
                return fallbackTabSize;
            }
        }

        internal static bool IsCommentLine(EditPoint point)
        {
            return LineMatchesRegex(point, GetCommentRegex(point.Parent.Language)).Success;
        }

        internal static Match LineMatchesRegex(EditPoint point, Regex regex)
        {
            var line = point.GetLine();
            var match = regex.Match(line);
            return match;
        }

        internal static string SpaceToFake(string value)
        {
            return value.Replace(Spacer, KeepTogetherSpacer);
        }
    }
}