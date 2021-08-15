using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler {
    public struct CompilerError {
        public Token Token;
        public string Message;
        public StackTrace StackTrace;

        private static List<string> _ignoredMethods = new() { ".ctor", "Check", "Advance", "Consume", "Error", "Warning"};

        public CompilerError(Token token, string message) {
            Token = token;
            Message = message;
            StackTrace = new StackTrace(true);
        }

        public string FilteredStackTrace() {
            StringBuilder sb = new();
            int expression_count = 0;
            foreach (var frame in StackTrace.GetFrames()) {
                var mname = frame.GetMethod().Name;
                if (_ignoredMethods.Contains(mname)) {
                    continue;
                }
                if (mname.Contains("Expression")) {
                    if (mname == "Expression") {
                        expression_count = 0;
                    }
                    else if (expression_count > 2) {
                        continue;
                    }
                    else {
                        expression_count += 1;
                    }
                } 

                Console.Write(frame.GetMethod().Name + ":" + frame.GetFileLineNumber() + " ");
            }
            return sb.ToString();
        }
        public override string ToString() {
            string location;

            if (Token != null) {
                location = Token.SourceFile + ":" + Token.Line + ":" + Token.Column;
            } else {
                location = "(unknown location)";
            }

            return "Error at " + location + ": " + Message;
        }
    }

    public struct CompilerWarning {
        public Token Token;
        public string Message;

        public CompilerWarning(Token token, string message) {
            Token = token;
            Message = message;
        }

        public override string ToString() {
            string location;

            if (Token != null) {
                location = Token.SourceFile + ":" + Token.Line + ":" + Token.Column;
            } else {
                location = "(unknown location)";
            }

            return "Warning at " + location + ": " + Message;
        }
    }

    public class CompileErrorException : Exception {
        public CompilerError Error;

        public CompileErrorException(CompilerError error) : base(error.Message) {
            Error = error;
        }

        public CompileErrorException(string message) : base(message) {
            Error = new CompilerError(null, message);
        }
    }
}
