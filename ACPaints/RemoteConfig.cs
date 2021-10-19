using System.Collections.Generic;

namespace ACPaints
{
    class RemoteConfig
    {
        public int Version { get; set; }

        public List<RemoteCar> Cars { get; set; }

        public List<string> Series { get; set; }

        public RemoteConfig()
        {
            Cars = new List<RemoteCar>();
            Series = new List<string>();
        }
    }
}
