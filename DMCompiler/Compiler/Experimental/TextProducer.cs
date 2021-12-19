
using System;
using System.IO;
using System.Collections.Generic;

namespace DMCompiler.Compiler.Experimental {
    public class SourceText {
        public char[] Text = null;
        public string IncludeBaseDir;
        public string IncludePath;
        public string RootDir;
        public string FullPath;
        public string FileName;

        public int Length { get { return Text.Length; } }

        public SourceText(string include_base_dir, string include_path) {
            IncludeBaseDir = include_base_dir;
            IncludePath = include_path.Replace('\\', Path.DirectorySeparatorChar);
            FullPath = Path.Combine(IncludeBaseDir, IncludePath);
            RootDir = Path.GetDirectoryName(FullPath);
            FileName = Path.GetFileName(FullPath);
        }
        public void LoadSource() {
            if (Text != null) { return; }
            if (!File.Exists(FullPath)) {
                Text = "\n".ToCharArray();
                //throw new Exception("file does not exist " + FullPath);
                Console.WriteLine("file does not exist " + FullPath);
                return;
            }
            string source = File.ReadAllText(FullPath);
            source = source.Replace("\r\n", "\n");
            source += '\n';
            Text = source.ToCharArray();
        }
    }
    public class SourceLocation {
        public SourceText Source;
        public int Position;
        public int Line;
        public int Column;
    }
    public class TextProducer {
        // state
        SourceText current_source = null;
        Stack<SourceText> _sourceTexts = new();
        Stack<SourceLocation> _sourceLocations = new();

        // saved state
        Stack<int> saved_positions = new();

        // optimizations
        private bool _is_end;
        int _cpos = 0;
        int _cLine = 0;
        int _cColumn = 0;
        char[] _ctext = null;

        public bool IsEnd { get { return _is_end; } }

        public int CurrentPosition() {
            return _cpos;
        }

        public string GetString(int start, int end) {
            return new string(_ctext, start, end - start);
        }
        public string GetString(int size) {
            int start = _cpos;
            return GetString(start, start + size);
        }
        public SourceLocation CurrentLocation() {
            var loc = new SourceLocation();
            loc.Source = current_source;
            loc.Position = _cpos;
            loc.Line = _cLine;
            loc.Column = _cColumn;
            return loc;
        }

        public char? Peek(int n) {
            if (_cpos + n < 0) { return null; }
            if (_cpos + n >= _ctext.Length) {
                return null;
            }
            return _ctext[_cpos + n];
        }
        public void Include(SourceText srctext) {
            srctext.LoadSource();
            if (saved_positions.Count != 0) { throw new Exception("attempt to SavePosition past #include boundary"); }
            if (srctext.Length == 0) { return; }
            if (current_source != null) {
                _sourceTexts.Push(current_source);
                _sourceLocations.Push(CurrentLocation());
            }
            current_source = srctext;
            //Console.WriteLine("push to " + current_source.FullPath);
            _ctext = srctext.Text;
            _cpos = 0;
            _cLine = 1;
            _cColumn = 0;
            Update();
        }
        public void SavePosition() {
            saved_positions.Push(_cpos);
        }
        public void AcceptPosition() {
            saved_positions.Pop();
            Update();
        }
        public void RestorePosition() {
            _cpos = saved_positions.Pop();
            Update();
        }
        public int SourceRemaining() {
            return _ctext.Length - _cpos;
        }

        public void Advance(int n) {
            for (int i = 0; i < n; i++) {
                ProducerNext();
            }
        }
        public char? ProducerNext() {
            if (_cpos < _ctext.Length) {
                char c = _ctext[_cpos++];
                _cColumn++;
                if (c == '\n') {
                    _cColumn = 0;
                    _cLine += 1;
                }
                return c;
            }
            else {
                if (_sourceTexts.Count == 0) {
                    if (current_source == null) {
                        throw new Exception("attempt to read past eof");
                    }
                    current_source = null;
                    return null;
                }
                if (saved_positions.Count > 0) {
                    throw new Exception("EndOfFile reached with saved position");
                }
                current_source = _sourceTexts.Pop();
                SourceLocation loc = _sourceLocations.Pop();
                //Console.WriteLine("pop to " + current_source.FullPath);
                _ctext = current_source.Text;
                _cpos = loc.Position;
                _cLine = loc.Line;
                _cColumn = loc.Column;
                Update();
                return null;
            }
        }

        public void Update() {
            if (_cpos + 1 == _ctext.Length && _sourceTexts.Count == 0) { _is_end = true; }
        }
        public bool ProducerEnd() {
            return _is_end;
        }

        public bool Match(string s, int offset = 0) {
            if (s.Length + offset > SourceRemaining()) {
                return false;
            }
            for (int i = 0; i < s.Length; i++) {
                if (_ctext[_cpos + i + offset] != s[i]) { return false; }
            }
            return true;
        }

        public bool IsIdentifierStart(char c) {
            return (IsAlphabetic(c) || c == '_');
        }
        public bool IsIdentifier(char c) {
            return (IsAlphabetic(c) || IsNumeric(c) || c == '_');
        }
        public bool IsAlphabetic(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public bool IsNumeric(char c) {
            return (c >= '0' && c <= '9');
        }

        public bool IsAlphanumeric(char c) {
            return IsAlphabetic(c) || IsNumeric(c);
        }

        public bool IsHex(char c) {
            return IsNumeric(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}
