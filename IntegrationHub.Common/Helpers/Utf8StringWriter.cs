using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Helpers
{
    // Pomocnicza klasa, by StringWriter był w UTF-8 (domyślnie UTF-16)
    public class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(){ }
        public Utf8StringWriter(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
