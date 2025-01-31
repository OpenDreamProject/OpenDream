using System.Diagnostics.Contracts;
using System.Text;
using OpenDreamClient.Interface.Html;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Helper for rendering maptext to a render target.
/// Adapted from RobustToolbox's RichTextEntry.
/// </summary>
public sealed class MapTextRenderer(IResourceCache resourceCache, MarkupTagManager tagManager) {
    private const float Scale = 1f;

    private readonly VectorFont _defaultFont =
        new(resourceCache.GetResource<FontResource>("/Fonts/NotoSans-Regular.ttf"), 8);

    private readonly Color _defaultColor = Color.White;

    // TODO: This is probably unoptimal and could cache a lot of things between frames
    public void RenderToTarget(DrawingHandleWorld handle, IRenderTexture texture, string maptext) {
        handle.RenderInRenderTarget(texture, () => {
            handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(texture.Size, Vector2.Zero));

            var message = new FormattedMessage();
            HtmlParser.Parse(maptext, message);

            var (height, lineBreaks) = ProcessWordWrap(message, texture.Size.X);
            var lineHeight = _defaultFont.GetLineHeight(Scale);
            var context = new MarkupDrawingContext();
            context.Color.Push(_defaultColor);
            context.Font.Push(_defaultFont);

            var baseLine = new Vector2(0, height - lineHeight);
            var lineBreakIndex = 0;
            var globalBreakCounter = 0;

            foreach (var node in message) {
                var text = ProcessNode(node, context);
                if (!context.Color.TryPeek(out var color))
                    color = _defaultColor;
                if (!context.Font.TryPeek(out var font))
                    font = _defaultFont;

                foreach (var rune in text.EnumerateRunes()) {
                    if (lineBreakIndex < lineBreaks.Count && lineBreaks[lineBreakIndex] == globalBreakCounter) {
                        baseLine = new(0, baseLine.Y - lineHeight);
                        lineBreakIndex += 1;
                    }

                    var metric = font.GetCharMetrics(rune, Scale);
                    Vector2 mod = new Vector2(0);
                    if (metric.HasValue)
                        mod.Y += metric.Value.BearingY - (metric.Value.Height - metric.Value.BearingY);

                    var advance = font.DrawChar(handle, rune, baseLine + mod, Scale, color);
                    baseLine.X += advance;

                    globalBreakCounter += 1;
                }
            }
        }, Color.Transparent);
    }

    private string ProcessNode(MarkupNode node, MarkupDrawingContext context) {
        // If a nodes name is null it's a text node.
        if (node.Name == null)
            return node.Value.StringValue ?? "";

        //Skip the node if there is no markup tag for it.
        if (!tagManager.TryGetMarkupTag(node.Name, null, out var tag))
            return "";

        if (!node.Closing) {
            tag.PushDrawContext(node, context);
            return tag.TextBefore(node);
        }

        tag.PopDrawContext(node, context);
        return tag.TextAfter(node);
    }

    private (int, List<int>) ProcessWordWrap(FormattedMessage message, float maxSizeX) {
        // This method is gonna suck due to complexity.
        // Bear with me here.
        // I am so deeply sorry for the person adding stuff to this in the future.

        var lineBreaks = new List<int>();
        var height = _defaultFont.GetLineHeight(Scale);

        int? breakLine;
        var wordWrap = new WordWrap(maxSizeX);
        var context = new MarkupDrawingContext();
        context.Font.Push(_defaultFont);
        context.Color.Push(_defaultColor);

        // Go over every node.
        // Nodes can change the markup drawing context and return additional text.
        // It's also possible for nodes to return inline controls. They get treated as one large rune.
        foreach (var node in message) {
            var text = ProcessNode(node, context);

            if (!context.Font.TryPeek(out var font))
                font = _defaultFont;

            // And go over every character.
            foreach (var rune in text.EnumerateRunes()) {
                if (ProcessRune(rune, out breakLine))
                    continue;

                // Uh just skip unknown characters I guess.
                if (!font.TryGetCharMetrics(rune, Scale, out var metrics))
                    continue;

                if (ProcessMetric(metrics, out breakLine))
                    return (height, lineBreaks);
            }
        }

        breakLine = wordWrap.FinalizeText();
        CheckLineBreak(breakLine);
        return (height, lineBreaks);

        bool ProcessRune(Rune rune, out int? outBreakLine) {
            wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
            CheckLineBreak(breakLine);
            CheckLineBreak(breakNewLine);
            outBreakLine = breakLine;
            return skip;
        }

        bool ProcessMetric(CharMetrics metrics, out int? outBreakLine) {
            wordWrap.NextMetrics(metrics, out breakLine, out var abort);
            CheckLineBreak(breakLine);
            outBreakLine = breakLine;
            return abort;
        }

        void CheckLineBreak(int? line) {
            if (line is { } l) {
                lineBreaks.Add(l);
                if (!context.Font.TryPeek(out var font))
                    font = _defaultFont;

                height += font.GetLineHeight(Scale);
            }
        }
    }

    /// <summary>
    /// Helper utility struct for word-wrapping calculations.
    /// </summary>
    private struct WordWrap {
        private readonly float _maxSizeX;

        private float _maxUsedWidth;
        private Rune _lastRune;

        // Index we put into the LineBreaks list when a line break should occur.
        private int _breakIndexCounter;

        private int _nextBreakIndexCounter;

        // If the CURRENT processing word ends up too long, this is the index to put a line break.
        private (int index, float lineSize)? _wordStartBreakIndex;

        // Word size in pixels.
        private int _wordSizePixels;

        // The horizontal position of the text cursor.
        private int _posX;

        // If a word is larger than maxSizeX, we split it.
        // We need to keep track of some data to split it into two words.
        private (int breakIndex, int wordSizePixels)? _forceSplitData = null;

        public WordWrap(float maxSizeX) {
            this = default;
            _maxSizeX = maxSizeX;
            _lastRune = new Rune('A');
        }

        public void NextRune(Rune rune, out int? breakLine, out int? breakNewLine, out bool skip) {
            _breakIndexCounter = _nextBreakIndexCounter;
            _nextBreakIndexCounter += rune.Utf16SequenceLength;

            breakLine = null;
            breakNewLine = null;
            skip = false;

            if (IsWordBoundary(_lastRune, rune) || rune == new Rune('\n')) {
                // Word boundary means we know where the word ends.
                if (_posX > _maxSizeX && _lastRune != new Rune(' ')) {
                    DebugTools.Assert(_wordStartBreakIndex.HasValue,
                        "wordStartBreakIndex can only be null if the word begins at a new line, in which case this branch shouldn't be reached as the word would be split due to being longer than a single line.");
                    //Ensure the assert had a chance to run and then just return
                    if (!_wordStartBreakIndex.HasValue)
                        return;

                    // We ran into a word boundary and the word is too big to fit the previous line.
                    // So we insert the line break BEFORE the last word.
                    breakLine = _wordStartBreakIndex!.Value.index;
                    _maxUsedWidth = Math.Max(_maxUsedWidth, _wordStartBreakIndex.Value.lineSize);
                    _posX = _wordSizePixels;
                }

                // Start a new word since we hit a word boundary.
                //wordSize = 0;
                _wordSizePixels = 0;
                _wordStartBreakIndex = (_breakIndexCounter, _posX);
                _forceSplitData = null;

                // Just manually handle newlines.
                if (rune == new Rune('\n')) {
                    _maxUsedWidth = Math.Max(_maxUsedWidth, _posX);
                    _posX = 0;
                    _wordStartBreakIndex = null;
                    skip = true;
                    breakNewLine = _breakIndexCounter;
                }
            }

            _lastRune = rune;
        }

        public void NextMetrics(in CharMetrics metrics, out int? breakLine, out bool abort) {
            abort = false;
            breakLine = null;

            // Increase word size and such with the current character.
            var oldWordSizePixels = _wordSizePixels;
            _wordSizePixels += metrics.Advance;
            // TODO: Theoretically, does it make sense to break after the glyph's width instead of its advance?
            //   It might result in some more tight packing but I doubt it'd be noticeable.
            //   Also definitely even more complex to implement.
            _posX += metrics.Advance;

            if (_posX <= _maxSizeX)
                return;

            _forceSplitData ??= (_breakIndexCounter, oldWordSizePixels);

            // Oh hey we get to break a word that doesn't fit on a single line.
            if (_wordSizePixels > _maxSizeX) {
                var (breakIndex, splitWordSize) = _forceSplitData.Value;
                if (splitWordSize == 0) {
                    // Happens if there's literally not enough space for a single character so uh...
                    // Yeah just don't.
                    abort = true;
                    return;
                }

                // Reset forceSplitData so that we can split again if necessary.
                _forceSplitData = null;
                breakLine = breakIndex;
                _wordSizePixels -= splitWordSize;
                _wordStartBreakIndex = null;
                _maxUsedWidth = Math.Max(_maxUsedWidth, _maxSizeX);
                _posX = _wordSizePixels;
            }
        }

        public int? FinalizeText() {
            // This needs to happen because word wrapping doesn't get checked for the last word.
            if (_posX > _maxSizeX) {
                if (!_wordStartBreakIndex.HasValue) {
                    throw new Exception(
                        "wordStartBreakIndex can only be null if the word begins at a new line," +
                        "in which case this branch shouldn't be reached as" +
                        "the word would be split due to being longer than a single line.");
                }

                return _wordStartBreakIndex.Value.index;
            } else {
                return null;
            }
        }

        [Pure]
        private static bool IsWordBoundary(Rune a, Rune b) {
            return a == new Rune(' ') || b == new Rune(' ') || a == new Rune('-') || b == new Rune('-');
        }
    }
}
