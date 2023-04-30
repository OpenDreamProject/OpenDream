using System.Buffers;
using OpenDreamShared.Dream.Procs;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Procs {
    public sealed class DreamProcArguments : IDisposable {
        public readonly int OrderedArgumentCount;
        public Dictionary<string, DreamValue>? NamedArguments;

        public int NamedArgumentCount => NamedArguments?.Count ?? 0;
        public int ArgumentCount => OrderedArgumentCount + NamedArgumentCount;

        private static readonly ArrayPool<DreamValue> OrderedArgPool = ArrayPool<DreamValue>.Shared;

        private readonly DreamValue[] _orderedArguments;

        public DreamProcArguments() {
            _orderedArguments = Array.Empty<DreamValue>();
            OrderedArgumentCount = 0;
        }

        public DreamProcArguments(DreamValue[] orderedArguments, Dictionary<string, DreamValue>? namedArguments = null) {
            NamedArguments = namedArguments;

            OrderedArgumentCount = orderedArguments.Length;
            if (OrderedArgumentCount == 0) {
                _orderedArguments = Array.Empty<DreamValue>();
                return;
            }

            _orderedArguments = OrderedArgPool.Rent(OrderedArgumentCount);
            orderedArguments.CopyTo((Span<DreamValue>) _orderedArguments);
        }

        private DreamProcArguments(int orderedArgCount, int namedArgCount) {
            OrderedArgumentCount = orderedArgCount;

            _orderedArguments = OrderedArgumentCount > 0 ? OrderedArgPool.Rent(OrderedArgumentCount) : Array.Empty<DreamValue>();
            NamedArguments = namedArgCount > 0 ? new Dictionary<string, DreamValue>(namedArgCount) : null;
        }

        public void SetDefaults(List<string> argumentNames, Dictionary<string, DreamValue> defaultValues) {
            foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in defaultValues) {
                int argumentIndex = argumentNames.IndexOf(defaultArgumentValue.Key);

                if (GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                    NamedArguments ??= new(defaultValues.Count);
                    NamedArguments[defaultArgumentValue.Key] = defaultArgumentValue.Value;
                }
            }
        }

        public void Dispose() {
            if (_orderedArguments.Length > 0)
                OrderedArgPool.Return(_orderedArguments);
        }

        public List<DreamValue> GetAllArguments() {
            List<DreamValue> allArguments = new List<DreamValue>(ArgumentCount);

            allArguments.AddRange(_orderedArguments[..OrderedArgumentCount]);
            if (NamedArguments != null)
                allArguments.AddRange(NamedArguments.Values);
            return allArguments;
        }

        public IEnumerator<DreamValue> AllArgumentsEnumerator() {
            for (int i = 0; i < OrderedArgumentCount; i++) {
                yield return _orderedArguments[i];
            }

            if (NamedArguments != null) {
                foreach (DreamValue arg in NamedArguments.Values) {
                    yield return arg;
                }
            }
        }

        /// <remarks>
        /// Argument position is 0-indexed.
        /// </remarks>
        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (NamedArguments?.TryGetValue(argumentName, out DreamValue argumentValue) == true) {
                return argumentValue;
            }

            return GetArgument(argumentPosition);
        }

        /// <inheritdoc cref="GetArgument(int,string)"/>
        public DreamValue GetArgument(int argumentPosition) {
            if (OrderedArgumentCount > argumentPosition) {
                return _orderedArguments[argumentPosition];
            }

            return DreamValue.Null;
        }

        public bool TryGetNamedArgument(string argumentName, out DreamValue value) {
            if(NamedArguments == null) {
                value = DreamValue.Null;
                return false;
            }

            return NamedArguments.TryGetValue(argumentName, out value);
        }

        public override string ToString() {
            return $"<Arguments {OrderedArgumentCount} {NamedArgumentCount}>";
        }

        public static DreamProcArguments FromDMProcState(DMProcState state, int orderedCount, int namedCount) {
            var argumentCount = orderedCount + namedCount;
            var argumentValues = state.PopCount(argumentCount);
            var arguments = new DreamProcArguments(orderedCount, namedCount);

            for (int i = 0; i < argumentCount; i++) {
                DreamProcOpcodeParameterType argumentType = (DreamProcOpcodeParameterType)state.ReadByte();

                switch (argumentType) {
                    case DreamProcOpcodeParameterType.Named: {
                        string argumentName = state.ReadString();

                        arguments.NamedArguments![argumentName] = argumentValues[i];
                        break;
                    }
                    case DreamProcOpcodeParameterType.Unnamed:
                        arguments._orderedArguments[i] = argumentValues[i];
                        break;
                    default:
                        throw new Exception($"Invalid argument type ({argumentType})");
                }
            }

            return arguments;
        }
    }
}
