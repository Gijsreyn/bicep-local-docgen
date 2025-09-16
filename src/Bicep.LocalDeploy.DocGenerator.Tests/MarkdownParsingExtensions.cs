using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Bicep.LocalDeploy.DocGenerator.Tests
{
    internal static class MarkdownParsingExtensions
    {
        public static MarkdownDocument ParseMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAutoIdentifiers()
                .Build();
            return Markdown.Parse(markdown, pipeline);
        }

        public static IEnumerable<YamlFrontMatterBlock> FrontMatters(this MarkdownDocument doc) =>
            DescendantsOfType<YamlFrontMatterBlock>(doc);

        public static IEnumerable<(string Level, string Text)> Headings(this MarkdownDocument doc)
        {
            foreach (HeadingBlock h in DescendantsOfType<HeadingBlock>(doc))
            {
                string level = new('#', h.Level);
                string text = GetInlineText(h.Inline);
                yield return (level, text);
            }
        }

        public static string GetFirstParagraph(this MarkdownDocument doc)
        {
            var para = DescendantsOfType<ParagraphBlock>(doc).FirstOrDefault();
            return para is null ? string.Empty : GetInlineText(para.Inline);
        }

        private static IEnumerable<T> DescendantsOfType<T>(MarkdownDocument doc)
            where T : class
        {
            foreach (Block node in Descendants(doc))
            {
                if (node is T t)
                {
                    yield return t;
                }
            }
        }

        private static IEnumerable<Block> Descendants(MarkdownDocument doc)
        {
            foreach (var block in doc)
            {
                foreach (Block d in Descendants(block))
                {
                    yield return d;
                }
            }
        }

        private static IEnumerable<Block> Descendants(Block block)
        {
            yield return block;
            if (block is ContainerBlock container)
            {
                foreach (var child in container)
                {
                    foreach (Block d in Descendants(child))
                    {
                        yield return d;
                    }
                }
            }
        }

        private static string GetInlineText(Inline? inline)
        {
            if (inline is null)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            void AppendInline(Inline? i)
            {
                while (i is not null)
                {
                    switch (i)
                    {
                        case LiteralInline lit:
                            sb.Append(lit.Content.ToString());
                            break;
                        case CodeInline code:
                            sb.Append(code.Content);
                            break;
                        case EmphasisInline emph:
                            if (emph.FirstChild is not null)
                            {
                                AppendInline(emph.FirstChild);
                            }
                            break;
                        case LinkInline link:
                            if (link.FirstChild is not null)
                            {
                                AppendInline(link.FirstChild);
                            }
                            else if (!string.IsNullOrEmpty(link.Title))
                            {
                                sb.Append(link.Title);
                            }
                            break;
                        case ContainerInline ci:
                            if (ci.FirstChild is not null)
                            {
                                AppendInline(ci.FirstChild);
                            }
                            break;
                    }
                    i = i.NextSibling;
                }
            }
            // Start from FirstChild if it's a container of inlines
            AppendInline(inline is ContainerInline c ? c.FirstChild : inline);
            return sb.ToString();
        }
    }
}
