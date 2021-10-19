using System.Collections.Generic;

namespace ACPaints
{
    class RemoteCar
    {
        public string Name { get; set; }

        public List<string> Series { get; set; }

        public List<RemoteSkin> Skins { get; set; }
    }
}
