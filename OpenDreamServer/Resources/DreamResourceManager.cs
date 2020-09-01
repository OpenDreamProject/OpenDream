using OpenDreamServer.Net;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenDreamServer.Resources {
    class DreamResourceManager {
        private string _rootPath;

        public DreamResourceManager(string rootPath) {
            _rootPath = rootPath;
        }

        public bool DoesResourceExist(string resourcePath) {
            return File.Exists(Path.Combine(_rootPath, resourcePath));
        }

        public DreamResource LoadResource(string resourcePath) {
            FileStream stream = new FileStream(Path.Combine(_rootPath, resourcePath), FileMode.Open);
            byte[] resourceData = new byte[stream.Length];
            int readBytes = 0;

            while (readBytes < stream.Length) readBytes += stream.Read(resourceData);
            stream.Close();

            return new DreamResource(resourcePath, resourceData);
        }

        public void HandleRequestResourcePacket(DreamConnection connection, PacketRequestResource pRequestResource) {
            DreamResource resource = LoadResource(pRequestResource.ResourcePath);

            connection.SendPacket(new PacketResource(resource.ResourcePath, resource.ResourceData));
        }
    }
}
