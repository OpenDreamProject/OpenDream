using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface
{
    internal interface IInterfaceElement
    {
        public string Name { get; }

        public ElementDescriptor ElementDescriptor { get; protected set; }

        public void SetAttribute(string name, object value);

        public void UpdateElementDescriptor();

        public void Shutdown();
    }
}

