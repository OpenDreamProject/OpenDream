using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMParser : Parser {
        public DMParser(DMLexer lexer) : base(lexer) {

        }

        public DMASTFile File() {
            DMASTBlockInner blockInner = BlockInner();
            Newline();
            Consume(TokenType.EndOfFile, "Expected EOF");

            return new DMASTFile(blockInner);
        }

        public DMASTBlockInner BlockInner() {
            DMASTStatement statement = Statement();

            if (statement != null) {
                List<DMASTStatement> statements = new List<DMASTStatement>() { statement };

                while (Delimiter()) {
                    statement = Statement();

                    if (statement != null) {
                        statements.Add(statement);
                    }
                }

                return new DMASTBlockInner(statements.ToArray());
            } else {
                return null;
            }
        }

        public DMASTStatement Statement() {
            DMASTPath path = Path();

            if (path != null) {
                return new DMASTStatement(path);
            } else {
                return null;
            }
        }

        public DMASTPath Path() {
            DreamPath.PathType pathType = DreamPath.PathType.Relative;
            bool hasPathTypeToken = true;

            if (Check(TokenType.DM_Slash)) {
                pathType = DreamPath.PathType.Absolute;
            } else if (Check(TokenType.DM_Colon)) {
                pathType = DreamPath.PathType.DownwardSearch;
            } else if (Check(TokenType.DM_Period)) {
                pathType = DreamPath.PathType.UpwardSearch;
            } else {
                hasPathTypeToken = false;
            }

            DMASTPathElement pathElement = PathElement();
            if (pathElement != null) {
                List<DMASTPathElement> pathElements = new List<DMASTPathElement>() { pathElement };

                while (pathElement != null && Check(TokenType.DM_Slash)) {
                    pathElement = PathElement();

                    if (pathElement != null) {
                        pathElements.Add(pathElement);
                    }
                }

                return new DMASTPath(pathType, pathElements.ToArray());
            } else if (hasPathTypeToken) {
                return new DMASTPath(pathType);
            }

            return null;
        }

        public DMASTPathElement PathElement() {
            Token elementToken = Current();

            if (Check(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Var, TokenType.DM_Proc })) {
                return new DMASTPathElement(elementToken.Text);
            } else {
                return null;
            }
        }

        private bool Newline() {
            return Check(TokenType.Newline);
        }

        private bool Delimiter() {
            return Check(TokenType.DM_Semicolon) || Newline();
        }
    }
}
