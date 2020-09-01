﻿using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Procs {
    interface IDreamProcIdentifier {
        public DreamValue GetValue();
        public void Assign(DreamValue value);
    }

    struct DreamProcIdentifierVariable : IDreamProcIdentifier {
        public DreamProcScope HoldingScope;
        public string IdentifierName;

        public DreamProcIdentifierVariable(DreamProcScope holdingScope, string identifierName) {
            HoldingScope = holdingScope;
            IdentifierName = identifierName;
        }

        public DreamValue GetValue() {
            return HoldingScope.GetValue(IdentifierName);
        }

        public void Assign(DreamValue value) {
            HoldingScope.AssignValue(IdentifierName, value);
        }
    }

    struct DreamProcIdentifierListIndex : IDreamProcIdentifier {
        public DreamObject List;
        public DreamValue ListIndex;

        public DreamProcIdentifierListIndex(DreamObject list, DreamValue listIndex) {
            List = list;
            ListIndex = listIndex;

            if (!list.IsSubtypeOf(DreamPath.List)) {
                throw new ArgumentException("Parameter must be a dream object of type " + DreamPath.List, nameof(list));
            }
        }

        public DreamValue GetValue() {
            return DreamMetaObjectList.DreamLists[List].GetValue(ListIndex);
        }

        public void Assign(DreamValue value) {
            DreamMetaObjectList.DreamLists[List].SetValue(ListIndex, value);
        }
    }

    struct DreamProcIdentifierSelfProc : IDreamProcIdentifier {
        public DreamProc SelfProc;
        public DreamProcInterpreter Interpreter;

        public DreamProcIdentifierSelfProc(DreamProc selfProc, DreamProcInterpreter interpreter) {
            SelfProc = selfProc;
            Interpreter = interpreter;
        }

        public DreamValue GetValue() {
            return Interpreter.DefaultReturnValue;
        }

        public void Assign(DreamValue value) {
            Interpreter.DefaultReturnValue = value;
        }
    }
}
