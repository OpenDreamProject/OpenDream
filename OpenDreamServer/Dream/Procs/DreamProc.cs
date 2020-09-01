using OpenDreamServer.Dream.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Procs {
    class DreamProc {
        public DreamProc SuperProc = null;

        private Func<DreamProcScope, DreamProcArguments, DreamValue> _runAction;
        private List<string> _argumentNames;
        private Dictionary<string, DreamValue> _defaultArgumentValues;

        public DreamProc(byte[] bytecode, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues = null) {
            _runAction = (DreamProcScope scope, DreamProcArguments arguments) => {
                return new DreamProcInterpreter(this, bytecode).Run(scope, arguments);
            };
            _argumentNames = argumentNames;
            _defaultArgumentValues = defaultArgumentValues;
        }

        public DreamProc(Func<DreamProcScope, DreamProcArguments, DreamValue> nativeProc, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues = null) {
            _runAction = nativeProc;
            _argumentNames = argumentNames;
            _defaultArgumentValues = defaultArgumentValues;
        }

        public DreamValue Run(DreamObject instance, DreamProcArguments arguments) {
            DreamProcScope scope = new DreamProcScope(instance);

            for (int i = 0; i < _argumentNames.Count; i++) {
                string argumentName = _argumentNames[i];

                if (arguments.NamedArguments.ContainsKey(argumentName)) {
                    scope.CreateVariable(argumentName, arguments.NamedArguments[argumentName]);
                } else if (i < arguments.OrderedArguments.Count) {
                    scope.CreateVariable(argumentName, arguments.OrderedArguments[i]);
                } else if (_defaultArgumentValues != null && _defaultArgumentValues.ContainsKey(argumentName)) {
                    scope.CreateVariable(argumentName, _defaultArgumentValues[argumentName]);
                } else {
                    scope.CreateVariable(argumentName, new DreamValue((DreamObject)null));
                }
            }

            scope.CreateVariable("..", new DreamValue(SuperProc));

            return _runAction(scope, arguments);
        }
    }
}
