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

        public bool DeleteFile(string filePath) {
            try {
                File.Delete(Path.Combine(_rootPath, filePath));
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool DeleteDirectory(string directoryPath) {
            try {
                Directory.Delete(Path.Combine(_rootPath, directoryPath), true);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool SaveTextToFile(string filePath, string text) {
            try {
                File.WriteAllText(Path.Combine(_rootPath, filePath), text);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool CopyFile(string sourceFilePath, string destinationFilePath) {
            try {
                File.Copy(Path.Combine(_rootPath, sourceFilePath), Path.Combine(_rootPath, destinationFilePath));
            } catch (Exception) {
                return false;
            }

            return true;
        }
    }
}
