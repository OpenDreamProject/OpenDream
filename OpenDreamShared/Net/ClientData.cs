using System;
using System.IO;
using System.Runtime.Serialization;

namespace OpenDreamShared.Net {
    [Serializable]
    public class ClientData: ISerializable {

        public readonly TimeZoneInfo Timezone;

        public ClientData() {
            Timezone = TimeZoneInfo.Local;
        }

        public ClientData(SerializationInfo info, StreamingContext context) {
            String tData = info.GetString(nameof(Timezone));
            if (tData == null) 
                throw new InvalidDataException("Invalid timezone data received from client.");
            Timezone = TimeZoneInfo.FromSerializedString(tData);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(Timezone), Timezone.ToSerializedString());
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null) 
                throw new ArgumentNullException(nameof(info));  

            GetObjectData(info, context);
        }
    }
}
